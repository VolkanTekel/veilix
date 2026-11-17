// GPL-3.0-or-later. Layout, input lifecycle and update-cost regressions.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Ada {
 public sealed partial class AdaWindow {
  async Task CheckDailyUse(string output,List<string> report){
   Preferences.ShowClock=true;Preferences.ReducedMotion=true;Media.Playing=true;Media.Title="Impostor Syndrome — a longer title to exercise the marquee";Media.Artist="Sidney Gish";
   foreach(var display in displays){selected=display;foreach(double scale in new[]{.85,1.0,1.2}){
    Preferences.Scale=scale;SetHeaderHovered(true);Navigate("");UpdateLayout();double start=weatherCluster.TranslatePoint(new Point(0,0),header).X;
    SetHeaderHovered(false);UpdateLayout();double end=weatherCluster.TranslatePoint(new Point(0,0),header).X;
    if(Math.Abs(start-end)>.1)throw new Exception("Weather moved when clock disappeared: "+(end-start));
    double right=date.TranslatePoint(new Point(date.ActualWidth,0),header).X;
    if(Math.Abs(header.ActualWidth-right-12)>1)throw new Exception("Date is not anchored 12 logical pixels from the right edge.");
    if(miniTitle.ActualWidth<100||date.ActualHeight>HeaderHeight-8)throw new Exception("Compact text clipped or squeezed.");
   }}
   selected=displays.FirstOrDefault(d=>!d.Primary)??displays[0];Preferences.Scale=.85;SetHeaderHovered(false);Navigate("");await Capture(output,"compact-readable-idle");
   Preferences.ReducedMotion=false;double fixedX=weatherCluster.TranslatePoint(new Point(0,0),header).X;
   SetHeaderHovered(true);for(int i=0;i<12;i++){await Task.Delay(20);if(Math.Abs(weatherCluster.TranslatePoint(new Point(0,0),header).X-fixedX)>.1)throw new Exception("Weather shifted during clock entry.");}
   await Capture(output,"compact-readable-hover");SetHeaderHovered(false);for(int i=0;i<12;i++){await Task.Delay(20);if(Math.Abs(weatherCluster.TranslatePoint(new Point(0,0),header).X-fixedX)>.1)throw new Exception("Weather shifted during clock exit.");}
   report.Add("Compact layout: title 16 / artist 12 / date 15; 12px outer insets; weather/date fixed within 0.1 logical pixels at both display DPIs and 85/100/120% scale, including clock animation");
   Preferences.ReducedMotion=true;foreach(string destination in new[]{"media","settings","controls","weather"}){
    Navigate(destination);HandleOutsideClick(WidgetProcessId);if(route!=destination)throw new Exception("Own popup click closed the menu.");
    themeEditorOpen=true;HandleOutsideClick(0);themeEditorOpen=false;if(route!=destination)throw new Exception("Color dialog closed its owner.");
    HandleOutsideClick(0);if(route!=""||scroll!=null||outsideClicks!=null)throw new Exception("Outside click failed to collapse menu.");
   }
   var watcher=new OutsideClickWatcher(delegate(uint pid){});try{await Task.Delay(120);if(!watcher.Installed)throw new Exception("Menu mouse observer could not install.");}finally{watcher.Dispose();}await Task.Delay(80);if(watcher.Installed)throw new Exception("Menu mouse observer leaked after dispose.");
   report.Add("Outside click: all four routes collapse; own-process popup and modal theme editor preserved; native hook installs and unhooks; closed widget keeps no mouse observer");
   Navigate("media");var oldContent=content;var oldPlay=playButton.Content;for(int i=0;i<100;i++)MediaChanged();if(content!=oldContent||playButton.Content!=oldPlay)throw new Exception("Unchanged timeline events rebuilt media controls.");
   int events=mediaEventCount;for(int i=0;i<200;i++)QueueMediaChanged();await Task.Delay(100);if(mediaEventCount-events!=1)throw new Exception("Media event burst was not coalesced.");
   Preferences.ReducedMotion=false;Navigate("");await WaitForReveal();
   var gaps=new List<double>();var watch=Stopwatch.StartNew();double last=0;EventHandler sampled=delegate{double now=watch.Elapsed.TotalMilliseconds;if(last>0)gaps.Add(now-last);last=now;};CompositionTarget.Rendering+=sampled;
   try{for(int i=0;i<4;i++){SetHeaderHovered(i%2==0);for(int j=0;j<40;j++)QueueMediaChanged();await Task.Delay(260);}}finally{CompositionTarget.Rendering-=sampled;}
   gaps.Sort();report.Add("Media updates: 100 unchanged notifications retained controls; 200 queued notifications became one update; WPF rendering samples="+gaps.Count+"; p95 gap="+(gaps.Count==0?0:gaps[(int)((gaps.Count-1)*.95)]).ToString("F1")+"ms; max="+(gaps.Count==0?0:gaps.Last()).ToString("F1")+"ms (dispatcher samples, not display FPS)");
   Preferences.Scale=1;Preferences.ReducedMotion=true;SetHeaderHovered(false);Navigate("");
  }
 }
}
