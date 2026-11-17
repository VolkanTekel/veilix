// GPL-3.0-or-later. One local last-track snapshot, not a playback history list.
using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
#if WINUI
using BitmapSource=Ada.Artwork;
#else
using System.Windows.Media.Imaging;
#endif
namespace Ada {
 internal static class SpotifyIdentity {
  public static bool Matches(string id){return !String.IsNullOrWhiteSpace(id)&&(id.Equals("Spotify",StringComparison.OrdinalIgnoreCase)||id.Equals("Spotify.exe",StringComparison.OrdinalIgnoreCase)||id.StartsWith("SpotifyAB.SpotifyMusic",StringComparison.OrdinalIgnoreCase)||id.StartsWith("com.squirrel.Spotify.",StringComparison.OrdinalIgnoreCase));}
 }
 internal sealed class SpotifySnapshot {public string Title,Artist,AppId;public double Position,Duration;public BitmapSource Cover;}
 internal sealed class SpotifyHistory {
  sealed class SavedTrack {public int Version=1;public string Title,Artist,AppId,Cover;public double Position,Duration;}
  static readonly object fileGate=new object();readonly object queueGate=new object();readonly string path;SpotifySnapshot pending;Task writer=Task.FromResult(0);
  public SpotifyHistory(string path){this.path=path;}
  public SpotifySnapshot Load(){try{lock(fileGate){if(!File.Exists(path)||new FileInfo(path).Length>2*1024*1024)return null;var saved=new JavaScriptSerializer().Deserialize<SavedTrack>(File.ReadAllText(path));if(saved==null||saved.Version!=1||!SpotifyIdentity.Matches(saved.AppId)||String.IsNullOrWhiteSpace(saved.Title))return null;BitmapSource cover=null;if(!String.IsNullOrEmpty(saved.Cover))try{cover=ArtworkCodec.Decode(Convert.FromBase64String(saved.Cover));}catch{}return new SpotifySnapshot{Title=saved.Title,Artist=saved.Artist??"",AppId=saved.AppId,Position=Finite(saved.Position),Duration=Finite(saved.Duration),Cover=cover};}}catch{return null;}}
  static double Finite(double value){return Double.IsNaN(value)||Double.IsInfinity(value)?0:Math.Max(0,Math.Min(604800,value));}
  public void Save(SpotifySnapshot snapshot){lock(queueGate){pending=snapshot;if(!writer.IsCompleted)return;writer=Task.Run((Action)Drain);}}
  public Task Flush(){lock(queueGate)return writer;}
  void Drain(){while(true){SpotifySnapshot next;lock(queueGate){next=pending;pending=null;if(next==null){writer=Task.FromResult(0);return;}}try{Write(next);}catch{}}}
  void Write(SpotifySnapshot snapshot){
   if(!SpotifyIdentity.Matches(snapshot.AppId)||String.IsNullOrWhiteSpace(snapshot.Title))return;
   var data=new SavedTrack{Title=snapshot.Title,Artist=snapshot.Artist??"",AppId=snapshot.AppId,Position=Finite(snapshot.Position),Duration=Finite(snapshot.Duration)};
   if(snapshot.Cover!=null)try{var bytes=ArtworkCodec.Encode(snapshot.Cover);if(bytes.Length<=1024*1024)data.Cover=Convert.ToBase64String(bytes);}catch{}
   lock(fileGate){Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(data));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
  }
 }
}
