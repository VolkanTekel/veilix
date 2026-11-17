using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
namespace Veilix;
public sealed partial class MainWindow {
 double shadowFrom,shadowTo; double VisibleShadow(){if(!resizing)return shadow.Opacity;double t=Math.Clamp((frameWatch.Elapsed.TotalMilliseconds-resizeStart)/resizeDuration,0,1);return shadowFrom+(shadowTo-shadowFrom)*(1-Math.Pow(1-t,3));}
 bool resizing;double resizeFrom=54,resizeTo=54,resizeStart,resizeDuration=340,resizeScale;
 double AvailableHeight=>Math.Max(250,(monitor.Work.Bottom-monitor.Bounds.Top)/(monitor.Dpi/96)-24);
 double VisibleHeight(){if(!resizing)return currentHeight;double t=Math.Clamp((frameWatch.Elapsed.TotalMilliseconds-resizeStart)/resizeDuration,0,1);return resizeFrom+(resizeTo-resizeFrom)*(1-Math.Pow(1-t,3));}
 void FadeBodyOut(){
  var visual=ElementCompositionPreview.GetElementVisual(body);
  if(!Motion){visual.Opacity=0;body.Visibility=Visibility.Collapsed;return;}
  using var animation=compositor.CreateScalarKeyFrameAnimation();
  animation.InsertKeyFrame(1,0);
  animation.Duration=TimeSpan.FromMilliseconds(170);
  visual.StartAnimation("Opacity",animation);
 }
 void SetReducedMotion(bool value){
  prefs.ReducedMotion=value;
  if(value){
   Resize(targetHeight);
   SettleEdgePosition();
   ElementCompositionPreview.GetElementVisual(body).Opacity=route==""?0:1;
   ResetContentAnimation(page);
   ResetContentAnimation(mediaArea);
  }
  clockShown=null;
  UpdateHeader();
  Save();
 }
 static void ResetContentAnimation(UIElement element){
  ElementCompositionPreview.SetIsTranslationEnabled(element,true);
  var visual=ElementCompositionPreview.GetElementVisual(element);
  visual.Opacity=1;
  visual.StopAnimation("Translation");
  visual.Properties.InsertVector3("Translation",Vector3.Zero);
 }
 void ReleasePage(){themeCards.Clear();gradientPreview=null;customError=null;applyCustom=null;page.Children.Clear();timeValue=waterValue=waterGoalValue=positionLabel=null;progress=volume=null;playButton=null;tick.Interval=TimeSpan.FromSeconds(1);}
}



