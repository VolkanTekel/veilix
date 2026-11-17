// GPL-3.0-or-later. Windows media adapter, injectable for fault tests.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Control;
using Windows.Storage.Streams;
namespace Ada {
 internal enum MediaSignal {Metadata,Playback,Timeline}
 internal sealed class PlaybackState {public bool Playing,CanPlay,CanNext,CanPrevious,CanSeek;}
 internal sealed class TimelineState {public TimeSpan Position,Duration;public DateTimeOffset Updated;}
 internal sealed class TrackMetadata {public string Title,Artist;public Func<Task<byte[]>> Artwork;}
 internal interface IMediaSession {
  object Key{get;}string AppId{get;}event Action<MediaSignal> Changed;
  PlaybackState Playback();TimelineState Timeline();Task<TrackMetadata> Metadata();Task<bool> Command(string command,long ticks);
 }
 internal interface IMediaConnection:IDisposable {event Action Changed;IList<IMediaSession> Sessions();IMediaSession Current();}
 internal interface IMediaBackend {Task<IMediaConnection> Connect();}
 internal static class MediaAwait {
  public static async Task<T> Wait<T>(IAsyncOperation<T> operation){
   var result=new TaskCompletionSource<T>();operation.Completed=(op,status)=>{try{if(status==AsyncStatus.Canceled)result.TrySetCanceled();else if(status==AsyncStatus.Error)result.TrySetException(op.ErrorCode);else result.TrySetResult(op.GetResults());}catch(Exception ex){result.TrySetException(ex);}finally{try{op.Close();}catch{}}};
   if(await Task.WhenAny(result.Task,Task.Delay(4000))!=result.Task){try{operation.Cancel();}catch{}throw new TimeoutException("Windows medya yanıtı zaman aşımına uğradı.");}return await result.Task;
  }
 }
 internal sealed class WindowsMediaBackend:IMediaBackend {
  public async Task<IMediaConnection> Connect(){return new WindowsMediaConnection(await MediaAwait.Wait(GlobalSystemMediaTransportControlsSessionManager.RequestAsync()));}
 }
 internal sealed class WindowsMediaConnection:IMediaConnection {
  readonly GlobalSystemMediaTransportControlsSessionManager manager;public event Action Changed;
  public WindowsMediaConnection(GlobalSystemMediaTransportControlsSessionManager manager){this.manager=manager;manager.CurrentSessionChanged+=CurrentChanged;manager.SessionsChanged+=SessionsChanged;}
  void CurrentChanged(GlobalSystemMediaTransportControlsSessionManager s,CurrentSessionChangedEventArgs e){Notify();}
  void SessionsChanged(GlobalSystemMediaTransportControlsSessionManager s,SessionsChangedEventArgs e){Notify();}
  void Notify(){var handler=Changed;if(handler!=null)handler();}
  static bool SpotifyRunning(){int desktop=Process.GetCurrentProcess().SessionId;bool found=false;foreach(var process in Process.GetProcessesByName("Spotify")){try{if(process.SessionId==desktop)found=true;}finally{process.Dispose();}}return found;}
  public IList<IMediaSession> Sessions(){var result=new List<IMediaSession>();Exception failed=null;bool spotifyRunning=SpotifyRunning();foreach(var item in manager.GetSessions())try{var wrapped=new WindowsMediaSession(item);if(!spotifyRunning&&wrapped.AppId.IndexOf("spotify",StringComparison.OrdinalIgnoreCase)>=0)continue;result.Add(wrapped);}catch(Exception ex){failed=ex;}if(result.Count==0&&failed!=null)throw failed;return result;}
  public IMediaSession Current(){var active=manager.GetCurrentSession();return active==null?null:new WindowsMediaSession(active);}
  public void Dispose(){manager.CurrentSessionChanged-=CurrentChanged;manager.SessionsChanged-=SessionsChanged;}
 }
 internal sealed class WindowsMediaSession:IMediaSession {
  readonly GlobalSystemMediaTransportControlsSession session;readonly string appId;Action<MediaSignal> listeners;
  public WindowsMediaSession(GlobalSystemMediaTransportControlsSession session){this.session=session;appId=session.SourceAppUserModelId??"";}
  public object Key{get{return session;}}public string AppId{get{return appId;}}
  public event Action<MediaSignal> Changed {
   add{if(listeners==null){session.MediaPropertiesChanged+=PropertiesChanged;session.PlaybackInfoChanged+=PlaybackChanged;session.TimelinePropertiesChanged+=TimelineChanged;}listeners+=value;}
   remove{listeners-=value;if(listeners==null){session.MediaPropertiesChanged-=PropertiesChanged;session.PlaybackInfoChanged-=PlaybackChanged;session.TimelinePropertiesChanged-=TimelineChanged;}}
  }
  void PropertiesChanged(GlobalSystemMediaTransportControlsSession s,MediaPropertiesChangedEventArgs e){Notify(MediaSignal.Metadata);}
  void PlaybackChanged(GlobalSystemMediaTransportControlsSession s,PlaybackInfoChangedEventArgs e){Notify(MediaSignal.Playback);}
  void TimelineChanged(GlobalSystemMediaTransportControlsSession s,TimelinePropertiesChangedEventArgs e){Notify(MediaSignal.Timeline);}
  void Notify(MediaSignal signal){var handler=listeners;if(handler!=null)handler(signal);}
  public PlaybackState Playback(){var p=session.GetPlaybackInfo();return new PlaybackState{Playing=p.PlaybackStatus==GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,CanPlay=p.Controls.IsPlayPauseToggleEnabled,CanNext=p.Controls.IsNextEnabled,CanPrevious=p.Controls.IsPreviousEnabled,CanSeek=p.Controls.IsPlaybackPositionEnabled};}
  public TimelineState Timeline(){var t=session.GetTimelineProperties();return new TimelineState{Position=t.Position,Duration=t.EndTime-t.StartTime,Updated=t.LastUpdatedTime};}
  public async Task<TrackMetadata> Metadata(){var p=await MediaAwait.Wait(session.TryGetMediaPropertiesAsync());return new TrackMetadata{Title=p.Title??"",Artist=p.Artist??"",Artwork=p.Thumbnail==null?null:(Func<Task<byte[]>>)(async delegate{
   using(var stream=await MediaAwait.Wait(p.Thumbnail.OpenReadAsync())){if(stream.Size==0||stream.Size>8*1024*1024)return null;using(var reader=new DataReader(stream)){await MediaAwait.Wait(reader.LoadAsync((uint)stream.Size));var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);return bytes;}}
  })};}
  public async Task<bool> Command(string command,long ticks){if(command=="toggle")return await MediaAwait.Wait(session.TryTogglePlayPauseAsync());if(command=="next")return await MediaAwait.Wait(session.TrySkipNextAsync());if(command=="previous")return await MediaAwait.Wait(session.TrySkipPreviousAsync());if(command=="seek")return await MediaAwait.Wait(session.TryChangePlaybackPositionAsync(ticks));return false;}
 }
}
