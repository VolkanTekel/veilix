// GPL-3.0-or-later. One reversible native translation for edge hide and reveal.
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Ada {
 public sealed partial class AdaWindow {
  double edgeOffset,edgeFrom,edgeTo,edgeDuration,edgeLastStep;
  bool edgeMotionRunning,edgePreciseTiming,edgeNavigationPending;int edgeMotionGeneration,edgeMotionFrames;EventHandler edgeFrame;FramePump edgePump;
  readonly Stopwatch edgeWatch=new Stopwatch();
  readonly DispatcherTimer edgeWatchdog=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(32)};
  void InitEdgeMotion(){edgeWatchdog.Tick+=delegate{if(edgeMotionRunning&&edgeWatch.Elapsed.TotalMilliseconds-edgeLastStep>40)StepEdgeMotion();};}
  void ChangeEdgeState(bool hidden){
   if(edgeHidden==hidden)return;
   bool wasTucked=edgeHidden&&!edgeMotionRunning&&surface.Visibility==Visibility.Collapsed;
   StopEdgeMotion();StopReveal();StopContentMotion();FreezeClockMotion();edgeHidden=hidden;
   if(!MotionAllowed||!IsLoaded){FinishEdgeState();return;}
   if(wasTucked&&!hidden){
    // Move the tiny HWND above the monitor before rebuilding the full header.
    // The restored layout is then already final when its first pixel appears.
    edgeOffset=-Math.Max(1,Math.Round(HeaderHeight*EffectiveScale)-1/WindowDpi);Place();
    surface.Visibility=Visibility.Visible;edgeHandle.Visibility=Visibility.Collapsed;
    RenderRoute(false);UpdateHeaderLayout(false);Place();
   }
   if(hidden)headerHovered=false;
   surface.Visibility=Visibility.Visible;edgeHandle.Visibility=Visibility.Collapsed;
   edgeFrom=edgeOffset;
   double visibleBottom=Math.Max(8,Math.Min(Height,reveal.Rect.Bottom));
   edgeTo=hidden?-Math.Max(1,visibleBottom-1/WindowDpi):0;
   double distance=Math.Abs(edgeTo-edgeFrom),extent=Math.Max(1,visibleBottom);
   if(distance<.25){FinishEdgeState();return;}
   edgeDuration=Math.Max(110,(hidden?240:280)*Math.Min(1,distance/extent));edgeLastStep=-1000;edgeMotionFrames=0;edgeMotionRunning=true;
   edgeWatch.Reset();edgePump=FramePump.TryStart(Dispatcher,TargetFps,StepEdgeMotion);edgePreciseTiming=edgePump!=null;
   if(edgePump==null){edgeFrame=delegate{StepEdgeMotion();};CompositionTarget.Rendering+=edgeFrame;StepEdgeMotion();}edgeWatchdog.Start();
  }
  void StepEdgeMotion(){
   if(!edgeMotionRunning)return;if(!edgeWatch.IsRunning)edgeWatch.Start();double now=edgeWatch.Elapsed.TotalMilliseconds;
   if(edgePump==null&&now-edgeLastStep<1000.0/TargetFps)return;edgeLastStep=now;
   double t=Math.Min(1,now/edgeDuration);double ease=(1-Math.Cos(Math.PI*t))/2;edgeOffset=edgeFrom+(edgeTo-edgeFrom)*ease;edgeMotionFrames++;Place();
   if(t>=1)FinishEdgeState();
  }
  void FinishEdgeState(){
   StopEdgeMotion();StopClockMotion();edgeOffset=0;
   if(edgeHidden){
    surface.Visibility=Visibility.Collapsed;edgeHandle.Visibility=Visibility.Visible;
    route="";StopOutsideClicks();if(pageHost!=null){main.Children.Remove(pageHost);pageHost=null;}if(scroll!=null){main.Children.Remove(scroll);scroll=null;}mediaTitle=null;progress=null;playButton=null;progressTimer.Stop();UpdateCompact();UpdateHeaderLayout(false);ResizeEdgeHandle();
    edgeNavigationPending=false;
   }else{
    surface.Visibility=Visibility.Visible;edgeHandle.Visibility=Visibility.Collapsed;
    bool animateMenu=edgeNavigationPending&&route!="";edgeNavigationPending=false;RenderRoute(animateMenu);UpdateHeaderLayout(false);Place();
   }
  }
  void StopEdgeMotion(){edgeMotionGeneration++;edgeMotionRunning=false;edgeWatchdog.Stop();if(edgePump!=null){edgePump.Dispose();edgePump=null;}if(edgeFrame!=null){CompositionTarget.Rendering-=edgeFrame;edgeFrame=null;}edgeWatch.Stop();}
  void FreezeClockMotion(){if(clock==null)return;double opacity=clock.Opacity;var shift=clock.RenderTransform as TranslateTransform;double y=shift==null?0:shift.Y;StopClockMotion();clock.Opacity=opacity;clock.RenderTransform=new TranslateTransform(0,y);}
 }
}
