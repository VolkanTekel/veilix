// GPL-3.0-or-later. Native vector icons and retained-mode motion for Ada.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Ada {
 public sealed class MarqueeLabel : Grid {
  readonly TextBlock label;readonly TranslateTransform motion=new TranslateTransform();string value="";bool enabled;
  public MarqueeLabel(double size,bool bold){ClipToBounds=true;Height=Math.Ceiling(size*1.65);label=new TextBlock{FontSize=size,FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Left,RenderTransform=motion};label.SetResourceReference(TextBlock.ForegroundProperty,"Text");Children.Add(label);Loaded+=delegate{Refresh();};Unloaded+=delegate{Stop();};SizeChanged+=delegate{Refresh();};IsVisibleChanged+=delegate{Refresh();};}
  public string Text {get{return value;}set{if(this.value==value)return;this.value=value??"";label.Text=this.value;ToolTip=this.value;AutomationProperties.SetName(this,this.value);Refresh();}}
  public bool MotionEnabled {get{return enabled;}set{if(enabled==value)return;enabled=value;Refresh();}}
  int frameRate=60;public int FrameRate{get{return frameRate;}set{if(frameRate==value)return;frameRate=value;Refresh();}}
  public bool IsScrolling {get{return motion.HasAnimatedProperties;}}
  public double Offset {get{return motion.X;}}
  protected override Size MeasureOverride(Size constraint){label.Measure(new Size(Double.PositiveInfinity,Double.PositiveInfinity));return new Size(Double.IsInfinity(constraint.Width)?Math.Min(180,label.DesiredSize.Width):Math.Max(0,constraint.Width),Height);}
  protected override Size ArrangeOverride(Size final){label.Arrange(new Rect(0,0,Math.Max(final.Width,label.DesiredSize.Width),final.Height));return final;}
  void Stop(){motion.BeginAnimation(TranslateTransform.XProperty,null);motion.X=0;}
  void Refresh(){Stop();if(!IsLoaded||!IsVisible||ActualWidth<=0)return;label.MaxWidth=Double.PositiveInfinity;label.TextTrimming=TextTrimming.None;label.Measure(new Size(Double.PositiveInfinity,Double.PositiveInfinity));double overflow=label.DesiredSize.Width-ActualWidth;if(overflow<8||!enabled){label.TextTrimming=TextTrimming.CharacterEllipsis;label.MaxWidth=ActualWidth;return;}label.MaxWidth=Double.PositiveInfinity;label.TextTrimming=TextTrimming.None;double seconds=Math.Max(2,overflow/24);var a=new DoubleAnimationUsingKeyFrames{RepeatBehavior=RepeatBehavior.Forever};a.KeyFrames.Add(new DiscreteDoubleKeyFrame(0,KeyTime.FromTimeSpan(TimeSpan.Zero)));a.KeyFrames.Add(new DiscreteDoubleKeyFrame(0,KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.3))));a.KeyFrames.Add(new LinearDoubleKeyFrame(-overflow,KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.3+seconds))));a.KeyFrames.Add(new DiscreteDoubleKeyFrame(-overflow,KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.5+seconds))));a.KeyFrames.Add(new LinearDoubleKeyFrame(0,KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.5+seconds*2))));a.KeyFrames.Add(new DiscreteDoubleKeyFrame(0,KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3.8+seconds*2))));Timeline.SetDesiredFrameRate(a,FrameRate);motion.BeginAnimation(TranslateTransform.XProperty,a);}
 }
 public static class LayoutMetrics {
  public static double AutoScale(Display display){double dpi=display.Dpi/96.0;double relative=Math.Sqrt((display.Bounds.Width/dpi/1920.0)*(display.Bounds.Height/dpi/1080.0));return Math.Max(.85,Math.Min(1.6,relative));}
  public static double Scale(Display display,double userScale){double logical=display.Bounds.Width/(display.Dpi/96.0);return Math.Min(AutoScale(display)*userScale,(logical-24)/528);}
 }
 public sealed partial class AdaWindow {
  const double NotchShoulder=14;
  Grid host;RectangleGeometry reveal=new RectangleGeometry(),bodyReveal=new RectangleGeometry(),topReveal=new RectangleGeometry();GeometryGroup revealMask=new GeometryGroup{FillRule=FillRule.Nonzero};MarqueeLabel mediaTitle;Border connector;FrameworkElement weatherCluster;string settingsPage="Ekran";
  FramePump revealPump;EventHandler revealNativeFrame;
  double shadowProgress;double revealDurationMultiplier=1;
  bool revealRunning;int revealGeneration;double visualWidth=520,visualHeight=HeaderHeight;
  double WindowDpi {get{var source=PresentationSource.FromVisual(this);return source==null?selected.Dpi/96.0:source.CompositionTarget.TransformToDevice.M11;}}
  double EffectiveScale{get{return LayoutMetrics.Scale(selected,Preferences.Scale)*(selected.Dpi/96.0)/WindowDpi;}}
  int TargetFps{get{return Math.Max(30,Math.Min(Preferences.FrameLimit,selected.Hz));}}
  bool MotionAllowed{get{return !Preferences.ReducedMotion&&SystemParameters.ClientAreaAnimation;}}
  // Anchor to the outer right edge, independent of clock visibility/ink width.
  double HeaderGapCenter(){return header.ActualWidth-12-weatherCluster.ActualWidth/2;}
  FrameworkElement Glyph(string name,double size=20,string color="Text"){
   var canvas=new Canvas{Width=24,Height=24,IsHitTestVisible=false};foreach(var geometry in FluentIcons.Get(name)){var path=new System.Windows.Shapes.Path{Data=geometry,IsHitTestVisible=false};path.SetResourceReference(System.Windows.Shapes.Shape.FillProperty,color);canvas.Children.Add(path);}return new Viewbox{Width=size,Height=size,Child=canvas,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,IsHitTestVisible=false};
  }
  object ButtonContent(string icon,string label,int iconSize=17){var row=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center};row.Children.Add(Glyph(icon,iconSize));var t=Label(label,12);t.Margin=new Thickness(8,0,0,0);t.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(t);return row;}
  MarqueeLabel Marquee(string text,double size,bool bold=true){var m=new MarqueeLabel(size,bold){Text=text,MotionEnabled=MotionAllowed&&Media.Playing,FrameRate=TargetFps};return m;}
  void SetSettledMask(double width,double height,double radius){
   if(!AllowsTransparency){main.OpacityMask=null;host.Clip=revealMask;return;}
   // The border paints its own antialiased edge. Mask only the children so a
   // scrollbar cannot bleed past it; avoid hard-clipping the painted curve.
   var geometry=new GeometryGroup{FillRule=FillRule.Nonzero};geometry.Children.Add(new RectangleGeometry(new Rect(0,0,width,height),radius,radius));geometry.Children.Add(new RectangleGeometry(new Rect(0,0,width,Math.Min(radius,height))));
   var mask=new DrawingBrush(new GeometryDrawing(Brushes.White,null,geometry)){Stretch=Stretch.Fill};mask.Freeze();main.OpacityMask=mask;host.Clip=null;
  }
  void AnimateRegion(double width,double height,bool animate){
   double scale=EffectiveScale;double shoulder=NotchShoulder*scale;double targetW=Math.Round((width+NotchShoulder*2)*scale),targetH=Math.Round(Math.Min(height*scale,selected.Bounds.Height/WindowDpi-12));
   double currentW=revealRunning?reveal.Rect.Width:visualWidth,currentH=revealRunning?reveal.Rect.Height:visualHeight;
   double shadowFrom=shadowProgress,shadowTo=height>HeaderHeight+.5?1:0;int generation=++revealGeneration;StopReveal();
   // Final logical layout is committed once. Only clipping/opacity moves; text,
   // controls and DPI never scale or reflow during the transition.
   bool motion=animate&&MotionAllowed&&IsLoaded;
   double hostW=motion?Math.Max(currentW,targetW):targetW,hostH=motion?Math.Max(currentH,targetH):targetH;
   main.Width=width;main.Height=(motion?Math.Max(currentH,targetH):targetH)/scale;main.HorizontalAlignment=HorizontalAlignment.Center;main.VerticalAlignment=VerticalAlignment.Top;main.LayoutTransform=new ScaleTransform(scale,scale);
   Width=hostW;Height=hostH;surface.Width=hostW;surface.Height=hostH;
   var from=new Rect((hostW-currentW)/2,0,currentW,currentH);var to=new Rect((hostW-targetW)/2,0,targetW,targetH);double radius=Math.Min(26*scale,targetH/2);
   reveal.RadiusX=reveal.RadiusY=bodyReveal.RadiusX=bodyReveal.RadiusY=radius;surface.CornerRadius=new CornerRadius(0,0,radius,radius);((NotchSurface)surface).Shoulder=shoulder;
   Action finish=delegate{if(generation!=revealGeneration)return;StopReveal();shadowProgress=shadowTo;main.Height=targetH/scale;Width=targetW;Height=targetH;surface.Width=targetW;surface.Height=targetH;reveal.Rect=new Rect(0,0,targetW,targetH);bodyReveal.Rect=new Rect(shoulder,0,Math.Max(1,targetW-shoulder*2),targetH);topReveal.Rect=new Rect(0,0,targetW,Math.Min(radius,targetH));visualWidth=targetW;visualHeight=targetH;((NotchSurface)surface).VisibleBounds=reveal.Rect;SetSettledMask(width,targetH/scale,radius/scale);Place();Native.CropNotch(this,reveal.Rect,radius,shoulder);UpdateClippedBackdrop(reveal.Rect,radius,shoulder);};
   if(!motion){finish();return;}
   revealRunning=true;main.OpacityMask=null;host.Clip=revealMask;
    reveal.Rect=from;((NotchSurface)surface).VisibleBounds=from;Native.CropNotch(this,from,radius,shoulder);Place();bodyReveal.Rect=new Rect(from.X+shoulder,0,Math.Max(1,from.Width-shoulder*2),from.Height);topReveal.Rect=new Rect(from.X,0,from.Width,Math.Min(radius,from.Height));
    var duration=TimeSpan.FromMilliseconds((targetH>currentH?360:300)*revealDurationMultiplier);
    if(!AllowsTransparency||clippedBackdrop!=null){var watch=Stopwatch.StartNew();double lastFrame=-1000;Action frame=delegate{if(!revealRunning||generation!=revealGeneration)return;double ms=watch.Elapsed.TotalMilliseconds;if(ms-lastFrame<1000.0/TargetFps&&ms<duration.TotalMilliseconds)return;lastFrame=ms;double t=Math.Min(1,ms/duration.TotalMilliseconds),ease=1-Math.Pow(1-t,3);var rect=new Rect(from.X+(to.X-from.X)*ease,0,from.Width+(to.Width-from.Width)*ease,from.Height+(to.Height-from.Height)*ease);shadowProgress=shadowFrom+(shadowTo-shadowFrom)*ease;reveal.Rect=rect;bodyReveal.Rect=new Rect(rect.X+shoulder,0,Math.Max(1,rect.Width-2*shoulder),rect.Height);topReveal.Rect=new Rect(rect.X,0,rect.Width,Math.Min(radius,rect.Height));((NotchSurface)surface).VisibleBounds=rect;UpdateClippedBackdrop(rect,radius,shoulder);Native.CropNotch(this,rect,radius,shoulder);if(t>=1)finish();};UpdateClippedBackdrop(from,radius,shoulder);revealPump=FramePump.TryStart(Dispatcher,TargetFps,frame);if(revealPump==null){revealNativeFrame=delegate{frame();};CompositionTarget.Rendering+=revealNativeFrame;}return;}
    var revealMotion=new RectAnimation(from,to,duration){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.HoldEnd};Timeline.SetDesiredFrameRate(revealMotion,TargetFps);
    var bodyMotion=new RectAnimation(bodyReveal.Rect,new Rect(to.X+shoulder,0,Math.Max(1,to.Width-shoulder*2),to.Height),duration){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.HoldEnd};Timeline.SetDesiredFrameRate(bodyMotion,TargetFps);bodyReveal.BeginAnimation(RectangleGeometry.RectProperty,bodyMotion,HandoffBehavior.SnapshotAndReplace);
    // Let WPF animate both parts of the mask. No per-frame dispatcher callback
    // is needed to copy rectangle coordinates into the top edge.
    var topMotion=new RectAnimation(new Rect(from.X,0,from.Width,Math.Min(radius,from.Height)),new Rect(to.X,0,to.Width,Math.Min(radius,to.Height)),duration){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.HoldEnd};Timeline.SetDesiredFrameRate(topMotion,TargetFps);topReveal.BeginAnimation(RectangleGeometry.RectProperty,topMotion,HandoffBehavior.SnapshotAndReplace);
    var shellMotion=new RectAnimation(from,to,duration){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.HoldEnd};Timeline.SetDesiredFrameRate(shellMotion,TargetFps);((NotchSurface)surface).BeginAnimation(NotchSurface.VisibleBoundsProperty,shellMotion);
    revealMotion.Completed+=delegate{finish();};reveal.BeginAnimation(RectangleGeometry.RectProperty,revealMotion,HandoffBehavior.SnapshotAndReplace);
   if(connector!=null){var pulse=new DoubleAnimation(44,72,TimeSpan.FromMilliseconds(260)){AutoReverse=true,FillBehavior=FillBehavior.Stop};Timeline.SetDesiredFrameRate(pulse,TargetFps);connector.BeginAnimation(FrameworkElement.WidthProperty,pulse);}
  }
  void StopReveal(){if(revealNativeFrame!=null){CompositionTarget.Rendering-=revealNativeFrame;revealNativeFrame=null;}if(revealPump!=null){revealPump.Dispose();revealPump=null;}if(surface is NotchSurface){var shell=(NotchSurface)surface;var bounds=shell.VisibleBounds;shell.BeginAnimation(NotchSurface.VisibleBoundsProperty,null);shell.VisibleBounds=bounds;}var current=reveal.Rect;var top=topReveal.Rect;var body=bodyReveal.Rect;reveal.BeginAnimation(RectangleGeometry.RectProperty,null);topReveal.BeginAnimation(RectangleGeometry.RectProperty,null);bodyReveal.BeginAnimation(RectangleGeometry.RectProperty,null);reveal.Rect=current;topReveal.Rect=top;bodyReveal.Rect=body;revealRunning=false;}
  void GoSettings(string page){settingsPage=page;Navigate("settings");}
  void SettingsChoices(string title,string[] values,string selectedValue,Action<string> changed){Section(title);var row=new UniformGrid{Columns=values.Length,Margin=new Thickness(0,0,0,16)};foreach(string s in values){string value=s;var b=MakeButton(value,value,delegate{changed(value);RenderRoute(true);});b.Margin=new Thickness(3,0,3,0);b.Padding=new Thickness(8,8,8,8);b.Background=(Brush)Resources[value==selectedValue?"Selected":"Tile"];b.FontWeight=value==selectedValue?FontWeights.SemiBold:FontWeights.Normal;row.Children.Add(b);}content.Children.Add(row);}
 }
}
