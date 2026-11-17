// GPL-3.0-or-later. Verify image/frame geometry and coverage, not only layout.
using System;using System.Collections.Generic;using System.IO;using System.Threading.Tasks;using System.Windows;using System.Windows.Media;using System.Windows.Media.Imaging;
namespace Ada {
 public sealed partial class AdaWindow {
  async Task CheckArtworkEdges(string output,List<string> report){
   var solid=BitmapSource.Create(1,1,96,96,PixelFormats.Bgra32,null,new byte[]{90,150,220,255},4);solid.Freeze();
   foreach(double scale in new[]{.85,.8871635,1.0,1.25,1.6})foreach(int size in new[]{40,48,88,170}){
    double radius=size==40?16:Math.Min(20,size/4.0),inset=size==40?0:size>=88?3:2;
    var art=new AlbumArtwork{Width=size,Height=size,Radius=radius,Inset=inset,Source=solid,Frame=Brushes.SlateGray};art.Measure(new Size(size,size));art.Arrange(new Rect(0,0,size,size));art.UpdateLayout();int pixels=(int)Math.Ceiling(size*scale);var bmp=new RenderTargetBitmap(pixels,pixels,96*scale,96*scale,PixelFormats.Pbgra32);bmp.Render(art);var data=new byte[pixels*pixels*4];bmp.CopyPixels(data,pixels*4,0);int partial=0;
    for(int y=0;y<pixels;y++)for(int x=0;x<pixels;x++){int alpha=data[(y*pixels+x)*4+3];if(alpha>0&&alpha<255)partial++;}
    if(partial<8||data[3]!=0||data[((pixels/2)*pixels+pixels/2)*4+3]!=255)throw new Exception("Artwork antialiasing/coverage failed at "+size+" x "+scale+": partial="+partial);
    if(Math.Abs((inset+radius-inset)-radius)>.001||art.Clip!=null)throw new Exception("Image and frame are not concentric or use hard clipping.");
    if(size==170&&Math.Abs(scale-.8871635)<.00001){var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bmp));using(var file=File.Create(Path.Combine(output,"artwork-antialias.png")))png.Save(file);}
   }
   Preferences.ReducedMotion=true;Preferences.Scale=.887163509471585;Preferences.Material="Düz";Media.Cover=solid;Media.Title="Concentric corners";Media.Artist="Veilix";Media.Playing=true;Navigate("");SetHeaderHovered(true);await Capture(output,"concentric-compact");
   var point=miniCover.TranslatePoint(new Point(miniCover.Radius,miniCover.ActualHeight-miniCover.Radius),header);if(Math.Abs(point.X-26)>.1||Math.Abs(point.Y-(HeaderHeight-26))>.1)throw new Exception("Compact cover and shell corner centers differ.");
   Navigate("media");await Capture(output,"concentric-media");Preferences.Scale=1;
   report.Add("Artwork: 40/48/88/170 DIP at five fractional/DPI scales retain partial-alpha edge pixels, transparent corners and opaque center; image/frame arcs share center; compact cover and shell bottom-left center coincide; shell radius scales with content; settled transparent child mask avoids hard-clipping border AA");
  }
 }
}
