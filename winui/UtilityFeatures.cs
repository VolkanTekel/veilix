using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
namespace Ada;
public sealed class NoticeEntry {
 public string Id=Guid.NewGuid().ToString("N"),Kind,Title,Detail,Icon,Route;
 public long AtUtc;public bool Read;
}
public sealed partial class UtilityData {
 public List<NoticeEntry> Notices=new();
 public bool NoticeBanners=true,TaskNotices=true,TimerNotices=true,VolumeNotices=true,KeyboardNotices=true,NetworkNotices=true,PowerNotices=true;
}
public sealed partial class UtilitySession {
 public const int NoticeLimit=100;
 public void NormalizeNotices(){Data.Notices=(Data.Notices??new()).Where(n=>n!=null&&n.Kind!="system-caps"&&!String.IsNullOrWhiteSpace(n.Title)&&IsNoticeFromToday(n,Now)).OrderByDescending(n=>n.AtUtc).Take(NoticeLimit).ToList();}
 static bool IsNoticeFromToday(NoticeEntry entry,DateTimeOffset now)
 {
  if(entry.AtUtc<0||entry.AtUtc>DateTimeOffset.MaxValue.UtcTicks)return false;
  return new DateTimeOffset(entry.AtUtc,TimeSpan.Zero).LocalDateTime.Date==now.LocalDateTime.Date;
 }
 internal bool PruneOldNotices()
 {
  Data.Notices ??= new List<NoticeEntry>();
  return Data.Notices.RemoveAll(n=>n==null||!IsNoticeFromToday(n,Now))>0;
 }
 public bool NoticeEnabled(string kind){if(kind==null||kind=="system-caps")return false;if(kind.StartsWith("system-",StringComparison.Ordinal)){if(!Data.SystemNotices)return false;return kind=="system-volume"?Data.VolumeNotices:kind=="system-network"?Data.NetworkNotices:kind=="system-power"?Data.PowerNotices:Data.KeyboardNotices;}if(kind.StartsWith("task-",StringComparison.Ordinal))return Data.TaskNotices;return kind=="pomodoro"||kind=="countdown"?Data.TimerNotices:true;}
 public void RecordNotice(UtilityNotice value){
  NormalizeNotices();var previous=Data.Notices.FirstOrDefault();bool merge=value.Kind.StartsWith("system-",StringComparison.Ordinal)&&previous?.Kind==value.Kind&&Now.UtcTicks-previous.AtUtc<TimeSpan.FromSeconds(10).Ticks;
  var entry=merge?previous:new NoticeEntry();entry.Kind=value.Kind;entry.Title=value.Title;entry.Detail=value.Detail;entry.Icon=value.Icon??"bell";entry.Route=value.Route;entry.AtUtc=Now.UtcTicks;entry.Read=false;if(!merge)Data.Notices.Insert(0,entry);if(Data.Notices.Count>NoticeLimit)Data.Notices.RemoveRange(NoticeLimit,Data.Notices.Count-NoticeLimit);
 }
 internal static List<string> CleanShelfFolder(string root,Action<string> remove){
  root=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);var errors=new List<string>();if(!Directory.Exists(root))return errors;
  if((File.GetAttributes(root)&FileAttributes.ReparsePoint)!=0)throw new IOException("Raf klasörü bir bağlantı olamaz.");
  foreach(string entry in Directory.GetFileSystemEntries(root))try{string target=Path.GetFullPath(entry);if(!Inside(target,root)||!String.Equals(Path.GetDirectoryName(target),root,StringComparison.OrdinalIgnoreCase))throw new IOException("Raf dışındaki dosyaya erişim engellendi.");remove(target);}catch(Exception ex){errors.Add(Path.GetFileName(entry)+": "+ex.Message);}return errors;
 }
 public async Task<List<string>> ClearShelf(){
  if(Copying)return new(){"Önce devam eden dosya işlemi bitsin."};Copying=true;
  try{var errors=await Task.Run(()=>CleanShelfFolder(ShelfRoot,path=>{var attr=File.GetAttributes(path);if((attr&FileAttributes.ReparsePoint)!=0){if((attr&FileAttributes.Directory)!=0)Directory.Delete(path,false);else File.Delete(path);}else if((attr&FileAttributes.Directory)!=0)FileSystem.DeleteDirectory(path,UIOption.OnlyErrorDialogs,RecycleOption.SendToRecycleBin,UICancelOption.ThrowException);else FileSystem.DeleteFile(path,UIOption.OnlyErrorDialogs,RecycleOption.SendToRecycleBin,UICancelOption.ThrowException);}));Data.Shelf.RemoveAll(x=>!File.Exists(x.Path)&&!Directory.Exists(x.Path));Save();return errors;}finally{Copying=false;ShelfChanged?.Invoke();}
 }
}
