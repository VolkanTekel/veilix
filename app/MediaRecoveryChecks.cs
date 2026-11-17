// GPL-3.0-or-later. Fault injection; never controls a real media player.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
#if !WINUI
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
#endif
namespace Ada {
 internal sealed class TestMediaSession:IMediaSession {
  public object Key{get;set;}public string AppId{get;set;}public bool Playing;public string Title="Track",Artist="Artist";public bool BrokenArtwork,EmptyMetadata,PlaybackFailure;public int Commands,Subscribers,Reads;
  public Func<Task<TrackMetadata>> PendingMetadata;
  Action<MediaSignal> changed;public event Action<MediaSignal> Changed{add{changed+=value;Subscribers++;}remove{changed-=value;Subscribers--;}}
  public void Signal(MediaSignal kind){var handler=changed;if(handler!=null)handler(kind);}
  public PlaybackState Playback(){if(PlaybackFailure)throw new COMException("RPC unavailable",unchecked((int)0x800706BA));return new PlaybackState{Playing=Playing,CanPlay=true,CanNext=true,CanPrevious=true,CanSeek=true};}
  public TimelineState Timeline(){return new TimelineState{Position=TimeSpan.FromSeconds(20),Duration=TimeSpan.FromSeconds(200),Updated=DateTimeOffset.MinValue};}
  public Task<TrackMetadata> Metadata(){Reads++;if(PendingMetadata!=null)return PendingMetadata();return Task.FromResult(new TrackMetadata{Title=EmptyMetadata?"":Title,Artist=Artist,Artwork=BrokenArtwork?(Func<Task<byte[]>>)(()=>{throw new IOException("Broken thumbnail");}):null});}
  public Task<bool> Command(string command,long ticks){Commands++;return Task.FromResult(true);}
 }
 internal sealed class TestMediaBackend:IMediaBackend {
  public List<IMediaSession> Items=new List<IMediaSession>();public IMediaSession Current;public int Connections,Disposed;public bool RpcFailure,ConnectFailure;
  public Task<IMediaConnection> Connect(){Connections++;if(ConnectFailure)throw new COMException("RPC unavailable",unchecked((int)0x800706BA));return Task.FromResult<IMediaConnection>(new Link(this));}
  sealed class Link:IMediaConnection {readonly TestMediaBackend owner;public Link(TestMediaBackend owner){this.owner=owner;}public event Action Changed{add{}remove{}}
   public IList<IMediaSession> Sessions(){if(owner.RpcFailure)throw new COMException("RPC unavailable",unchecked((int)0x800706BA));return owner.Items.ToList();}public IMediaSession Current(){return owner.Current;}public void Dispose(){owner.Disposed++;}
  }
 }
 internal sealed class SpotifyCheckBackend:IMediaBackend {
  public async Task<IMediaConnection> Connect(){return new Scope(await new WindowsMediaBackend().Connect());}
  sealed class Scope:IMediaConnection {
   readonly IMediaConnection inner;public Scope(IMediaConnection inner){this.inner=inner;}
   public event Action Changed{add{inner.Changed+=value;}remove{inner.Changed-=value;}}
   static bool Match(IMediaSession item){return item!=null&&item.AppId.IndexOf("spotify",StringComparison.OrdinalIgnoreCase)>=0;}
   public IList<IMediaSession> Sessions(){return inner.Sessions().Where(Match).ToList();}public IMediaSession Current(){var current=inner.Current();return Match(current)?current:null;}public void Dispose(){inner.Dispose();}
  }
 }
#if !WINUI
 public sealed partial class AdaWindow {
  public async Task CheckMediaRecovery(List<string> report){
   var brokenOld=new TestMediaSession{Key="stale-instance",AppId="Spotify.exe",Playing=true,EmptyMetadata=true};
   var replacement=new TestMediaSession{Key="healthy-instance",AppId="Spotify.exe",Playing=true,Title="Shared live song"};
   var shared=new TestMediaBackend{Items=new List<IMediaSession>{brokenOld,replacement},Current=brokenOld};
   var clientOne=new MediaService(shared,false,()=>DateTimeOffset.UtcNow);
   using(var clientTwo=new MediaService(shared,false,()=>DateTimeOffset.UtcNow)){
    try{await Task.WhenAll(clientOne.Start(),clientTwo.Start());if(clientOne.Title!="Shared live song"||clientTwo.Title!="Shared live song")throw new Exception("An old current/Playing proxy with empty metadata hid the healthy Spotify session from concurrent clients.");}
    finally{clientOne.Dispose();}
    replacement.Title="Both clients can reconnect";replacement.Signal(MediaSignal.Metadata);await Task.Delay(50);await clientTwo.Reconcile();
    if(clientTwo.Title!="Both clients can reconnect")throw new Exception("Disposing one media client stopped the other client's updates.");
    await clientTwo.Command("toggle");if(replacement.Commands!=1||brokenOld.Commands!=0)throw new Exception("Concurrent client command targeted stale Spotify.");
   }
   if(replacement.Subscribers!=0||shared.Connections!=shared.Disposed)throw new Exception("Concurrent clients leaked their own subscriptions/managers.");
   report.Add("Concurrent clients: both validate healthy Spotify despite stale current/Playing metadata; disposing/reconnecting one does not stop the other; commands use the healthy instance");
   var otherAudio=new TestMediaSession{Key="browser-audio",AppId="firefox.exe",Playing=true,Title="Other application audio"};
   var musicAfterVideo=new TestMediaBackend{Items=new List<IMediaSession>{otherAudio},Current=otherAudio};
   using(var audioFocus=new MediaService(musicAfterVideo,false,()=>DateTimeOffset.UtcNow)){
    await audioFocus.Start();if(audioFocus.Title!=""||otherAudio.Reads!=0)throw new Exception("Non-Spotify audio was read or displayed.");musicAfterVideo.Items.Add(replacement);await audioFocus.Reconcile();
    if(audioFocus.SourceAppId!="Spotify.exe")throw new Exception("Browser audio retained Windows current/preferred focus when Spotify became available and playing.");
    for(int i=0;i<5;i++){musicAfterVideo.Current=otherAudio;await audioFocus.Reconcile();if(audioFocus.SourceAppId!="Spotify.exe"||!audioFocus.Playing)throw new Exception("Another application's audio stole playing Spotify.");}
   }
   report.Add("Audio focus: playing Spotify wins even if browser audio started first; subsequent Windows-current browser changes do not steal it");
   var cachedOld=new TestMediaSession{Key="cached-old",AppId="Spotify.exe",Playing=true,Title="Cached old song"};
   var newlyOpened=new TestMediaSession{Key="new-paused",AppId="Spotify.exe",Playing=false,Title="New instance paused"};
   var staleCurrent=new TestMediaBackend{Items=new List<IMediaSession>{cachedOld},Current=cachedOld};
   using(var instances=new MediaService(staleCurrent,false,()=>DateTimeOffset.UtcNow)){
    await instances.Start();staleCurrent.Items.Add(newlyOpened);await instances.Reconcile();
    if(instances.Title!="New instance paused"||instances.Playing||cachedOld.Subscribers!=0||newlyOpened.Subscribers!=1)throw new Exception("Cached old Playing/current state beat a genuinely new paused instance.");
    await instances.Command("toggle");if(newlyOpened.Commands!=1||cachedOld.Commands!=0)throw new Exception("New paused instance did not receive the command.");
   }
   report.Add("Instance identity: newly discovered healthy paused Spotify replaces older cached Playing/current; subscriptions and commands follow the new instance");
   var instant=DateTimeOffset.UtcNow;var spotify=new TestMediaSession{Key="spotify-1",AppId="Spotify.exe",Playing=true,Title="High Forever"};var browser=new TestMediaSession{Key="browser-1",AppId="firefox.exe",Playing=false,Title=""};var backend=new TestMediaBackend{Items=new List<IMediaSession>{spotify,browser},Current=browser};
   var service=new MediaService(backend,false,()=>instant);int blanks=0;service.Changed+=delegate{if(String.IsNullOrWhiteSpace(service.Title))blanks++;};
   try{
    await service.Start();if(service.Title!="High Forever"||service.SourceAppId!="Spotify.exe")throw new Exception("Paused browser stole playing Spotify at startup.");
    browser.Playing=true;for(int i=0;i<10;i++)await service.Reconcile();if(service.SourceAppId!="Spotify.exe"||spotify.Reads!=1||blanks!=0)throw new Exception("Current-session changes cleared or rebound an unchanged live track.");browser.Playing=false;
    await service.Command("toggle");if(spotify.Commands!=1||browser.Commands!=0)throw new Exception("Transport command targeted Windows current instead of selected music session.");
    backend.RpcFailure=true;await service.Reconcile();if(service.Title!="High Forever"||service.Error.Length==0)throw new Exception("RPC failure discarded the last track.");
    backend.RpcFailure=false;instant=instant.AddSeconds(3);await service.Reconcile();if(service.Title!="High Forever"||service.Error!=""||backend.Connections!=2)throw new Exception("RPC recovery did not recreate the manager.");
    spotify.PlaybackFailure=true;await service.Reconcile();if(service.Title!="High Forever"||service.Error=="")throw new Exception("Healthy browser hid selected-player RPC failure.");spotify.PlaybackFailure=false;await service.Reconcile();if(backend.Connections!=3||service.Error!="")throw new Exception("Selected-player RPC failure did not reconnect.");
    spotify.Title="Next track";spotify.BrokenArtwork=true;instant=instant.AddSeconds(16);await service.Reconcile();if(service.Title!="Next track"||service.Error!="")throw new Exception("Broken artwork discarded valid metadata.");spotify.BrokenArtwork=false;
    spotify.EmptyMetadata=true;instant=instant.AddSeconds(16);await service.Reconcile();if(service.Title!="Next track")throw new Exception("One empty metadata reply erased the song.");spotify.EmptyMetadata=false;instant=instant.AddSeconds(2);await service.Reconcile();
    backend.Items=new List<IMediaSession>{browser};await service.Reconcile();if(service.Title!="Next track"||service.Playing||service.CanPlay)throw new Exception("Missing Spotify was replaced by browser audio or retained live controls.");
    instant=instant.AddSeconds(3);backend.Items.Insert(0,spotify);await service.Reconcile();if(service.Title!="Next track"||service.Error!="")throw new Exception("Session-list recovery left the stale marker.");
    backend.Items.Clear();await service.Reconcile();instant=instant.AddSeconds(9);await service.Reconcile();if(service.Title!="Next track"||service.Playing||service.CanPlay)throw new Exception("Last Spotify track was erased or falsely marked live after closure.");
    backend.Items.Add(spotify);await service.Reconcile();if(service.Title!="Next track")throw new Exception("Reappearing session did not repopulate cleared metadata.");
   }finally{service.Dispose();}
   if(spotify.Subscribers!=0||browser.Subscribers!=0||backend.Disposed!=backend.Connections)throw new Exception("Media recovery leaked subscriptions/managers.");
   var oldSpotify=new TestMediaSession{Key="dead-spotify",AppId="Spotify.exe",Playing=true,Title="Before restart"};var newSpotify=new TestMediaSession{Key="new-spotify",AppId="Spotify.exe",Playing=true,Title="After restart"};var restartBackend=new TestMediaBackend{Items=new List<IMediaSession>{oldSpotify},Current=oldSpotify};
   using(var restarting=new MediaService(restartBackend,false,()=>instant)){
    await restarting.Start();oldSpotify.PlaybackFailure=true;restartBackend.Items.Add(newSpotify);restartBackend.Current=newSpotify;await restarting.Reconcile();if(restarting.Title!="After restart"||restarting.Error!=""||oldSpotify.Subscribers!=0||newSpotify.Subscribers!=1)throw new Exception("Dead old Spotify proxy poisoned its healthy replacement.");
    await restarting.Command("toggle");if(newSpotify.Commands!=1||oldSpotify.Commands!=0)throw new Exception("Restarted player command used the old proxy.");
    oldSpotify.PlaybackFailure=false;restartBackend.Current=oldSpotify;oldSpotify.Title="Stale Windows current";await restarting.Reconcile();if(restarting.Title!="After restart")throw new Exception("An old Windows-current proxy stole the newer validated instance.");
    restartBackend.Items.Clear();await restarting.Reconcile();await restarting.Reconcile();restartBackend.Items.Add(newSpotify);restartBackend.Current=newSpotify;await restarting.Reconcile();if(restarting.Title!="After restart"||restartBackend.Connections<2)throw new Exception("Empty stale manager was never rebuilt after relaunch.");
    var fallback=new TestMediaSession{Key="fallback-video",AppId="firefox.exe",Playing=true,Title="Browser fallback"};restartBackend.Items=new List<IMediaSession>{fallback};restartBackend.Current=fallback;await restarting.Reconcile();if(restarting.SourceAppId!="Spotify.exe"||restarting.Title!="After restart"||restarting.Playing||fallback.Reads!=0)throw new Exception("Browser fallback replaced last Spotify track.");restartBackend.Items.Add(newSpotify);await restarting.Reconcile();if(restarting.Title!="After restart")throw new Exception("Browser fallback permanently stole the restarted preferred player.");
   }
   var hung=new TaskCompletionSource<TrackMetadata>();var stuck=new TestMediaSession{Key="hung",AppId="Spotify.exe",Playing=true,PendingMetadata=()=>hung.Task};var freshSong=new TestMediaSession{Key="fresh",AppId="Spotify.exe",Playing=true,Title="Recovered without waiting"};var stuckBackend=new TestMediaBackend{Items=new List<IMediaSession>{stuck}};
   using(var recovering=new MediaService(stuckBackend,false,()=>instant,TimeSpan.FromMilliseconds(200))){await recovering.Start();if(recovering.Error=="")throw new Exception("A stalled metadata request kept recovery busy forever.");stuckBackend.Items=new List<IMediaSession>{freshSong};stuckBackend.Current=freshSong;await recovering.Reconcile();if(recovering.Title!="Recovered without waiting")throw new Exception("A quarantined call blocked fresh-session recovery.");hung.SetResult(new TrackMetadata{Title="Stale reply",Artist=""});await Task.Delay(30);if(recovering.Title!="Recovered without waiting")throw new Exception("Late old-session metadata overwrote the restarted player.");}
   report.Add("Spotify relaunch: dead + healthy same-app proxies coexist; healthy replacement selected, command targets new instance, old subscriptions removed; cached Playing cannot pin an old instance; stale empty manager rebuilt; stalled call times out and fresh connection recovers before old reply; late old reply rejected");
   var initial=new TestMediaBackend{ConnectFailure=true};using(var recovery=new MediaService(initial,false,()=>instant)){await recovery.Start();initial.ConnectFailure=false;initial.Items.Add(spotify);await recovery.Reconcile();if(recovery.Title!="Next track")throw new Exception("Initial access/RPC failure never retried.");}
   var pending=new TaskCompletionSource<TrackMetadata>();var late=new TestMediaSession{Key="late",AppId="Spotify.exe",Playing=true,PendingMetadata=()=>pending.Task};var lateBackend=new TestMediaBackend{Items=new List<IMediaSession>{late}};var stopping=new MediaService(lateBackend,false,()=>instant);int callbacks=0;stopping.Changed+=delegate{callbacks++;};var operation=stopping.Start();while(late.Reads==0)await Task.Delay(10);stopping.Dispose();int atClose=callbacks;pending.SetResult(new TrackMetadata{Title="Too late",Artist=""});await operation;if(callbacks!=atClose||stopping.Title!="")throw new Exception("Late metadata was published after disposal.");
   report.Add("Media recovery: playing Spotify beats paused Windows-current browser; sticky during browser playback; unchanged selection keeps subscriptions/metadata; commands stay on selected player; injected RPC 0x800706BA reconnects; broken artwork/empty metadata/temporary missing session preserve valid track; closed Spotify retains last track with playback/controls stopped; initial failure retries; disposal rejects late replies; no manager/subscription leaks");
   var hostPanel=new StackPanel();double applied=-1;int writes=0;var slider=AddSlider(hostPanel,"Wheel test",50,delegate(double value){applied=value;writes++;},wheel:true);
   var wheel=new MouseWheelEventArgs(Mouse.PrimaryDevice,Environment.TickCount,120){RoutedEvent=Mouse.PreviewMouseWheelEvent};slider.RaiseEvent(wheel);if(!wheel.Handled||slider.Value!=52)throw new Exception("Slider wheel did not handle +2%.");
   AdjustLevelWheel(slider,-240,false);if(slider.Value!=48)throw new Exception("Slider wheel direction/steps incorrect.");AdjustLevelWheel(slider,120,true);if(slider.Value!=49)throw new Exception("Fine wheel step incorrect.");AdjustLevelWheel(slider,6000,false);if(slider.Value!=100)throw new Exception("Wheel upper bound overflowed.");AdjustLevelWheel(slider,-12000,false);if(slider.Value!=0)throw new Exception("Wheel lower bound overflowed.");var wheelDeadline=DateTime.UtcNow.AddSeconds(2);while(writes==0&&DateTime.UtcNow<wheelDeadline)await Task.Delay(20);await Task.Delay(160);if(writes!=1||applied!=0)throw new Exception("Wheel burst was not debounced into one write: writes="+writes+"; value="+applied);slider.IsEnabled=false;AdjustLevelWheel(slider,120,false);if(slider.Value!=0)throw new Exception("Disabled brightness slider responded to wheel.");
   report.Add("Level wheel: routed event consumed; +/-2% detent; Shift fine 1%; 0..100 bounds; disabled slider ignores; burst debounced into one fake write, no real volume/brightness change");
   await CheckSpotifyHistory(report);
  }
  public async Task MediaCheck(bool spotifyOnly=false){var observed=spotifyOnly?new MediaService(new SpotifyCheckBackend(),true,()=>DateTimeOffset.UtcNow):Media;try{var report=new List<string>();string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"qa");Directory.CreateDirectory(folder);await observed.Start();File.WriteAllText(Path.Combine(folder,"media-check-ready.txt"),observed.SourceAppId+"|"+observed.Playing);for(int i=0;i<10;i++){await Task.Delay(3000);report.Add(DateTimeOffset.Now.ToString("O")+" source="+observed.SourceAppId+" title="+observed.Title+" playing="+observed.Playing+" error="+observed.Error);}report.Add(observed.Diagnostics);File.WriteAllLines(Path.Combine(folder,spotifyOnly?"spotify-live-report.txt":"live-media-report.txt"),report);}finally{if(spotifyOnly)observed.Dispose();Close();}}
 }
#endif
}
