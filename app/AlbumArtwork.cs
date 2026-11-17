// GPL-3.0-or-later. One antialiased drawing with concentric image/frame arcs.
using System;
using System.Windows;
using System.Windows.Media;
namespace Ada {
 public sealed class AlbumArtwork:FrameworkElement {
  public static readonly DependencyProperty SourceProperty=DependencyProperty.Register("Source",typeof(ImageSource),typeof(AlbumArtwork),new FrameworkPropertyMetadata(null,FrameworkPropertyMetadataOptions.AffectsRender,SourceChanged));
  public static readonly DependencyProperty RadiusProperty=DependencyProperty.Register("Radius",typeof(double),typeof(AlbumArtwork),new FrameworkPropertyMetadata(16.0,FrameworkPropertyMetadataOptions.AffectsRender));
  public static readonly DependencyProperty InsetProperty=DependencyProperty.Register("Inset",typeof(double),typeof(AlbumArtwork),new FrameworkPropertyMetadata(0.0,FrameworkPropertyMetadataOptions.AffectsRender));
  public static readonly DependencyProperty FrameProperty=DependencyProperty.Register("Frame",typeof(Brush),typeof(AlbumArtwork),new FrameworkPropertyMetadata(Brushes.Transparent,FrameworkPropertyMetadataOptions.AffectsRender));
  ImageBrush image;
  public ImageSource Source{get{return (ImageSource)GetValue(SourceProperty);}set{SetValue(SourceProperty,value);}}
  public double Radius{get{return (double)GetValue(RadiusProperty);}set{SetValue(RadiusProperty,value);}}
  public double Inset{get{return (double)GetValue(InsetProperty);}set{SetValue(InsetProperty,value);}}
  public Brush Frame{get{return (Brush)GetValue(FrameProperty);}set{SetValue(FrameProperty,value);}}
  public AlbumArtwork(){SnapsToDevicePixels=false;RenderOptions.SetEdgeMode(this,EdgeMode.Unspecified);RenderOptions.SetBitmapScalingMode(this,BitmapScalingMode.HighQuality);}
  static void SourceChanged(DependencyObject owner,DependencyPropertyChangedEventArgs e){var art=(AlbumArtwork)owner;art.image=e.NewValue==null?null:new ImageBrush((ImageSource)e.NewValue){Stretch=Stretch.UniformToFill,AlignmentX=AlignmentX.Center,AlignmentY=AlignmentY.Center};if(art.image!=null&&art.image.CanFreeze)art.image.Freeze();}
  protected override void OnRender(DrawingContext dc){
   double w=ActualWidth,h=ActualHeight;if(w<=0||h<=0)return;double radius=Math.Max(0,Math.Min(Radius,Math.Min(w,h)/2));
   double inset=Math.Max(0,Math.Min(Inset,Math.Min(w,h)/2));dc.DrawRoundedRectangle(Frame,null,new Rect(0,0,w,h),radius,radius);
   if(image!=null&&w>2*inset&&h>2*inset){double inner=Math.Max(0,radius-inset);dc.DrawRoundedRectangle(image,null,new Rect(inset,inset,w-2*inset,h-2*inset),inner,inner);}
  }
 }
}
