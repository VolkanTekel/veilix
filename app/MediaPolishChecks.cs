// GPL-3.0-or-later. Artwork and rendering regressions used by --self-test.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ada {
 public sealed partial class AdaWindow {
  async Task CheckMediaPolish(string output,List<string> report){
   // Reproduce the observed Windows thumbnail: square album at the top,
   // black side margins, a separate monochrome player footer below it.
   var visual=new DrawingVisual();using(var dc=visual.RenderOpen()){
    dc.DrawRectangle(Brushes.Black,null,new Rect(0,0,240,240));dc.DrawRectangle(Brushes.IndianRed,null,new Rect(26,0,188,188));dc.DrawEllipse(Brushes.White,null,new Point(64,219),21,21);dc.DrawRectangle(Brushes.White,null,new Rect(94,207,100,20));
   }
   var branded=new RenderTargetBitmap(240,240,96,96,PixelFormats.Pbgra32);branded.Render(visual);branded.Freeze();
   var clean=MediaArtwork.SongCover(branded,"SpotifyAB.SpotifyMusic");if(clean.PixelWidth!=188||clean.PixelHeight!=188)throw new Exception("Spotify footer was not separated from artwork.");
   if(MediaArtwork.SongCover(branded,"OtherPlayer")!=branded)throw new Exception("Unrelated player artwork was cropped.");
   if(MediaArtwork.SongCover(clean,"Spotify.exe")!=clean)throw new Exception("Plain album artwork was cropped twice.");
   if(MediaArtwork.SongCover(null,"Spotify.exe")!=null)throw new Exception("Missing artwork failed.");
   double filled=0,regular=0;foreach(var g in FluentIcons.Get("pause-filled"))filled+=g.GetArea();foreach(var g in FluentIcons.Get("pause"))regular+=g.GetArea();if(regular>=filled*.65)throw new Exception("Pause icon retained its heavy silhouette.");
   if(TextOptions.GetTextFormattingMode(this)!=TextFormattingMode.Display||TextOptions.GetTextRenderingMode(this)!=TextRenderingMode.Grayscale||TextOptions.GetTextHintingMode(this)!=TextHintingMode.Fixed)throw new Exception("Text rasterization policy was lost.");
   var oldCover=Media.Cover;string oldTitle=Media.Title,oldArtist=Media.Artist;bool oldPlaying=Media.Playing;
   try{Media.Cover=clean;Media.Title="Stress Relief";Media.Artist="late night drive home";Media.Playing=true;Preferences.MediaLayout="Kapak";Preferences.Scale=.923244415454429;Preferences.ReducedMotion=true;Navigate("media");await Capture(output,"media-polish-playing");Media.Playing=false;MediaChanged();await Capture(output,"media-polish-paused");}
   finally{Media.Cover=oldCover;Media.Title=oldTitle;Media.Artist=oldArtist;Media.Playing=oldPlaying;Preferences.Scale=1;}
   report.Add("Media polish: observed branded-thumbnail layout extracted to 188x188; plain/other-player covers unchanged; pause icon ink="+(regular/filled*100).ToString("F1")+"% of filled version; Display/Grayscale/Fixed text at fractional scale; both transport states captured");
  }
 }
}
