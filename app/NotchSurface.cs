// GPL-3.0-or-later. Soft reverse shoulders where the island meets the screen.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace Ada {
 public sealed class NotchSurface:Border {
  public double Shoulder=14;
  public static readonly DependencyProperty VisibleBoundsProperty=DependencyProperty.Register("VisibleBounds",typeof(Rect),typeof(NotchSurface),new FrameworkPropertyMetadata(Rect.Empty,FrameworkPropertyMetadataOptions.AffectsRender));
  public Rect VisibleBounds{get{return (Rect)GetValue(VisibleBoundsProperty);}set{SetValue(VisibleBoundsProperty,value);}}
  public static Geometry Outline(double width,double height,double shoulder,double radius){
   double s=Math.Max(0,Math.Min(shoulder,Math.Min(width/4,height/3))),r=Math.Max(0,Math.Min(radius,Math.Min((width-2*s)/2,height/2))),k=.5522847498;
   var shape=new StreamGeometry();using(var g=shape.Open()){
    g.BeginFigure(new Point(0,0),true,true);g.LineTo(new Point(width,0),true,false);
    g.BezierTo(new Point(width-k*s,0),new Point(width-s,s-k*s),new Point(width-s,s),true,false);
    g.LineTo(new Point(width-s,height-r),true,false);g.BezierTo(new Point(width-s,height-r+k*r),new Point(width-s-r+k*r,height),new Point(width-s-r,height),true,false);
    g.LineTo(new Point(s+r,height),true,false);g.BezierTo(new Point(s+r-k*r,height),new Point(s,height-r+k*r),new Point(s,height-r),true,false);
    g.LineTo(new Point(s,s),true,false);g.BezierTo(new Point(s,s-k*s),new Point(k*s,0),new Point(0,0),true,false);
   }shape.Freeze();return shape;
  }
  protected override void OnRender(DrawingContext dc){Rect box=VisibleBounds.IsEmpty?new Rect(0,0,ActualWidth,ActualHeight):VisibleBounds;dc.PushTransform(new TranslateTransform(box.X,box.Y));dc.DrawGeometry(Background,null,Outline(box.Width,box.Height,Shoulder,CornerRadius.BottomLeft));dc.Pop();}
 }
}
