// GPL-3.0-or-later. Spotify-only selection, offline presentation and persistence.
using System;using System.Collections.Generic;using System.IO;using System.Threading.Tasks;using System.Windows;using System.Windows.Input;using System.Windows.Media;using System.Windows.Media.Imaging;
namespace Ada {
 public sealed partial class AdaWindow {
  async Task CheckSpotifyHistory(List<string> report){
   string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"qa","history-"+Guid.NewGuid().ToString("N"));string path=Path.Combine(folder,"spotify-last-track.json");
   var picture=BitmapSource.Create(1,1,96,96,PixelFormats.Bgra32,null,new byte[]{50,130,210,255},4);picture.Freeze();byte[] bytes;var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(picture));using(var stream=new MemoryStream()){encoder.Save(stream);bytes=stream.ToArray();}
   var spotify=new TestMediaSession{Key="history-live",AppId="Spotify.exe",Playing=true,Title="Last Spotify song",Artist="Spotify artist",PendingMetadata=()=>Task.FromResult(new TrackMetadata{Title="Last Spotify song",Artist="Spotify artist",Artwork=()=>Task.FromResult(bytes)})};
   var browser=new TestMediaSession{Key="browser",AppId="firefox.exe",Playing=true,Title="Other audio"};var platform=new TestMediaBackend{Items=new List<IMediaSession>{browser,spotify},Current=browser};var clock=DateTimeOffset.UtcNow;
   var service=new MediaService(platform,false,()=>clock,null,path);
   try{
    await service.Start();await service.FlushHistory();if(service.Title!="Last Spotify song"||service.Cover==null||browser.Reads!=0)throw new Exception("Spotify-only selection/cover snapshot failed.");var artwork=service.Cover;
    spotify.Playing=false;await service.Reconcile();if(service.Playing||service.Title!="Last Spotify song"||service.Cover!=artwork||!service.CanPlay)throw new Exception("Paused Spotify lost its title/cover or live resume control.");
    platform.Items=new List<IMediaSession>{browser};await service.Reconcile();clock=clock.AddDays(1);await service.Reconcile();if(service.Title!="Last Spotify song"||service.Cover!=artwork||service.Playing||service.CanPlay||service.CanSeek||browser.Reads!=0)throw new Exception("Closed Spotify was erased, replaced or reported as playing.");
    await service.Command("toggle");if(browser.Commands!=0||spotify.Commands!=0)throw new Exception("Offline history sent a transport command.");
    Preferences.ReducedMotion=true;Media.Title=service.Title;Media.Artist=service.Artist;Media.Cover=service.Cover;Media.SourceAppId=service.SourceAppId;Media.Playing=false;Navigate("");SetHeaderHovered(false);UpdateLayout();
    if(miniTitle.Text!=service.Title||miniArtist.Text!=service.Artist||miniCover.Visibility!=Visibility.Visible||miniTitle.IsScrolling)throw new Exception("Compact view hid or animated the paused last track.");
    header.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonUpEvent});if(route!="media")throw new Exception("Cached song header did not open the media panel.");
   }finally{service.Dispose();}await service.FlushHistory();
   try{
    var idle=new TestMediaBackend{Items=new List<IMediaSession>{browser},Current=browser};var restored=new MediaService(idle,false,()=>clock,null,path);using(restored){await restored.Start();if(restored.Title!="Last Spotify song"||restored.Artist!="Spotify artist"||restored.Cover==null||restored.Playing||restored.CanPlay)throw new Exception("Last track did not survive app restart with Spotify closed.");}await restored.FlushHistory();
    // Check malformed and unrelated-source cache data without touching user data.
    File.WriteAllText(path,"{broken");using(var broken=new MediaService(idle,false,()=>clock,null,path)){await broken.Start();if(broken.Title!="")throw new Exception("Malformed history produced a track.");}
    File.WriteAllText(path,"{\"Version\":1,\"Title\":\"Browser title\",\"AppId\":\"firefox.exe\"}");using(var unrelated=new MediaService(idle,false,()=>clock,null,path)){await unrelated.Start();if(unrelated.Title!="")throw new Exception("Foreign-source cache was accepted.");}
   }finally{if(File.Exists(path))File.Delete(path);if(File.Exists(path+".tmp"))File.Delete(path+".tmp");if(Directory.Exists(folder)&&Directory.GetFileSystemEntries(folder).Length==0)Directory.Delete(folder);}
   report.Add("Spotify history: foreign audio never queried/displayed; pause preserves title/artist/cover with live resume; closed Spotify retains last song after one day, stops progress and disables commands; header keeps artwork and opens media; single local snapshot restores title/artist/cover after app restart; malformed/foreign-source history ignored");
  }
 }
}
