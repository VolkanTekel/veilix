// GPL-3.0-or-later. Per-pixel transparency and native/layered window handoff.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Ada {
 public sealed partial class AdaWindow {
  static bool UsesLayeredSurface(string material){return material=="Cam"||material=="Şeffaf"||material=="Düz";}
  bool replacingWindow;
  AdaWindow ChangeMaterial(string material){
   Preferences.Material=material;
   if(UsesLayeredSurface(material)==AllowsTransparency){ApplyTheme();ApplyMaterial();Save();RenderRoute(false);return this;}
   // AllowsTransparency cannot change after SourceInitialized. Replace only the
   // host window, preserving the settings instance, navigation and media view.
   var next=new AdaWindow(testing,inspection,Preferences,tools);
   next.route=route;next.settingsPage=settingsPage;next.headerHovered=headerHovered;foreach(var item in occludingWindows)next.occludingWindows[item.Key]=item.Value;
   next.Media.Title=Media.Title;next.Media.Artist=Media.Artist;next.Media.Cover=Media.Cover;next.Media.SourceAppId=Media.SourceAppId;
   next.Media.Playing=Media.Playing;next.Media.Position=Media.Position;next.Media.Duration=Media.Duration;
   next.Media.CanPlay=Media.CanPlay;next.Media.CanNext=Media.CanNext;next.Media.CanPrevious=Media.CanPrevious;next.Media.CanSeek=Media.CanSeek;
   next.weather.City=weather.City;next.weather.Summary=weather.Summary;next.weather.Description=weather.Description;next.weather.ConditionText=weather.ConditionText;next.weather.Code=weather.Code;next.weather.IsDay=weather.IsDay;next.weather.Details=weather.Details;next.weather.Updated=weather.Updated;next.weather.Stale=weather.Stale;
   next.UpdateCompact();
   replacingWindow=true;
   bool mainWindow=Application.Current.MainWindow==this;
   try{next.Show();if(mainWindow)Application.Current.MainWindow=next;}
   catch{replacingWindow=false;next.Close();throw;}
   Close();return next;
  }
  async Task WaitForReveal(){var deadline=DateTime.UtcNow.AddSeconds(3);while((revealRunning||edgeMotionRunning||animatedBody!=null)&&DateTime.UtcNow<deadline)await Task.Delay(25);if(revealRunning||edgeMotionRunning||edgeFrame!=null||reveal.HasAnimatedProperties||bodyReveal.HasAnimatedProperties||topReveal.HasAnimatedProperties||animatedBody!=null)throw new Exception("Animation remained active after the transition.");}
  async Task CheckTransparency(string output,List<string> report){
   var preferences=new Settings{Material="Şeffaf",Tint=.5,ReducedMotion=true,DisplayId=selected.Id};
   var probe=new AdaWindow(true,false,preferences);var originalMain=Application.Current.MainWindow;
   try{
    probe.Show();probe.Media.Title=Media.Title;probe.Media.Artist=Media.Artist;probe.Media.Cover=Media.Cover;
    probe.Media.Duration=Media.Duration;probe.Media.Position=Media.Position;
    foreach(string destination in new[]{"","media","settings"}){
     probe.Navigate(destination);await probe.Dispatcher.InvokeAsync(delegate{probe.UpdateLayout();},DispatcherPriority.Render);await Task.Delay(150);
     if(!probe.AllowsTransparency||!Native.IsLayered(probe))throw new Exception("Transparent mode has no layered native window.");
     double dpi=probe.WindowDpi;int width=(int)Math.Ceiling(probe.ActualWidth*dpi),height=(int)Math.Ceiling(probe.ActualHeight*dpi);
     var bitmap=new RenderTargetBitmap(width,height,96*dpi,96*dpi,PixelFormats.Pbgra32);bitmap.Render(probe);
     var pixels=new byte[width*height*4];bitmap.CopyPixels(pixels,width*4,0);
     int left=pixels[((height-1)*width)*4+3],right=pixels[((height-1)*width+width-1)*4+3];
     int body=pixels[((Math.Min(height-1,(int)(32*probe.EffectiveScale*dpi)))*width+(int)((NotchShoulder+2)*probe.EffectiveScale*dpi))*4+3];
     if(left!=0||right!=0||body<100||body>150)throw new Exception("Transparent alpha failed: corners="+left+","+right+"; body="+body);
     string name=destination==""?"idle":destination;
     var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(Path.Combine(output,"transparent-"+name+".png")))png.Save(file);
     report.Add("Transparent "+name+": native layered=True; corner alpha="+left+","+right+"; body alpha="+body+"/255 at 50% opacity");
    }
    probe.settingsPage="Görünüm";probe.Navigate("settings");Application.Current.MainWindow=probe;
    foreach(string material in new[]{"Cam","Şeffaf","Düz","Şeffaf"}){
     var previous=probe;probe=probe.ChangeMaterial(material);await Task.Delay(150);
     if(!probe.IsVisible||Application.Current.MainWindow!=probe||probe.route!="settings"||probe.settingsPage!="Görünüm"||probe.Preferences!=preferences)throw new Exception("Surface switch lost window state.");
     if(Native.IsLayered(probe)!=UsesLayeredSurface(material))throw new Exception("Native transparency mode did not change.");
     if(previous!=probe&&!previous.closing)throw new Exception("Old window remained alive after surface switch.");
     report.Add("Surface switch "+material+": visible; settings page retained; layered="+Native.IsLayered(probe));
    }
    probe.Navigate("");preferences.ReducedMotion=false;
    probe.Navigate("media");await Task.Delay(60);probe.Navigate("");await Task.Delay(40);probe.Navigate("media");await probe.WaitForReveal();
    report.Add("Transparent interrupted reveal: completed; mask animations removed="+(!probe.reveal.HasAnimatedProperties&&!probe.topReveal.HasAnimatedProperties));
    await probe.CheckEdgeSymmetry(output,report);
   }finally{Application.Current.MainWindow=originalMain;probe.Close();}
  }
 }
}
