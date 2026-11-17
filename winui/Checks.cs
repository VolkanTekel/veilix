// Integration checks use fake media and ephemeral utility state; no system volume changes.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ada;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Web.Script.Serialization;
namespace Veilix;
public sealed partial class MainWindow {
 readonly bool testing=Environment.GetCommandLineArgs().Contains("--self-test")||Environment.GetCommandLineArgs().Contains("--stability-test");
 static void Check(bool valid,string message){if(!valid)throw new InvalidOperationException(message);}
 async Task RunChecks(){var report=new List<string>();string folder=Path.Combine(AppContext.BaseDirectory,"qa");Directory.CreateDirectory(folder);try{
  Check(!AppWindow.IsShownInSwitchers&&host.UsesToolWindowStyle,"Widget appears in taskbar/Alt+Tab after activation");report.Add("Shell visibility: AppWindow hidden from taskbar/Alt+Tab; TOOLWINDOW set, APPWINDOW cleared after activation.");
  await CheckSectionMenu(report);
  await CheckLocalization(report,folder);
  await CheckCallbackRecovery(report);long allowedFaults=UiFaultBoundary.RecoveredCount;await CheckWindowLayers(report,folder);await CheckDailyFeatures(report,folder);await CheckBluetoothPanel(report,folder);await CheckRecentChanges(report);root.UpdateLayout();Check(shoulders.Count==2,"Missing reverse curves");for(int y=0;y<56;y++)for(int x=0;x<56;x++){var p=new System.Numerics.Vector2((x+.5f)/4,(y+.5f)/4);Check(leftShoulderMask.FillContainsPoint(p)==rightShoulderMask.FillContainsPoint(new System.Numerics.Vector2(14-p.X,p.Y)),"Curves are not mirrors");}var curveLeft=shoulders[0].Grid.TransformToVisual(scene).TransformPoint(new Windows.Foundation.Point());var curveRight=shoulders[1].Grid.TransformToVisual(scene).TransformPoint(new Windows.Foundation.Point());Check(Math.Abs(curveLeft.X+14)<.01&&Math.Abs(curveRight.X-width)<.01&&Math.Abs(curveLeft.Y-curveRight.Y)<.01,"Curve anchoring");Check(shoulders.All(x=>x.Tint.Fill is LinearGradientBrush b&&b.MappingMode==BrushMappingMode.Absolute),"Curve gradient mapping");report.Add("Reverse curves: identical mirrored mask at 3136 sample points; x=-14 / body width; common absolute gradient coordinates.");
  Check(ThemePalette.Presets.Length==62,"Preset count");Check(ThemePalette.Presets.Select(p=>p.Name).Distinct().Count()==62,"Duplicate presets");Check(ThemePalette.Presets.Count(p=>!p.Light)==50&&ThemePalette.Presets.Count(p=>p.Light)==12&&ThemePalette.Presets.All(p=>p.Readable),"Preset text contrast");report.Add("62 distinct themes (50 dark, 12 light): contrast >= 4.5:1 at both endpoints and tile.");
  var json=new JavaScriptSerializer();var settings=json.Deserialize<Settings>(json.Serialize(prefs));settings.Theme="Özel";settings.CustomStops=Enumerable.Range(0,9).Select(i=>new ThemeStop(i%2==0?"#18232D":"#293946",i/8.0)).ToList();var copy=json.Deserialize<Settings>(json.Serialize(settings));Check(copy.CustomStops.Count==9&&copy.CustomStops[4].Offset==.5,"Gradient persistence");Check(GradientModel.Normalize(new[]{new ThemeStop("#112233",1),new ThemeStop("#223344",0)},"#000000","#111111")[0].Color=="#223344","Stop sort");report.Add("9-stop custom theme round-trip; position ordering retained.");
  var tracker=new ConnectionTracker();tracker.Set("headset-a","c",true);tracker.Set("headset-b","c",false);Check(tracker.Connected("c")==true,"Connected sibling incorrectly hidden");tracker.Set("headset-a","c",false);Check(tracker.Connected("c")==false,"Disconnected device retained");tracker.Remove("headset-a");tracker.Remove("headset-b");Check(tracker.Connected("c")==false,"Removed device resurrected from cached PnP");tracker.Set("new-headset","c",true);Check(tracker.Connected("c")==true,"Reconnect blocked");report.Add("Bluetooth connection aggregation: disconnect, removal, reconnect, multiple endpoints.");
  var fake=new TestMediaSession{Key="spotify-first",AppId="Spotify.exe",Title="First",Playing=true};var other=new TestMediaSession{Key="browser",AppId="firefox.exe",Playing=true};var backend=new TestMediaBackend{Items=new(){other,fake},Current=other};string history=Path.Combine(folder,"isolated-last-track.json");if(File.Exists(history))File.Delete(history);
  using(var service=new MediaService(backend,false,()=>DateTimeOffset.UtcNow,TimeSpan.FromMilliseconds(120),history)){
   await service.Start();Check(service.Title=="First"&&service.Playing&&other.Reads==0,"Spotify selection");backend.Items=new(){other};await service.Reconcile();Check(service.Title=="First"&&!service.Playing&&!service.CanPlay,"Offline history");var fresh=new TestMediaSession{Key="spotify-reopened",AppId="Spotify.exe",Title="Reopened",Playing=false};fake.PlaybackFailure=true;backend.Items=new(){fake,fresh,other};await service.Reconcile();Check(service.Title=="Reopened"&&service.CanPlay&&!service.Playing,"Reopened session");fresh.BrokenArtwork=true;fresh.Title="New track";fresh.Signal(MediaSignal.Metadata);await Task.Delay(40);await service.Reconcile();Check(service.Title=="New track","Artwork error discarded track");backend.RpcFailure=true;await service.Reconcile();Check(service.Title=="New track"&&!service.CanPlay,"RPC recovery history");backend.RpcFailure=false;backend.ConnectFailure=true;await service.Reconcile();backend.ConnectFailure=false;fresh.Title="Recovered";fresh.Playing=true;fresh.EmptyMetadata=false;fresh.PendingMetadata=()=>Task.FromResult(new TrackMetadata{Title="Recovered",Artist="Test"});await service.Reconcile();Check(service.Title=="Recovered"&&service.Playing,"Manager reconnect");await service.FlushHistory();
  }
  var saved=new SpotifyHistory(history).Load();Check(saved!=null&&saved.Title=="Recovered","Snapshot read compatibility");report.Add("Spotify-only selection, close/reopen, stale proxy, broken artwork, RPC/manager recovery, persisted last track.");
  var never=new TaskCompletionSource<TrackMetadata>();var hung=new TestMediaSession{Key="hung",AppId="Spotify.exe",Playing=true,PendingMetadata=()=>never.Task};var healthy=new TestMediaSession{Key="healthy",AppId="Spotify.exe",Playing=false,Title="Healthy"};using(var service=new MediaService(new TestMediaBackend{Items=new(){hung,healthy},Current=hung},false,()=>DateTimeOffset.UtcNow,TimeSpan.FromMilliseconds(80))){var watch=Stopwatch.StartNew();await service.Start();Check(service.Title=="Healthy"&&watch.ElapsedMilliseconds<1800,"Bounded hung session recovery");}never.SetResult(new TrackMetadata{Title="Late"});report.Add("Hung metadata bounded; healthy candidate survives.");
  prefs.ReducedMotion=true;foreach(string r in Routes.Concat(new[]{"settings","themes","custom","weather","notifications","compact-layout"})){Navigate(r);root.UpdateLayout();await Task.Delay(30);Check(double.IsFinite(targetHeight)&&targetHeight>=54&&targetHeight<=AvailableHeight+.01,"Page exceeds monitor: "+r);Check(page.Children.Count>0,"Empty page "+r);report.Add(r+": height="+targetHeight.ToString("F0")+", content="+page.DesiredSize.Height.ToString("F0")+", scroll="+scroller.ScrollableHeight.ToString("F0"));}
  foreach(string mode in new[]{"Cam","Şeffaf","Düz","Mika"}){prefs.Material=mode;ApplyTheme();Navigate("media");await Task.Delay(60);Check(glass.Visibility==(mode=="Mika"?Visibility.Visible:Visibility.Collapsed),"Material visibility");}
  foreach(string mode in new[]{"Cam","Mika"}){prefs.Material=mode;ApplyTheme();root.UpdateLayout();await Task.Delay(80);if(mode=="Mika")Check(Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementChildVisual(glass)?.Clip==bodyBackdropClip,"Body material rectangle clip missing: "+mode);else Check(SharedGlass&&unifiedGlass.Visible&&!glassCreationFailed,"Unified glass unavailable");foreach(var shoulder in shoulders)Check(shoulder.Grid.Children.Count==1&&shoulder.Grid.Children[0] is Microsoft.UI.Xaml.Shapes.Path,"A second material remains underneath a curve");}
  foreach(string palette in new[]{"Grafit","İnci"})foreach(string material in new[]{"Cam","Mika","Şeffaf","Düz"}){prefs.Theme=palette;prefs.Material=material;ApplyTheme();root.UpdateLayout();await Task.Delay(40);await CheckRenderedCurvePixels();}
  report.Add("Actual WinUI rendered pixels: outside-curve alpha=0 in dark/light themes and Cam/Mika/Transparent/Solid; curve interiors present.");
  prefs.Material="Cam";prefs.Theme="Grafit";prefs.Tint=.6;ApplyTheme();root.UpdateLayout();Check(SharedGlass&&unifiedGlass.SharedSource,"Body and curves do not share a glass brush");Check(glass.SystemBackdrop==null&&glass.Visibility==Visibility.Collapsed,"A second Cam material remains underneath the tint");var bodyPaint=(LinearGradientBrush)tint.Background;foreach(var curve in shoulders){var paint=(LinearGradientBrush)curve.Tint.Fill;Check(paint.GradientStops.Count==bodyPaint.GradientStops.Count,"Curve gradient count mismatch");for(int i=0;i<paint.GradientStops.Count;i++)Check(paint.GradientStops[i].Color==bodyPaint.GradientStops[i].Color,"Curve has an estimated gray base instead of the body RGBA");}
  Navigate("controls");await Task.Delay(30);Check(unifiedGlass.Visible,"Glass hidden while expanded");prefs.Material="Düz";ApplyTheme();Check(unifiedGlass==null,"Inactive glass host retained");prefs.Material="Cam";ApplyTheme();Check(unifiedGlass.SharedSource&&unifiedGlass.Visible,"Glass failed to recreate on an expanded page");Navigate("");
  report.Add("Cam seam regression: one host-backdrop brush for body + both curves, identical RGBA gradient stops (no gray color estimate), no secondary SystemBackdropElement, dispose/recreate across material switches.");
  report.Add("Cam/Mika: 2 extra rectangular shoulder materials removed. Each curve has exactly one native Path fill. Cam uses one common backdrop; Mika retains the rounded rectangle material clip.");
  themeFilter="Tümü";Navigate("themes");root.UpdateLayout();Check(themeCards.Count==62,"Gallery count");double cardWidth=themeCards[0].ActualWidth;foreach(var card in themeCards){Check(Math.Abs(card.ActualWidth-cardWidth)<.05&&Math.Abs(card.ActualHeight-104)<.05,"Unequal theme card size: "+card.ActualWidth+"x"+card.ActualHeight+" first="+cardWidth);var stack=(Grid)card.Content;var swatch=(Border)stack.Children[0];var label=(TextBlock)stack.Children[1];Check(Math.Abs(swatch.ActualHeight-46)<=.5/host.Dpi+.01&&Math.Abs(swatch.ActualWidth-((Border)((Grid)themeCards[0].Content).Children[0]).ActualWidth)<.05&&swatch.ActualWidth>=cardWidth-24,$"Unequal color sample size: {swatch.ActualWidth}x{swatch.ActualHeight}; first={((Border)((Grid)themeCards[0].Content).Children[0]).ActualWidth}; card={cardWidth}; scale={prefs.Scale}; dpi={host.Dpi}");Check(label.FontSize==14&&ThemePalette.Contrast("#F5F7FB","#252C35")>=7,"Gallery label contrast");}
  foreach(string name in new[]{"İnci","Nane","Lavanta Açığı"}){prefs.Theme=name;ApplyTheme();themeFilter="Açık";Navigate("themes");root.UpdateLayout();Check(root.RequestedTheme==ElementTheme.Light&&themeCards.Count==12,"Light palette UI/filter");Check(((SolidColorBrush)date.Foreground).Color==C("#242C38"),"Light header foreground");}
  try{await RenderGalleryPreview(Path.Combine(folder,"gallery-light.png"));report.Add("Native WinUI gallery control preview rendered (not a desktop/glass capture).");}catch(Exception ex){report.Add("Gallery preview unavailable: "+ex.GetType().Name);}
  report.Add("Gallery: equal-width 104px cards; full-width 46px color samples; 14px semibold names with >7:1 contrast. Light theme controls/header and 12-item filter checked.");prefs.Theme="Grafit";ApplyTheme();
  prefs.ReducedMotion=false;Navigate("");await Task.Delay(50);Navigate("controls");await Task.Delay(70);Navigate("");await Task.Delay(400);var settle=Stopwatch.StartNew();while(resizing&&settle.ElapsedMilliseconds<1600)await Task.Delay(25);Check(Math.Abs(surface.Height-54)<.01&&Math.Abs(clipGeometry.Size.Y-78)<.01&&shadow.Opacity==0&&body.Visibility==Visibility.Collapsed&&!resizing,"Interrupted resize final state: surface="+surface.Height+" clip="+clipGeometry.Size+" shadow="+shadow.Opacity+" body="+body.Visibility+" resizing="+resizing+" route="+route);if(SharedGlass)Check(Math.Abs(unifiedGlass.ClipSize.Y-78)<.01,"Glass did not settle with the foreground");report.Add("Interrupted expansion/collapse: 54px final foreground AND glass, shadow zero.");
  prefs.ReducedMotion=true;SetHovered(false);root.UpdateLayout();var before=date.TransformToVisual(header).TransformPoint(new Windows.Foundation.Point());SetHovered(true);root.UpdateLayout();var after=date.TransformToVisual(header).TransformPoint(new Windows.Foundation.Point());Check(Math.Abs(before.X-after.X)<.01,"Hover moved date/weather");foreach(double dpi in new[]{1.0,1.25,1.5,1.75}){double px=Math.Round(3*dpi);Check(px>=2,"Wave width");}report.Add("Clock hover leaves date/weather coordinates fixed; equal-width wave columns.");
  await CheckAnimationInterruptions(report);
  await CheckClockWave(report);Check(!AppWindow.IsShownInSwitchers&&host.UsesToolWindowStyle,"Navigation/material switches restored taskbar entry");
  if(Environment.GetCommandLineArgs().Contains("--stability-test"))await CheckStability(report);Check(UiFaultBoundary.RecoveredCount==allowedFaults,"Unexpected recovered UI error during checks");Check(VisualTreeHelper.GetChildrenCount(root)>0,"No WinUI tree");Check(!AppDomain.CurrentDomain.GetAssemblies().Any(x=>new[]{"PresentationCore","PresentationFramework","WindowsBase"}.Contains(x.GetName().Name)),"WPF assembly loaded in WinUI");report.Add("UI and text: WinUI 3. Artwork uses bounded PNG data; no WPF assemblies loaded.");File.WriteAllLines(Path.Combine(folder,"report.txt"),report);File.WriteAllText(Path.Combine(folder,"FAILED.txt"),"");Environment.ExitCode=0;
 }catch(Exception ex){File.WriteAllText(Path.Combine(folder,"FAILED.txt"),ex.ToString());File.WriteAllLines(Path.Combine(folder,"report.txt"),report);Environment.ExitCode=1;}finally{Close();}}
 bool benchmarking,perfQuiet,perfForceWave;int perfWaveTicks;
 async Task RunBenchmark(){
  string folder=Path.Combine(AppContext.BaseDirectory,"qa");Directory.CreateDirectory(folder);var phases=new List<object>();var lines=new List<string>();await Task.Delay(3000);occlusionTimer.Stop();host.WatchOutside(false);SetHidden(false);SetHovered(false);benchmarking=true;
  try{
   // Reproduce the ordinary settings/gallery visit before comparing memory.
   Navigate("themes");await Task.Delay(500);Navigate("custom");await Task.Delay(300);Navigate("");await Task.Delay(1800);
   foreach(var phase in new[]{new[]{"compact",""},new[]{"wave_test",""},new[]{"media","media"},new[]{"controls","controls"}}){
    perfQuiet=false;perfForceWave=phase[0]=="wave_test";Navigate(phase[1]);UpdateClock();headerMarquee?.Sync();await Task.Delay(1200);
    var start=DateTimeOffset.UtcNow;TimeSpan cpu=PerformanceMetrics.Cpu();var sw=Stopwatch.StartNew();long ws=0,privateBytes=0,managed=0;int samples=20;
    bool playing=media.Playing,visualizer=peakTimer.IsRunning;int waveStart=perfWaveTicks;
    for(int i=0;i<samples;i++){await Task.Delay(500);var memory=PerformanceMetrics.Memory();ws+=memory.WorkingSet;privateBytes+=memory.Private;managed+=GC.GetTotalMemory(false);}
    double percent=(PerformanceMetrics.Cpu()-cpu).TotalSeconds/sw.Elapsed.TotalSeconds/Environment.ProcessorCount*100;
    var record=new{Phase=phase[0],StartUtc=start,EndUtc=DateTimeOffset.UtcNow,Seconds=sw.Elapsed.TotalSeconds,CPU=percent,WS=ws/(double)samples/1048576,Private=privateBytes/(double)samples/1048576,Managed=managed/(double)samples/1048576,Playing=playing,Visualizer=visualizer,Material=prefs.Material,FrameLimit=prefs.FrameLimit,WaveUpdates=(perfWaveTicks-waveStart)/sw.Elapsed.TotalSeconds,SyntheticWave=perfForceWave};phases.Add(record);
    lines.Add(phase[0]+": CPU="+percent.ToString("F3")+"%; WS="+record.WS.ToString("F1")+" MiB; private="+record.Private.ToString("F1")+" MiB; managed="+record.Managed.ToString("F1")+" MiB; playing="+playing+"; visualizer="+visualizer);
   }
   File.WriteAllLines(Path.Combine(folder,"benchmark.txt"),lines);File.WriteAllText(Path.Combine(folder,"benchmark.json"),System.Text.Json.JsonSerializer.Serialize(phases,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
  }finally{perfQuiet=false;perfForceWave=false;benchmarking=false;Close();}
 }
}

















