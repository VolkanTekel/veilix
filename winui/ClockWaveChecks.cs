using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Veilix;
public sealed partial class MainWindow {
 async Task CheckClockWave(List<string> report){
  bool reduced=prefs.ReducedMotion,show=prefs.ShowClock,playing=media.Playing,force=perfForceWave,quiet=perfQuiet,wasHovered=hovered;double volumeBefore=audio.Volume;
  try{
   Navigate("");SetHidden(false);prefs.ReducedMotion=false;prefs.ShowClock=true;perfForceWave=true;perfQuiet=false;SetHovered(false);clockShown=null;UpdateClock();await Task.Delay(240);
   Check(!clockSwitching&&clock.Opacity==0&&waves.Opacity==1&&Math.Abs(clock.Translation.Y+5)<.01&&waves.Translation.Y==0,"Wave state did not settle");
   SetHovered(true);Check(clockSwitching&&peakTimer.IsRunning,"Outgoing wave stopped before its fade completed");await Task.Delay(65);double progress=ClockProgress();Check(progress>0&&progress<1,"Clock transition jumped to an endpoint");SetHovered(false);Check(Math.Abs(ClockProgress()-progress)<.1,"Interrupted clock transition restarted from an endpoint");await Task.Delay(240);Check(!clockSwitching&&clock.Opacity==0&&waves.Opacity==1,"Old clock completion overwrote reverse transition");
   for(int i=0;i<6;i++){SetHovered(i%2==0);await Task.Delay(20);}SetHovered(true);await Task.Delay(240);
   Check(!clockSwitching&&!clockBindingsActive&&clock.Opacity==1&&waves.Opacity==0&&clock.Translation.Y==0&&Math.Abs(waves.Translation.Y-5)<.01&&!peakTimer.IsRunning,"Rapid hover changes left an animated/stale clock or wave");
   Check(clockBindings.Count==4&&clockMix!=null,"Clock/wave do not share a transition source");
   prefs.ReducedMotion=true;SetHovered(false);Check(!clockSwitching&&clock.Opacity==0&&waves.Opacity==1,"Reduced motion did not apply immediately");
   perfForceWave=false;peakTimer.Stop();double now=frameWatch.Elapsed.TotalSeconds;DrawLevels(now,.016,.8);int baseline=(int)Math.Round(3*host.Dpi);Check(barPixels.Any(x=>x>baseline),"Wave fixture did not produce active bars");
   DrawLevels(now+.016,.016,NormalizePeak(0));for(int i=0;i<30;i++)DrawLevels(now+.032+i*.016,.016,NormalizePeak(.0004));Check(barPixels.All(x=>x==baseline)&&peakLevels.All(x=>x==0),"Silence/noise still moves or retains wave bars");Check(NormalizePeak(double.NaN)==0&&NormalizePeak(double.PositiveInfinity)==0&&NormalizePeak(.0009)==0&&NormalizePeak(.1)>0,"Audio noise gate");
   prefs.ReducedMotion=false;prefs.ShowClock=false;media.Playing=false;clockShown=null;UpdateClock();Check(!peakTimer.IsRunning&&barPixels.All(x=>x==baseline),"Paused playback retained sampling/motion");
   media.Playing=true;audio.Volume=0;UpdateClock();await Task.Delay(120);Check(barPixels.All(x=>x==baseline)&&peakTimer.Interval==TimeSpan.FromMilliseconds(50),"Silent playing session animated bars or kept full-rate sampling");
   report.Add("Clock/wave: shared GPU progress, reversing mid-fade and 6 rapid hover changes settle exact facade opacity/translation; outgoing wave samples until hidden. Reduced motion applies immediately.");
   report.Add("Wave silence: real UI bars flatten exactly, remain unchanged for 30 silent/noise samples; paused playback stops timer, playing-with-zero-volume stays static at low-rate detection. Test uses synthetic levels; no real sound/volume changes.");
  }finally{perfForceWave=force;perfQuiet=quiet;media.Playing=playing;audio.Volume=volumeBefore;prefs.ReducedMotion=reduced;prefs.ShowClock=show;SetHovered(wasHovered);clockShown=null;UpdateClock();}
 }
}
