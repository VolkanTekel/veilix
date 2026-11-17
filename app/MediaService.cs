// GPL-3.0-or-later. Stable session selection and bounded media recovery.
using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Security.Cryptography;using System.Threading.Tasks;
#if WINUI
using BitmapSource=Ada.Artwork;
using Dispatcher=Veilix.MediaDispatcher;
using DispatcherTimer=Veilix.MediaTimer;
using DispatcherPriority=Veilix.MediaPriority;
#else
using System.Windows.Media.Imaging;
using System.Windows.Threading;
#endif
namespace Ada {
 public sealed class MediaService:IDisposable {
  readonly IMediaBackend backend;readonly Dispatcher dispatcher;readonly DispatcherTimer watchdog;readonly Func<DateTimeOffset> now;readonly MediaTransport transport;readonly SpotifyHistory history;
  IMediaConnection connection;IMediaSession session;TimelineState timeline;
  readonly bool automatic;bool disposed,busy,pending,metadataDirty=true,metadataReady,historyLoaded;int generation,emptyScans;string coverKey="",historySignature="";
  DateTimeOffset? missingSince;DateTimeOffset lastMetadata;readonly Queue<string> diagnostics=new Queue<string>();
  public event Action Changed;
  public string Title="",Artist="",Error="",SourceAppId="";
  public bool Playing,CanPlay,CanNext,CanPrevious,CanSeek;
  public TimeSpan Position,Duration;public BitmapSource Cover;
  internal bool UseHistory=true;
  bool otherSources;
  internal void SetOtherSources(bool enabled)
  {
   if(otherSources==enabled)return;
   otherSources=enabled;
   generation++;
   if(!enabled&&!SpotifyIdentity.Matches(SourceAppId))
   {
    DetachSession();
    Title=Artist=SourceAppId=coverKey=Error="";
    Cover=null;
    Position=Duration=TimeSpan.Zero;
    timeline=null;
    Playing=CanPlay=CanNext=CanPrevious=CanSeek=false;
    Notify();
   }
   Schedule();
  }
  public MediaService():this(new WindowsMediaBackend(),true,()=>DateTimeOffset.UtcNow,null,Path.Combine(Settings.DirectoryPath,"spotify-last-track.json")){}
  internal MediaService(IMediaBackend backend,bool automatic,Func<DateTimeOffset> now,TimeSpan? timeout=null,string historyPath=null){this.backend=backend;this.automatic=automatic;this.now=now;history=historyPath==null?null:new SpotifyHistory(historyPath);transport=new MediaTransport(timeout??TimeSpan.FromSeconds(4));dispatcher=Dispatcher.CurrentDispatcher;watchdog=new DispatcherTimer(DispatcherPriority.Background,dispatcher){Interval=TimeSpan.FromSeconds(3)};watchdog.Tick+=async delegate{await Reconcile();};}
  public string Diagnostics{get{return String.Join(Environment.NewLine,diagnostics);}}
  void Trace(string value){diagnostics.Enqueue(now().ToString("O")+" "+value);while(diagnostics.Count>40)diagnostics.Dequeue();}
  public async Task Start(){if(disposed)return;if(!historyLoaded){historyLoaded=true;if(UseHistory&&history!=null){var saved=await Task.Run(()=>history.Load());if(disposed)return;if(saved!=null&&String.IsNullOrWhiteSpace(Title)){Title=saved.Title;Artist=saved.Artist;SourceAppId=saved.AppId;Cover=saved.Cover;Position=TimeSpan.FromSeconds(saved.Position);Duration=TimeSpan.FromSeconds(saved.Duration);Playing=CanPlay=CanNext=CanPrevious=CanSeek=false;Notify();}}}if(automatic&&!disposed)watchdog.Start();await Reconcile();}
  void Schedule(){if(disposed||dispatcher.HasShutdownStarted)return;dispatcher.BeginInvoke(DispatcherPriority.Background,new Action(async delegate{await Reconcile();}));}
  void ConnectionChanged(){Schedule();}
  void SessionChanged(MediaSignal signal){if(disposed)return;dispatcher.BeginInvoke(DispatcherPriority.Background,new Action(delegate{if(disposed)return;if(signal==MediaSignal.Metadata)metadataDirty=true;Schedule();}));}
  static bool Same(IMediaSession a,IMediaSession b){return a!=null&&b!=null&&Object.Equals(a.Key,b.Key);}
  sealed class Candidate {public IMediaSession Session;public PlaybackState Playback;public long Birth;}
  sealed class Instance {public WeakReference Key;public long Birth;}
  readonly List<Instance> instances=new List<Instance>();long scanNumber;
  sealed class Scan {public List<Candidate> Items=new List<Candidate>();public IMediaSession Current;}
  Scan ReadSessions(IMediaConnection link,IMediaSession tracked,string trackedId,bool includeOthers){var result=new Scan();var sessions=link.Sessions().Where(s=>includeOthers||SpotifyIdentity.Matches(s.AppId)).ToList();try{var current=link.Current();if(current!=null&&(includeOthers||SpotifyIdentity.Matches(current.AppId)))result.Current=current;}catch{}Exception last=null,trackedFailure=null;foreach(var item in sessions)try{result.Items.Add(new Candidate{Session=item,Playback=item.Playback()});}catch(Exception ex){if(Same(item,tracked)||(!String.IsNullOrEmpty(trackedId)&&item.AppId==trackedId))trackedFailure=ex;last=ex;}if(sessions.Count>0&&result.Items.Count==0&&last!=null)throw last;
   // A dead old Spotify proxy must not poison its healthy replacement. Inspect
   // the entire list before deciding whether a manager rebuild is necessary.
   if(trackedFailure!=null&&!result.Items.Any(c=>c.Session.AppId==trackedId)&&!result.Items.Any(c=>c.Playback.Playing))throw trackedFailure;return result;}
  Candidate Choose(Scan scan){
   // Instance identity is independent of AppId. A newly discovered instance
   // gets a chance before an older proxy with cached Playing/current state.
   var newest=scan.Items.GroupBy(c=>c.Session.AppId).ToDictionary(g=>g.Key,g=>g.Max(c=>c.Birth));
   var available=scan.Items.Where(c=>c.Birth==newest[c.Session.AppId]).ToList();
   return available.FirstOrDefault(c=>Same(c.Session,scan.Current)&&c.Playback.Playing)
    ??available.FirstOrDefault(c=>Same(c.Session,session)&&c.Playback.Playing)
    ??available.FirstOrDefault(c=>c.Session.AppId==SourceAppId&&c.Playback.Playing)
    ??available.FirstOrDefault(c=>c.Playback.Playing)
    ??available.FirstOrDefault(c=>Same(c.Session,session))
    ??available.FirstOrDefault(c=>Same(c.Session,scan.Current))??available.FirstOrDefault();
  }
  void StampInstances(Scan scan){scanNumber++;instances.RemoveAll(i=>!i.Key.IsAlive);foreach(var candidate in scan.Items){object key=candidate.Session.Key;var found=instances.FirstOrDefault(i=>Object.Equals(i.Key.Target,key));if(found==null){found=new Instance{Key=new WeakReference(key),Birth=scanNumber};instances.Add(found);}candidate.Birth=found.Birth;}while(instances.Count>64){var old=instances.FirstOrDefault(i=>!scan.Items.Any(c=>Object.Equals(c.Session.Key,i.Key.Target)));if(old==null)break;instances.Remove(old);}}
  internal async Task Reconcile(){
   if(disposed)return;if(busy){pending=true;return;}busy=true;
   try{
    if(connection==null){var fresh=await transport.Run(()=>backend.Connect(),late=>late.Dispose());if(disposed){fresh.Dispose();return;}connection=fresh;connection.Changed+=ConnectionChanged;Trace("manager connected");}
    var activeConnection=connection;var tracked=session;string trackedId=SourceAppId;bool includeOthers=otherSources;int scanTicket=generation;var scan=await transport.Read(()=>ReadSessions(activeConnection,tracked,trackedId,includeOthers));if(disposed||connection!=activeConnection||scanTicket!=generation)return;
    StampInstances(scan);
    if(Environment.GetCommandLineArgs().Contains("--inspect"))Trace("scan "+String.Join(", ",scan.Items.Select(c=>c.Session.AppId+" playing="+c.Playback.Playing+" instance="+c.Birth)));
    var candidate=Choose(scan);if(candidate==null){MarkMissing("no available session");if(++emptyScans>=2){ResetConnection();emptyScans=0;}return;}
    emptyScans=0;
    TrackMetadata props=null;TimelineState nextTimeline=null;bool readMetadata=false;int readTicket=generation;
    // Validate before rebinding. An empty/broken/hung old proxy is rejected
    // independently; it cannot prevent a healthy candidate in this scan.
    while(candidate!=null){
     try{
      var probing=candidate.Session;nextTimeline=await transport.Read(()=>probing.Timeline());if(disposed||readTicket!=generation)return;
      readMetadata=!Same(session,probing)||!metadataReady||metadataDirty||String.IsNullOrWhiteSpace(Title)||(now()-lastMetadata).TotalSeconds>=15;
      props=readMetadata?await transport.Run(()=>probing.Metadata()):null;if(disposed||readTicket!=generation)return;
      if(readMetadata&&(props==null||String.IsNullOrWhiteSpace(props.Title)))throw new InvalidDataException("Empty media metadata");
      break;
     }catch(Exception ex){if(disposed||readTicket!=generation)return;Trace("candidate rejected "+candidate.Session.AppId+" 0x"+ex.HResult.ToString("X8")+" "+ex.GetType().Name);scan.Items.Remove(candidate);candidate=Choose(scan);}
    }
    if(candidate==null){ResetConnection();MarkMissing("no healthy media candidate",true);return;}
    bool changed=!Same(session,candidate.Session);
    if(changed){DetachSession();session=candidate.Session;session.Changed+=SessionChanged;metadataReady=false;metadataDirty=true;Trace("selected "+session.AppId+"; Windows current="+(scan.Current==null?"none":scan.Current.AppId));}
    var active=session;int ticket=generation;var state=candidate.Playback;
    bool wasPlaying=Playing;timeline=nextTimeline;Playing=state.Playing;CanPlay=state.CanPlay;CanNext=state.CanNext;CanPrevious=state.CanPrevious;CanSeek=state.CanSeek;RefreshTimeline();
    if(readMetadata){
     metadataDirty=false;
     bool sameTrack=SourceAppId==active.AppId&&Title==props.Title&&Artist==props.Artist;
     Title=props.Title;Artist=props.Artist;SourceAppId=active.AppId;metadataReady=true;lastMetadata=now();missingSince=null;Error="";
     if(!sameTrack){Cover=null;coverKey="";}Notify();
     // A broken thumbnail must never discard valid track metadata.
     if(props.Artwork!=null)try{
      string oldKey=coverKey,sourceId=SourceAppId;BitmapSource oldCover=Cover;
      var decoded=await transport.Run(async delegate{var bytes=await props.Artwork();if(bytes==null)return Tuple.Create((BitmapSource)null,"");string key;using(var hash=SHA256.Create())key=Convert.ToBase64String(hash.ComputeHash(bytes));if(key==oldKey&&oldCover!=null)return Tuple.Create(oldCover,key);return Tuple.Create(MediaArtwork.SongCover(ArtworkCodec.Decode(bytes),sourceId),key);});
      if(disposed||ticket!=generation)return;Cover=decoded.Item1;coverKey=decoded.Item2;
     }catch(Exception ex){Trace("artwork unavailable: "+ex.GetType().Name);}
    }else{missingSince=null;Error="";}
    SaveHistory(wasPlaying&&!Playing);Notify();
   }catch(Exception ex){if(!disposed){Trace("transport failure 0x"+ex.HResult.ToString("X8")+" "+ex.GetType().Name);ResetConnection();MarkMissing("transport unavailable",true);}}
   finally{busy=false;if(pending&&!disposed){pending=false;Schedule();}}
  }
  void MarkMissing(string reason,bool transport=false){if(!missingSince.HasValue){RefreshTimeline();SaveHistory(true);missingSince=now();Trace(reason+"; retaining last Spotify track");}Playing=CanPlay=CanNext=CanPrevious=CanSeek=false;timeline=null;metadataReady=false;Error=transport?"Medya bağlantısı yenileniyor…":"";Notify();}
  void SaveHistory(bool force=false){if(!UseHistory||history==null||!SpotifyIdentity.Matches(SourceAppId)||String.IsNullOrWhiteSpace(Title))return;string signature=Title+"\n"+Artist+"\n"+coverKey;if(!force&&historySignature==signature)return;historySignature=signature;history.Save(new SpotifySnapshot{Title=Title,Artist=Artist,AppId=SourceAppId,Position=Position.TotalSeconds,Duration=Duration.TotalSeconds,Cover=Cover});}
  internal Task FlushHistory(){return history==null?Task.FromResult(0):history.Flush();}
  void DetachSession(){generation++;if(session!=null)try{session.Changed-=SessionChanged;}catch{}session=null;metadataReady=false;}
  void ResetConnection(){DetachSession();var old=connection;connection=null;if(old!=null){try{old.Changed-=ConnectionChanged;old.Dispose();}catch{}}metadataDirty=true;}
  public void RefreshTimeline(){if(timeline==null)return;Duration=timeline.Duration;Position=timeline.Position;if(Playing&&timeline.Updated!=DateTimeOffset.MinValue){var delta=now()-timeline.Updated;if(delta>TimeSpan.Zero&&delta<TimeSpan.FromDays(1))Position+=delta;}if(Duration>TimeSpan.Zero&&Position>Duration)Position=Duration;if(Position<TimeSpan.Zero)Position=TimeSpan.Zero;}
  public async Task Command(string command,double ratio=0){if(disposed||session==null||!metadataReady||missingSince.HasValue)return;var active=session;int ticket=generation;try{bool ok=await transport.Run(()=>active.Command(command,(long)(Duration.Ticks*Math.Max(0,Math.Min(1,ratio)))));if(disposed||ticket!=generation)return;Error=ok?"":"Oynatıcı bu komutu desteklemiyor.";await Reconcile();}catch(Exception ex){if(disposed||ticket!=generation)return;Trace("command failure 0x"+ex.HResult.ToString("X8"));ResetConnection();MarkMissing("command transport unavailable",true);}}
  void Notify(){if(disposed)return;var handler=Changed;if(handler!=null)handler();}
  public void Dispose(){if(disposed)return;SaveHistory(true);disposed=true;watchdog.Stop();ResetConnection();Cover=null;}
 }
}
