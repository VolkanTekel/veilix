// GPL-3.0-or-later. Real-window and hover regressions, used only by --self-test.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Ada {
 public static class InteractionFixture {
  public static void Run(string[] args){int index;var displays=Native.Displays();if(args.Length<2||!Int32.TryParse(args[1],out index)||index>=displays.Count)return;
   var window=new Window{Title="Ada overlap test",Width=440,Height=180,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,Background=Brushes.DimGray};var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(4)};timer.Tick+=delegate{timer.Stop();window.Close();};window.Loaded+=delegate{Native.Position(window,displays[index]);window.Activate();Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"qa"));File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"qa","occlusion-fixture.txt"),Process.GetCurrentProcess().Id+"|"+new WindowInteropHelper(window).Handle.ToInt64());timer.Start();};new Application().Run(window);
  }
 }
 public sealed partial class AdaWindow {
  async Task CheckHoverFeatures(string output,List<string> report){
   Preferences.ReducedMotion=true;Preferences.ShowClock=true;Media.Playing=true;Media.Title="Uzun medya adı — saatten kalan alan bu başlığa ayrılıyor";Navigate("");SetHeaderHovered(true);await Capture(output,"hover-clock-on");double titleWidth=miniTitle.ActualWidth;
   SetHeaderHovered(false);await Capture(output,"idle-media-wide");double gained=miniTitle.ActualWidth-titleWidth;if(clock.Visibility!=Visibility.Collapsed||Math.Abs(gained)>.1||audioBars.Visibility!=Visibility.Visible)throw new Exception("Hover clock changed the reserved audio meter slot: "+gained);report.Add("Hover clock: hidden at idle; audio-meter slot retained; title delta "+gained.ToString("F1")+" logical units; visible on hover");
   SetHeaderHovered(true);if(clock.Visibility!=Visibility.Visible)throw new Exception("Clock failed to appear on hover.");SetHeaderHovered(false);
   Preferences.ReducedMotion=false;SetHeaderHovered(true);await Task.Delay(45);if(!clockMotionRunning||clock.Opacity<=0||clock.Opacity>=1)throw new Exception("Clock entrance did not fade.");await WaitForClockMotion();
   SetHeaderHovered(false);await Task.Delay(45);var clockShift=clock.RenderTransform as TranslateTransform;if(!clockMotionRunning||clock.Visibility!=Visibility.Visible||clock.Opacity>=1||clockShift==null||clockShift.Y>=0)throw new Exception("Clock exit was collapsed before animating.");await WaitForClockMotion();if(clock.Visibility!=Visibility.Collapsed||header.ColumnDefinitions[1].Width.Value!=104)throw new Exception("Clock exit did not release its slot.");
   SetHeaderHovered(true);await Task.Delay(45);SetHeaderHovered(false);await Task.Delay(35);SetHeaderHovered(true);await WaitForClockMotion();if(clock.Visibility!=Visibility.Visible||clock.Opacity!=1||clock.HasAnimatedProperties||clockMotionRunning)throw new Exception("Clock hover reversal did not settle.");Preferences.ReducedMotion=true;SetHeaderHovered(false);if(clockMotionRunning||clock.Visibility!=Visibility.Collapsed)throw new Exception("Reduced motion did not skip clock effects.");report.Add("Clock motion: fade/slide in and out; audio meter restored after exit; rapid reversal settled; reduced motion respected");
   Preferences.ReducedMotion=false;Navigate("media");await WaitForReveal();Navigate("controls");await Task.Delay(45);if(animatedBody==null)throw new Exception("Menu switch had no content motion.");var shift=content.RenderTransform as TranslateTransform;if(shift==null||(Math.Abs(shift.X)<.01&&Math.Abs(shift.Y)<.01))throw new Exception("Menu slide did not move.");await WaitForReveal();if(content.RenderTransform.HasAnimatedProperties||content.Opacity!=1)throw new Exception("Menu motion did not settle.");
   Navigate("settings");await WaitForReveal();settingsPage="Görünüm";RenderRoute(true);await Task.Delay(35);if(animatedBody==null)throw new Exception("Settings subpage had no motion.");await WaitForReveal();report.Add("Menu and settings-page transitions: fade + slide; settled; no animated scaling");
   Preferences.Gradient=false;var expected=ThemeSwatch(ThemePalette.Presets[0]);expected.Measure(new Size(29,29));expected.Arrange(new Rect(0,0,29,29));expected.UpdateLayout();var bitmap=new RenderTargetBitmap(29,29,96,96,PixelFormats.Pbgra32);bitmap.Render(expected);var basePixels=new byte[29*29*4];bitmap.CopyPixels(basePixels,29*4,0);
   foreach(var theme in ThemePalette.Presets){if(theme.Light)throw new Exception("Light preset remained.");Preferences.Theme=theme.Name;ApplyTheme();var swatch=ThemeSwatch(ThemePalette.Presets[0]);swatch.Measure(new Size(29,29));swatch.Arrange(new Rect(0,0,29,29));swatch.UpdateLayout();var image=new RenderTargetBitmap(29,29,96,96,PixelFormats.Pbgra32);image.Render(swatch);var pixels=new byte[29*29*4];image.CopyPixels(pixels,29*4,0);if(!pixels.SequenceEqual(basePixels))throw new Exception("Swatch changed with selected theme.");}
   Preferences.Gradient=true;Preferences.Theme="Grafit";ApplyTheme();RenderRoute(false);await Capture(output,"dark-themes-swatches");report.Add("Theme swatches: identical rendered pixels across all "+ThemePalette.Presets.Length+" dark presets; gradient independent of global switch");
   Navigate("");SetHeaderHovered(false);var area=HeaderScreenBounds();var separate=new Native.Rect{Left=area.Right+10,Right=area.Right+150,Top=area.Top,Bottom=area.Bottom};if(Native.Overlaps(area,separate))throw new Exception("Non-overlapping window matched.");
   int displayIndex=displays.IndexOf(selected);string readyFile=Path.Combine(output,"occlusion-fixture.txt");if(File.Exists(readyFile))File.Delete(readyFile);var start=new ProcessStartInfo(System.Reflection.Assembly.GetExecutingAssembly().Location,"--occlusion-fixture "+displayIndex){UseShellExecute=false};using(var fixture=Process.Start(start)){
    IntPtr trackedHandle=IntPtr.Zero;Native.Rect foreground=new Native.Rect();bool covered=false,wasForeground=false;var deadline=DateTime.UtcNow.AddSeconds(3);while(DateTime.UtcNow<deadline){if(File.Exists(readyFile)){var values=File.ReadAllText(readyFile).Split('|');if(values.Length==2&&Int32.Parse(values[0])==fixture.Id){var handle=new IntPtr(Int64.Parse(values[1]));wasForeground=Native.GetForegroundWindow()==handle;if(Native.ForeignWindowBounds(handle,new WindowInteropHelper(this).Handle,out foreground)&&Native.Overlaps(area,foreground)){trackedHandle=handle;covered=true;break;}}}await Task.Delay(60);}
    if(!covered)throw new Exception("Foreign visible fixture did not overlap header. Bounds="+foreground.Left+","+foreground.Top+","+foreground.Right+","+foreground.Bottom+"; header="+area.Left+","+area.Top+","+area.Right+","+area.Bottom);
    report.Add("Foreign native overlap fixture: visible=True; foreground="+wasForeground+" (Windows may deny focus to automated launches)");
    UpdateOcclusionTracking(trackedHandle);await Task.Delay(70);Native.Rect midHide;Native.GetWindowRect(new WindowInteropHelper(this).Handle,out midHide);if(!edgeMotionRunning||surface.Visibility!=Visibility.Visible||midHide.Top>=selected.Bounds.Top)throw new Exception("Edge retreat skipped its visible movement.");await WaitForReveal();await Capture(output,"edge-tucked");if(!edgeHidden||Visibility!=Visibility.Visible||Height>12*EffectiveScale||edgeHandle.Visibility!=Visibility.Visible)throw new Exception("Auto-hide left no recoverable handle.");
    UpdateOcclusionTracking(IntPtr.Zero);UpdateOcclusionTracking(new WindowInteropHelper(this).Handle);if(!edgeHidden||occludingWindows.Count!=1)throw new Exception("Changing focus discarded the still-open blocking window.");report.Add("Sticky overlap: changing foreground candidate retained the original live blocking window");
    HoverEntered();await WaitForReveal();if(edgeHidden||clock.Visibility!=Visibility.Visible||Height<(HeaderHeight-2)*EffectiveScale)throw new Exception("Hover did not recover the widget.");
    SetHeaderHovered(false);UpdateOcclusionTracking(IntPtr.Zero);await WaitForReveal();if(!edgeHidden)throw new Exception("Widget failed to tuck again after leaving.");
    await Task.Run(delegate{fixture.WaitForExit(6000);});if(!fixture.HasExited){fixture.Kill();throw new Exception("Test fixture did not close.");}
   }
   UpdateOcclusionTracking(IntPtr.Zero);await WaitForReveal();if(edgeHidden||occludingWindows.Count!=0)throw new Exception("Widget remained tucked after blocker closed.");report.Add("Auto-hide: real foreign window bounds overlapped selected screen; focus changes retained hiding; hover restored; leave re-tucked; closing blocker restored");
   await CheckEdgeSymmetry(output,report);
   Preferences.ReducedMotion=true;SetHeaderHovered(false);Navigate("");
  }
  async Task WaitForClockMotion(){var deadline=DateTime.UtcNow.AddSeconds(2);while(clockMotionRunning&&DateTime.UtcNow<deadline)await Task.Delay(20);if(clockMotionRunning)throw new Exception("Clock animation did not finish.");}
  async Task CheckEdgeSymmetry(string output,List<string> report){
   Preferences.ReducedMotion=false;Navigate("");await WaitForReveal();SetHeaderHovered(false);
   SetEdgeHidden(true);await Task.Delay(70);Native.Rect mid;Native.GetWindowRect(new WindowInteropHelper(this).Handle,out mid);if(!edgeMotionRunning||mid.Top>=selected.Bounds.Top||surface.Visibility!=Visibility.Visible)throw new Exception("Retreat had no intermediate native position.");await WaitForReveal();int hideFrames=edgeMotionFrames;if(hideFrames<4)throw new Exception("Retreat had too few movement steps.");
   Navigate("");if(!edgeHidden)throw new Exception("A close/deactivation request reopened the tucked widget.");
   HoverEntered();await Task.Delay(70);Native.GetWindowRect(new WindowInteropHelper(this).Handle,out mid);if(!edgeMotionRunning||mid.Top>=selected.Bounds.Top)throw new Exception("Reveal had no intermediate native position.");await WaitForReveal();int showFrames=edgeMotionFrames;
   for(int i=0;i<3;i++){SetHeaderHovered(false);SetEdgeHidden(true);await Task.Delay(65);Native.Rect before,after;Native.GetWindowRect(new WindowInteropHelper(this).Handle,out before);HoverEntered();Native.GetWindowRect(new WindowInteropHelper(this).Handle,out after);if(Math.Abs(before.Top-after.Top)>2)throw new Exception("Rapid edge reversal jumped its native position.");await WaitForReveal();}
   SetHeaderHovered(false);SetEdgeHidden(true);await WaitForReveal();HoverEntered();await Task.Delay(60);Navigate("settings");await WaitForReveal();if(route!="settings"||scroll==null||edgeNavigationPending)throw new Exception("A menu click during edge reveal was lost.");
   await Task.Delay(30);Native.Rect final;Native.GetWindowRect(new WindowInteropHelper(this).Handle,out final);if(final.Top!=selected.Bounds.Top||edgeHidden||edgeFrame!=null||edgeWatchdog.IsEnabled||edgePump!=null||FramePump.ActiveCount!=0)throw new Exception("Edge motion left stale geometry or callbacks.");
   foreach(string destination in new[]{"media","settings","controls"}){Navigate(destination);await Task.Delay(65);}await WaitForReveal();if(miniTitle.Visibility!=Visibility.Collapsed||miniTitle.Text.Length!=0)throw new Exception("Route title remained in compact header.");
   report.Add((AllowsTransparency?"Layered":"Native")+" edge motion: hide steps="+hideFrames+"; show steps="+showFrames+"; high resolution="+edgePreciseTiming+"; 3 interrupted reversals stayed within 2px; settled at monitor top; no frame/timer leak; route header blank");
   Preferences.ReducedMotion=true;SetHeaderHovered(false);Navigate("");
  }
  public async Task MotionTest(){
   string output=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"qa");Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"FAILED.txt"),"");var report=new List<string>();Media.Title="Motion preview";Media.Artist="";Media.Playing=true;
   await CheckEdgeSymmetry(output,report);
   var probe=new AdaWindow(true,false,new Settings{Material="Şeffaf",DisplayId=selected.Id});try{probe.Show();probe.Media.Title="Motion preview";probe.Media.Playing=true;await probe.CheckEdgeSymmetry(output,report);}finally{probe.Close();}
   File.WriteAllLines(Path.Combine(output,"motion-report.txt"),report);Close();
  }
 }
}
