// Reverse curves meet the screen edge and share the body's material and tint.
using System;
using System.Collections.Generic;
using System.Numerics;
using Ada;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
namespace Veilix;
public sealed partial class MainWindow {
 readonly List<(Grid Grid,Microsoft.UI.Xaml.Shapes.Path Tint)> shoulders=new();readonly List<IDisposable> shoulderResources=new();

 Canvas shoulderLayer;CanvasGeometry leftShoulderMask,rightShoulderMask;
 readonly Microsoft.UI.Xaml.Shapes.Path transparentTop=new(){IsHitTestVisible=false};
 void BuildShoulders(){
  const float s=14;shoulderLayer=new Canvas{Width=width,Height=s,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,IsHitTestVisible=false};scene.Children.Insert(0,shoulderLayer);
  using var builder=new CanvasPathBuilder(null);builder.BeginFigure(0,0);builder.AddCubicBezier(new Vector2(s*.5522848f,0),new Vector2(s,s*.4477152f),new Vector2(s,s));builder.AddLine(s,0);builder.EndFigure(CanvasFigureLoop.Closed);
  leftShoulderMask=CanvasGeometry.CreatePath(builder);rightShoulderMask=leftShoulderMask.Transform(new Matrix3x2(-1,0,0,1,s,0));
  foreach(bool left in new[]{true,false}){
   var canvas=left?leftShoulderMask:rightShoulderMask;shoulderResources.Add(canvas);
   var grid=new Grid{Width=s,Height=s,IsHitTestVisible=false};Canvas.SetLeft(grid,left?-s:width);Canvas.SetTop(grid,0);
   // A real Path owns the fill. No rectangular external backdrop is created here.
   var color=(Microsoft.UI.Xaml.Shapes.Path)Microsoft.UI.Xaml.Markup.XamlReader.Load("<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Data='M0,0 C7.7319872,0 14,6.2680128 14,14 L14,0 Z'/>");
   if(!left)color.Data.Transform=new MatrixTransform{Matrix=new Matrix(-1,0,0,1,14,0)};
   grid.Children.Add(color);shoulderLayer.Children.Add(grid);shoulders.Add((grid,color));
  }
  Canvas.SetLeft(transparentTop,-s);
  shoulderLayer.Children.Add(transparentTop);
  UpdateShoulders();
 }
 RectangleClip bodyBackdropClip;
 void ClipBodyBackdrop(){
  if(disposed||!glass.IsLoaded||glass.SystemBackdrop==null)return;var placement=ElementCompositionPreview.GetElementChildVisual(glass);if(placement==null)return;
  if(bodyBackdropClip==null){
   // External backdrop links support rectangle/corner clips. The previous
   // geometric clip was accepted by the API but did not clip the system surface.
   bodyBackdropClip=compositor.CreateRectangleClip();bodyBackdropClip.Top=0;bodyBackdropClip.Left=clipGeometry.Offset.X;bodyBackdropClip.Right=clipGeometry.Offset.X+clipGeometry.Size.X;bodyBackdropClip.Bottom=clipGeometry.Offset.Y+clipGeometry.Size.Y;bodyBackdropClip.BottomLeftRadius=bodyBackdropClip.BottomRightRadius=new Vector2(24);
   foreach(var entry in new[]{new[]{"Left","shape.Offset.X"},new[]{"Right","shape.Offset.X + shape.Size.X"},new[]{"Bottom","shape.Offset.Y + shape.Size.Y"}}){var expression=compositor.CreateExpressionAnimation(entry[1]);expression.SetReferenceParameter("shape",clipGeometry);bodyBackdropClip.StartAnimation(entry[0],expression);expression.Dispose();}
  }
  if(placement.Clip!=bodyBackdropClip)placement.Clip=bodyBackdropClip;placement.BorderMode=CompositionBorderMode.Soft;
 }
 void UpdateShoulders(){
  if(shoulders.Count==0)return;shoulderLayer.Width=width;Canvas.SetLeft(shoulders[0].Grid,-14);Canvas.SetLeft(shoulders[1].Grid,width);
  bool joined=prefs.Material=="Şeffaf";
  transparentTop.Visibility=joined?Visibility.Visible:Visibility.Collapsed;
  foreach(var shoulder in shoulders)shoulder.Grid.Visibility=joined?Visibility.Collapsed:Visibility.Visible;
  if(tint.Background is LinearGradientBrush fill){
   fill.EndPoint=new Windows.Foundation.Point(width,surface.Height);
   if(joined)UpdateTransparentTop(fill);
   else tint.Clip=null;
  }
  if(joined)return;

  for(int i=0;i<shoulders.Count;i++){
   var item=shoulders[i];
   // Absolute coordinates continue the SAME body gradient across each seam.
   // One gradient is shared; material base is folded into the path color.
   if(tint.Background is LinearGradientBrush source){source.EndPoint=new Windows.Foundation.Point(width,surface.Height);double x=i==0?14:-width;var paint=new LinearGradientBrush{MappingMode=BrushMappingMode.Absolute,StartPoint=new Windows.Foundation.Point(x,0),EndPoint=new Windows.Foundation.Point(x+width,surface.Height)};foreach(var stop in source.GradientStops)paint.GradientStops.Add(new GradientStop{Color=ShoulderColor(stop.Color),Offset=stop.Offset});item.Tint.Fill=paint;}
  }
 }
 void UpdateTransparentTop(LinearGradientBrush source){
  // Paint both shoulders and the top of the body in one pass. Two separately
  // antialiased translucent edges produce a seam at fractional-pixel DPI.
  const double curve=14;
  double split=Math.Round(16*host.Dpi)/host.Dpi;
  double right=width+curve*2;
  static Windows.Foundation.Point P(double x,double y)=>new(x,y);
  var outline=new PathFigure{StartPoint=P(0,0),IsClosed=true};
  outline.Segments.Add(new LineSegment{Point=P(right,0)});
  outline.Segments.Add(new BezierSegment{Point1=P(right-7.732,0),Point2=P(right-curve,6.268),Point3=P(right-curve,curve)});
  outline.Segments.Add(new LineSegment{Point=P(right-curve,split)});
  outline.Segments.Add(new LineSegment{Point=P(curve,split)});
  outline.Segments.Add(new LineSegment{Point=P(curve,curve)});
  outline.Segments.Add(new BezierSegment{Point1=P(curve,6.268),Point2=P(7.732,0),Point3=P(0,0)});
  var geometry=new PathGeometry();
  geometry.Figures.Add(outline);
  transparentTop.Data=geometry;
  var paint=new LinearGradientBrush{MappingMode=BrushMappingMode.Absolute,StartPoint=P(curve,0),EndPoint=P(curve+width,surface.Height)};
  foreach(var stop in source.GradientStops)paint.GradientStops.Add(new GradientStop{Color=stop.Color,Offset=stop.Offset});
  transparentTop.Fill=paint;
  tint.Clip=new RectangleGeometry{Rect=new Windows.Foundation.Rect(0,split,width,Math.Max(0,surface.Height-split))};
 }
 Windows.UI.Color ShoulderColor(Windows.UI.Color color){
  if(prefs.Material!="Mika")return color;
  // Fold the neutral material base into the SAME path fill; never add a
  // square surface underneath the inverse curve. Main-window glass stays live.
  bool light=ThemePalette.Get(prefs).Light;var baseColor=C(light?"#F3F3F3":"#202020");double a=color.A/255.0;
  return Windows.UI.Color.FromArgb(255,(byte)Math.Round(color.R*a+baseColor.R*(1-a)),(byte)Math.Round(color.G*a+baseColor.G*(1-a)),(byte)Math.Round(color.B*a+baseColor.B*(1-a)));
 }
 void DisposeShoulders(){if(bodyBackdropClip!=null)bodyBackdropClip.Dispose();foreach(var resource in shoulderResources)resource.Dispose();}
 void AnimateHandleClip(){if(resizing)return;if(SharedGlass)unifiedGlass.SetNarrow(hidden,Motion);var size=new Vector2((float)(hidden?112:width),78);var offset=new Vector2((float)(hidden?(width-112)/2:0),-24);if(!Motion){clipGeometry.StopAnimation("Size");clipGeometry.StopAnimation("Offset");clipGeometry.Size=size;clipGeometry.Offset=offset;return;}var move=compositor.CreateVector2KeyFrameAnimation();move.InsertKeyFrame(1,offset,compositor.CreateCubicBezierEasingFunction(new Vector2(1f/3,1),new Vector2(2f/3,1)));move.Duration=TimeSpan.FromMilliseconds(280);var scale=compositor.CreateVector2KeyFrameAnimation();scale.InsertKeyFrame(1,size,compositor.CreateCubicBezierEasingFunction(new Vector2(1f/3,1),new Vector2(2f/3,1)));scale.Duration=move.Duration;clipGeometry.StartAnimation("Offset",move);clipGeometry.StartAnimation("Size",scale);}
}







