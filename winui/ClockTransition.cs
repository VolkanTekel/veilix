using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
namespace Veilix;
public sealed partial class MainWindow {
 CompositionPropertySet clockMix;
 readonly List<(UIElement Element,ExpressionAnimation Animation)> clockBindings=new();
 bool clockBindingsActive,clockSwitching;
 int clockVersion;double clockFrom,clockTo,clockStart,clockDuration;
 double ClockProgress(){if(!clockSwitching)return clockTo;double t=Math.Clamp((frameWatch.Elapsed.TotalMilliseconds-clockStart)/clockDuration,0,1);return clockFrom+(clockTo-clockFrom)*(1-Math.Pow(1-t,3));}
 void CreateClockBindings(){if(clockMix!=null)return;clockMix=compositor.CreatePropertySet();clockMix.InsertScalar("Progress",(float)clockTo);
  // Use XAML facade animations consistently. Do not mix a handout Visual's
  // Translation with UIElement.Translation on these two elements.
  void Bind(UIElement element,string target,string expression){var animation=compositor.CreateExpressionAnimation(expression);animation.Target=target;animation.SetReferenceParameter("mix",clockMix);clockBindings.Add((element,animation));}
  Bind(clock,"Opacity","mix.Progress");Bind(waves,"Opacity","1 - mix.Progress");
  Bind(clock,"Translation","Vector3(0, -5 * (1 - mix.Progress), 0)");Bind(waves,"Translation","Vector3(0, 5 * mix.Progress, 0)");
 }
 void SetClockBase(double progress){foreach(var binding in clockBindings)if(clockBindingsActive)binding.Element.StopAnimation(binding.Animation);clockBindingsActive=false;clock.Opacity=progress;waves.Opacity=1-progress;clock.Translation=new Vector3(0,(float)(-5*(1-progress)),0);waves.Translation=new Vector3(0,(float)(5*progress),0);}
 async void TransitionClock(bool show){
  double from=ClockProgress(),to=show?1:0;int version=++clockVersion;CreateClockBindings();clockMix.StopAnimation("Progress");clockMix.InsertScalar("Progress",(float)from);
  clockFrom=from;clockTo=to;clockStart=frameWatch.Elapsed.TotalMilliseconds;clockDuration=Math.Max(60,180*Math.Abs(to-from));clockSwitching=Motion&&ready&&Math.Abs(to-from)>.001;
  if(!clockSwitching){clockMix.InsertScalar("Progress",(float)to);SetClockBase(to);return;}
  if(!clockBindingsActive){clockBindingsActive=true;foreach(var binding in clockBindings)binding.Element.StartAnimation(binding.Animation);}
  using(var easing=compositor.CreateCubicBezierEasingFunction(new Vector2(1f/3,1),new Vector2(2f/3,1)))using(var animation=compositor.CreateScalarKeyFrameAnimation()){animation.InsertKeyFrame(0,(float)from);animation.InsertKeyFrame(1,(float)to,easing);animation.Duration=TimeSpan.FromMilliseconds(clockDuration);clockMix.StartAnimation("Progress",animation);}
  await Task.Delay(TimeSpan.FromMilliseconds(clockDuration));if(disposed||version!=clockVersion)return;
  clockMix.StopAnimation("Progress");clockMix.InsertScalar("Progress",(float)to);clockSwitching=false;SetClockBase(to);SyncPeakSampling();
 }
 void DisposeClockTransition(){clockVersion++;foreach(var binding in clockBindings){if(clockBindingsActive)binding.Element.StopAnimation(binding.Animation);binding.Animation.Dispose();}clockBindings.Clear();clockMix?.Dispose();}
 void SyncPeakSampling(){
  bool allowed=!hidden&&!perfQuiet&&(media.Playing||perfForceWave)&&(Motion||perfForceWave)&&((route==""&&clockShown!=true)||clockSwitching);
  if(!allowed){peakTimer.Stop();DrawLevels(frameWatch.Elapsed.TotalSeconds,0,0);return;}
  if(!peakTimer.IsRunning){lastPeakTime=frameWatch.Elapsed.TotalSeconds;DrawPeak();peakTimer.Start();}
 }
 static double NormalizePeak(double raw)=>!double.IsFinite(raw)||raw<=.001?0:Math.Pow(Math.Clamp(raw*1.7,0,1),.55);
}
