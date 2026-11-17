// GPL-3.0-or-later. Extract the album tile from Spotify's Windows thumbnail.
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ada {
 internal static class ArtworkCodec {
  internal static BitmapSource Decode(byte[] bytes){using(var stream=new System.IO.MemoryStream(bytes)){var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.DecodePixelWidth=512;bitmap.StreamSource=stream;bitmap.EndInit();bitmap.Freeze();return bitmap;}}
  internal static byte[] Encode(BitmapSource bitmap){var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=new System.IO.MemoryStream()){png.Save(stream);return stream.ToArray();}}
 }
 public static class MediaArtwork {
  public static BitmapSource SongCover(BitmapSource source,string appId){
   if(source==null||String.IsNullOrEmpty(appId)||appId.IndexOf("spotify",StringComparison.OrdinalIgnoreCase)<0)return source;
   int w=source.PixelWidth,h=source.PixelHeight;if(w<96||Math.Abs(w-h)>1)return source;
   var bitmap=new FormatConvertedBitmap(source,PixelFormats.Bgra32,null,0);int stride=w*4;var pixels=new byte[stride*h];bitmap.CopyPixels(pixels,stride,0);
   int left=w,right=-1,sideBlack=0,sideCount=0,white=0,black=0,footerCount=0,colored=0;
   // The branded layout has a centered ~78.5% square at the top, black side
   // margins and a monochrome Spotify footer. Plain album covers stay intact.
   for(int y=0;y<(int)(h*.78);y+=2)for(int x=0;x<w;x++){
    int i=y*stride+x*4,max=Math.Max(pixels[i],Math.Max(pixels[i+1],pixels[i+2]));
    if(max>24){left=Math.Min(left,x);right=Math.Max(right,x);}
    if(x<w*.08||x>w*.92){sideCount++;if(max<=24)sideBlack++;}
   }
   for(int y=(int)(h*.82);y<h;y++)for(int x=0;x<w;x++){
    int i=y*stride+x*4,max=Math.Max(pixels[i],Math.Max(pixels[i+1],pixels[i+2])),min=Math.Min(pixels[i],Math.Min(pixels[i+1],pixels[i+2]));
    footerCount++;if(max<=24)black++;if(min>=220)white++;if(max-min>16)colored++;
   }
   int size=right-left+1;
   if(size<w*.77||size>w*.81||Math.Abs(left-(w-right-1))>2||sideBlack<sideCount*.99||black<footerCount*.55||white<footerCount*.08||white>footerCount*.42||colored>footerCount*.015)return source;
   var result=new CroppedBitmap(source,new Int32Rect(left,0,size,size));result.Freeze();return result;
  }
 }
}
