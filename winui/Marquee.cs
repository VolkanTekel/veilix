using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
namespace Veilix;
internal sealed class Marquee {
 readonly TextBlock text;readonly Grid viewport;readonly Func<bool> enabled;string signature="";
 internal Marquee(TextBlock text,Grid viewport,Func<bool> enabled){this.text=text;this.viewport=viewport;this.enabled=enabled;text.TextTrimming=TextTrimming.None;text.TextWrapping=TextWrapping.NoWrap;text.HorizontalAlignment=HorizontalAlignment.Left;ElementCompositionPreview.SetIsTranslationEnabled(text,true);viewport.SizeChanged+=(_,e)=>Sync();viewport.Loaded+=(_,e)=>Sync();viewport.Unloaded+=(_,e)=>{ElementCompositionPreview.GetElementVisual(text).StopAnimation("Translation");signature="";};}
 internal void Invalidate()=>signature="";
 internal void Sync(){
  double width=viewport.ActualWidth;if(!double.IsFinite(width)||width<1)return;string key=text.Text+"|"+Math.Round(width,1)+"|"+enabled();if(signature==key)return;signature="";
  var visual=ElementCompositionPreview.GetElementVisual(text);visual.StopAnimation("Translation");visual.Properties.InsertVector3("Translation",Vector3.Zero);text.Width=double.NaN;text.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));double full=text.DesiredSize.Width;
  if(!double.IsFinite(full)||full<0){text.Width=0;return;}full=Math.Min(full,1000000);text.Width=Math.Ceiling(full);double height=double.IsFinite(viewport.ActualHeight)?Math.Max(22,viewport.ActualHeight):22;viewport.Clip=new RectangleGeometry{Rect=new Rect(0,0,width,height)};double distance=Math.Max(0,full-width);
  if(distance<2||!enabled()){signature=key;return;}using var animation=visual.Compositor.CreateVector3KeyFrameAnimation();animation.InsertKeyFrame(0,Vector3.Zero);animation.InsertKeyFrame(.12f,Vector3.Zero);animation.InsertKeyFrame(.5f,new Vector3(-(float)distance,0,0));animation.InsertKeyFrame(.62f,new Vector3(-(float)distance,0,0));animation.InsertKeyFrame(1,Vector3.Zero);animation.Duration=TimeSpan.FromSeconds(Math.Clamp(distance/28*2+1.4,5,3600));animation.IterationBehavior=AnimationIterationBehavior.Forever;visual.StartAnimation("Translation",animation);signature=key;
 }
}