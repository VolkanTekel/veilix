// Album artwork is bounded PNG data. No WPF render/dispatcher assemblies are
// needed by the WinUI frontend, and temporary GDI resources are always disposed.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
namespace Ada;
public sealed class Artwork {
 public readonly byte[] Png;public readonly int PixelWidth,PixelHeight;
 internal Artwork(byte[] png,int width,int height){Png=png;PixelWidth=width;PixelHeight=height;}
}
internal static class ArtworkCodec {
 internal static Artwork Decode(byte[] bytes){
  if(bytes==null||bytes.Length==0||bytes.Length>8*1024*1024)throw new IOException("Kapak verisi geçersiz.");
  using var stream=new MemoryStream(bytes,false);using var image=Image.FromStream(stream,true,true);
  if(image.Width<1||image.Height<1||image.Width>8192||image.Height>8192)throw new IOException("Kapak boyutu geçersiz.");
  double scale=Math.Min(1,512.0/Math.Max(image.Width,image.Height));int width=Math.Max(1,(int)Math.Round(image.Width*scale)),height=Math.Max(1,(int)Math.Round(image.Height*scale));
  using var bitmap=new Bitmap(width,height,PixelFormat.Format32bppArgb);using(var graphics=Graphics.FromImage(bitmap)){graphics.CompositingMode=CompositingMode.SourceCopy;graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;graphics.DrawImage(image,new Rectangle(0,0,width,height));}return FromBitmap(bitmap);
 }
 internal static byte[] Encode(Artwork artwork)=>artwork.Png;
 internal static Artwork FromBitmap(Bitmap bitmap){using var output=new MemoryStream();bitmap.Save(output,ImageFormat.Png);return new Artwork(output.ToArray(),bitmap.Width,bitmap.Height);}
}
public static class MediaArtwork {
 public static Artwork SongCover(Artwork source,string appId){
  if(source==null||String.IsNullOrEmpty(appId)||appId.IndexOf("spotify",StringComparison.OrdinalIgnoreCase)<0)return source;
  int w=source.PixelWidth,h=source.PixelHeight;if(w<96||Math.Abs(w-h)>1)return source;
  using var stream=new MemoryStream(source.Png,false);using var image=new Bitmap(stream);using var bitmap=image.Clone(new Rectangle(0,0,w,h),PixelFormat.Format32bppArgb);
  var data=bitmap.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);var pixels=new byte[w*h*4];try{for(int y=0;y<h;y++)Marshal.Copy(data.Scan0+y*data.Stride,pixels,y*w*4,w*4);}finally{bitmap.UnlockBits(data);}
  int stride=w*4,left=w,right=-1,sideBlack=0,sideCount=0,white=0,black=0,footerCount=0,colored=0;
  for(int y=0;y<(int)(h*.78);y+=2)for(int x=0;x<w;x++){int i=y*stride+x*4,max=Math.Max(pixels[i],Math.Max(pixels[i+1],pixels[i+2]));if(max>24){left=Math.Min(left,x);right=Math.Max(right,x);}if(x<w*.08||x>w*.92){sideCount++;if(max<=24)sideBlack++;}}
  for(int y=(int)(h*.82);y<h;y++)for(int x=0;x<w;x++){int i=y*stride+x*4,max=Math.Max(pixels[i],Math.Max(pixels[i+1],pixels[i+2])),min=Math.Min(pixels[i],Math.Min(pixels[i+1],pixels[i+2]));footerCount++;if(max<=24)black++;if(min>=220)white++;if(max-min>16)colored++;}
  int size=right-left+1;if(size<w*.77||size>w*.81||Math.Abs(left-(w-right-1))>2||sideBlack<sideCount*.99||black<footerCount*.55||white<footerCount*.08||white>footerCount*.42||colored>footerCount*.015)return source;
  using var crop=bitmap.Clone(new Rectangle(left,0,size,size),PixelFormat.Format32bppArgb);return ArtworkCodec.FromBitmap(crop);
 }
}
