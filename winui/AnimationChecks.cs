using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

namespace Veilix;

public sealed partial class MainWindow {
 async Task CheckAnimationInterruptions(List<string> report){
  bool reduced=prefs.ReducedMotion;
  bool wasHovered=hovered;
  string originalTab=timeTab;
  try{
   SetReducedMotion(true);
   Navigate("");
   SetHidden(false);
   SetReducedMotion(false);
   Navigate("controls");
   Check(resizing,"Opening did not start a height transition");
   int version=resizeVersion;
   double openingHeight=targetHeight;
   Render();
   Check(resizeVersion==version&&targetHeight==openingHeight,"Same-section refresh restarted the opening animation");

   SetReducedMotion(true);
   CheckOpen();
   await Task.Delay(380);
   CheckOpen();
   SetReducedMotion(false);
   Navigate("");
   Check(resizing,"Closing did not start a height transition");
   version=resizeVersion;
   Navigate("");
   Check(resizeVersion==version,"Repeated collapse restarted the height transition");
   SetReducedMotion(true);
   CheckClosed();
   await Task.Delay(320);
   CheckClosed();
   SetReducedMotion(false);
   Navigate("time");
   SelectTimeTab("Sayaç");
   await Task.Delay(35);
   SelectTimeTab("Kronometre");
   await Task.Delay(35);
   SelectTimeTab("Pomodoro");
   SetReducedMotion(true);
   Check(route=="time"&&timeTab=="Pomodoro"&&timeValue!=null&&ElementCompositionPreview.GetElementVisual(page).Opacity==1,
    "Reversing timer tabs left stale or faded content");
   report.Add("Animation interruptions: same-section refresh and repeated collapse do not restart height motion. Reduced Motion immediately snaps an active open/close and clears page motion; expired callbacks do not change the settled state.");
   report.Add("Timer tabs: fast Countdown / Stopwatch / Pomodoro switching leaves the selected content visible after Reduced Motion.");

   void CheckOpen(){
    Check(!resizing&&body.Visibility==Visibility.Visible&&ElementCompositionPreview.GetElementVisual(body).Opacity==1
      &&ElementCompositionPreview.GetElementVisual(page).Opacity==1&&Math.Abs(surface.Height-openingHeight)<.01
      &&Math.Abs(clipGeometry.Size.Y-openingHeight-24)<.01,"Reduced Motion left an incomplete open or fade");
   }
   void CheckClosed(){
    Check(!resizing&&route==""&&body.Visibility==Visibility.Collapsed&&page.Children.Count==0
      &&ElementCompositionPreview.GetElementVisual(body).Opacity==0&&Math.Abs(surface.Height-CompactHeight)<.01
      &&shadow.Opacity==0,"Reduced Motion left an incomplete close, fade, or shadow");
   }
  }finally{
   timeTab=originalTab;
   SetHovered(wasHovered);
   SetReducedMotion(reduced);
   Navigate("");
  }
 }
}
