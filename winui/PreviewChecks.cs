// Render our own gallery controls for visual QA; this does not capture desktop pixels.
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ada;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
namespace Veilix;
public sealed partial class MainWindow {
 async Task CheckHiddenGripPixels(){
  var bitmap=new RenderTargetBitmap();
  await bitmap.RenderAsync(surface);
  var buffer=await bitmap.GetPixelsAsync();
  var bytes=new byte[(int)buffer.Length];
  using(var reader=DataReader.FromBuffer(buffer))reader.ReadBytes(bytes);
  int w=bitmap.PixelWidth,h=bitmap.PixelHeight;
  Check(w>64&&h>8,"Hidden grip pixel fixture is empty");
  int middle=w/2;
  int limit=(int)Math.Ceiling((edgeGrip.Width+4)*w/width/2);
  bool marker=false;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   byte alpha=bytes[(y*w+x)*4+3];
   if(Math.Abs(x-middle)>limit)Check(alpha==0,"Hidden panel still paints a rectangular surround");
   else if(alpha>0)marker=true;
  }
  Check(marker,"Hidden panel did not paint the grip");
 }
 async Task CheckRenderedCurvePixels(){
  if(prefs.Material=="Şeffaf"){
   var bitmap=new RenderTargetBitmap();
   await bitmap.RenderAsync(transparentTop);
   var buffer=await bitmap.GetPixelsAsync();
   var bytes=new byte[(int)buffer.Length];
   using(var reader=DataReader.FromBuffer(buffer))reader.ReadBytes(bytes);
   int w=bitmap.PixelWidth,h=bitmap.PixelHeight;
   Check(w>50&&h>8,"Joined transparent curve tint did not render");
   int seam=(int)Math.Round(14*w/(width+28));
   int Alpha(int x,int y)=>bytes[(y*w+x)*4+3];
   int expected=((LinearGradientBrush)transparentTop.Fill).GradientStops[0].Color.A;
   foreach(int edge in new[]{seam,w-seam}){
    for(int x=edge-2;x<=edge+2;x++)Check(Math.Abs(Alpha(x,2)-expected)<=2,"Transparent tint has an alpha seam");
   }
   Check(Alpha(1,h-2)==0&&Alpha(w-2,h-2)==0,"Joined tint paints outside either shoulder");
   return;
  }
  for(int i=0;i<shoulders.Count;i++){
   var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(shoulders[i].Grid,56,56);var buffer=await bitmap.GetPixelsAsync();var bytes=new byte[(int)buffer.Length];using(var reader=DataReader.FromBuffer(buffer))reader.ReadBytes(bytes);
   int w=bitmap.PixelWidth,h=bitmap.PixelHeight;
   int OutsideAlpha(int x,int y)=>bytes[(y*w+x)*4+3];
   int outerX=i==0?w/8:w*7/8;int innerX=i==0?w*7/8:w/8;
   Check(OutsideAlpha(outerX,h*7/8)==0,"Rectangle pixels remain outside rendered curve");
   var fill=shoulders[i].Tint.Fill as Microsoft.UI.Xaml.Media.LinearGradientBrush;Check(fill!=null&&fill.GradientStops.Count>0,"Curve gradient missing");int min=fill.GradientStops.Min(s=>s.Color.A),max=fill.GradientStops.Max(s=>s.Color.A),actual=OutsideAlpha(innerX,h/8);
   Check(actual>=Math.Max(1,min-2)&&actual<=Math.Min(255,max+2),$"Curve interior alpha {actual}, expected {min}..{max}: {prefs.Material}/{prefs.Theme}, tint={prefs.Tint}");
  }
 }
 async Task RenderGalleryPreview(string path){
  var previous=page.Background;
  try{
   page.Background=B(ThemePalette.Get(prefs).Start);root.UpdateLayout();await Task.Delay(60);
   var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(page);var buffer=await bitmap.GetPixelsAsync();var bytes=new byte[(int)buffer.Length];using(var reader=DataReader.FromBuffer(buffer))reader.ReadBytes(bytes);
   using var output=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,output);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,bytes);await encoder.FlushAsync();output.Seek(0);using var data=new DataReader(output.GetInputStreamAt(0));await data.LoadAsync((uint)output.Size);var png=new byte[(int)output.Size];data.ReadBytes(png);File.WriteAllBytes(path,png);
  }finally{page.Background=previous;}
 }
}
