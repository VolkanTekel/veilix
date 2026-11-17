// GPL-3.0-or-later. Local utility state and clocks; no continuously rendered UI.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
namespace Ada {
 public sealed class ShelfEntry {public string Id=Guid.NewGuid().ToString("N"),Name,Path,Original;public bool Folder;public long Bytes;}
 public sealed class TaskEntry {public string Id=Guid.NewGuid().ToString("N"),Title;public bool Done,Notified;public long DueUtc;}
 public sealed class WaterEntry {public int Ml;public long AtUtc;}
 public sealed class CountdownState {public bool Running;public double Remaining=1500,Total=1500;public long EndUtc;}
 public sealed partial class UtilityData {
  public List<ShelfEntry> Shelf=new List<ShelfEntry>();public List<TaskEntry> Tasks=new List<TaskEntry>();public List<WaterEntry> Drinks=new List<WaterEntry>();
  public CountdownState Pomodoro=new CountdownState(),Countdown=new CountdownState{Remaining=300,Total=300};
  public bool FocusPhase=true,StopwatchRunning,WaterEnabled=true,WaterSound,TimerSound=true,SystemNotices=true;
  public double StopwatchSeconds;public long StopwatchStartUtc,WaterNextUtc;public int FocusMinutes=25,BreakMinutes=5,PomodoroSessions,WaterGoal=2000,CupMl=250,WaterInterval=45,WaterFrom=9,WaterTo=22;
  public string WaterDay="";public List<double> Laps=new List<double>();
 }
 public sealed class UtilityNotice {public string Kind,Title,Detail,Icon,Route;public double Value=-1;public bool Sound;}
 public sealed partial class UtilitySession {
  public readonly UtilityData Data;readonly string statePath;readonly Func<DateTimeOffset> now;readonly object fileGate=new object();public event Action ShelfChanged;public bool Copying;
  public UtilitySession(bool ephemeral=false,string path=null,Func<DateTimeOffset> clock=null){now=clock??(()=>DateTimeOffset.UtcNow);statePath=ephemeral?null:path??System.IO.Path.Combine(Settings.DirectoryPath,"utilities.json");Data=Load(statePath);NewDay();if(Data.WaterNextUtc==0)Data.WaterNextUtc=Now.AddMinutes(Data.WaterInterval).UtcTicks;}
  public DateTimeOffset Now{get{return now();}}
  static UtilityData Load(string path){UtilityData value=null;try{if(path!=null&&File.Exists(path)&&new FileInfo(path).Length<2*1024*1024)value=new JavaScriptSerializer().Deserialize<UtilityData>(File.ReadAllText(path));}catch{}value=value??new UtilityData();value.Shelf=value.Shelf??new List<ShelfEntry>();value.Tasks=value.Tasks??new List<TaskEntry>();value.Drinks=value.Drinks??new List<WaterEntry>();value.Laps=value.Laps??new List<double>();value.Pomodoro=value.Pomodoro??new CountdownState();value.Countdown=value.Countdown??new CountdownState{Remaining=300,Total=300};value.WaterGoal=Math.Max(250,Math.Min(10000,value.WaterGoal));value.CupMl=Math.Max(50,Math.Min(1000,value.CupMl));value.WaterInterval=Math.Max(5,Math.Min(240,value.WaterInterval));value.WaterFrom=Math.Max(0,Math.Min(23,value.WaterFrom));value.WaterTo=Math.Max(0,Math.Min(23,value.WaterTo));value.FocusMinutes=Math.Max(1,Math.Min(180,value.FocusMinutes));value.BreakMinutes=Math.Max(1,Math.Min(60,value.BreakMinutes));return value;}
  public void Save(){if(statePath==null)return;lock(fileGate){Directory.CreateDirectory(System.IO.Path.GetDirectoryName(statePath));string temp=statePath+".tmp";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(Data));if(File.Exists(statePath))File.Replace(temp,statePath,null);else File.Move(temp,statePath);}}
  bool NewDay(){string day=Now.ToLocalTime().ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);if(Data.WaterDay==day)return false;Data.WaterDay=day;Data.Drinks.Clear();Data.WaterNextUtc=Now.AddMinutes(Data.WaterInterval).UtcTicks;return true;}
  public int WaterMl{get{return Data.Drinks.Sum(d=>d.Ml);}}
  public bool WaterHours{get{int hour=Now.ToLocalTime().Hour;return Data.WaterFrom==Data.WaterTo|| (Data.WaterFrom<Data.WaterTo?hour>=Data.WaterFrom&&hour<Data.WaterTo:hour>=Data.WaterFrom||hour<Data.WaterTo);}}
  public void Drink(int ml){NewDay();Data.Drinks.Add(new WaterEntry{Ml=ml,AtUtc=Now.UtcTicks});Data.WaterNextUtc=Now.AddMinutes(Data.WaterInterval).UtcTicks;Save();}
  public void SnoozeWater(){Data.WaterNextUtc=Now.AddMinutes(10).UtcTicks;Save();}
  public void ResetWaterSchedule(){Data.WaterNextUtc=Now.AddMinutes(Data.WaterInterval).UtcTicks;Save();}
  public double Remaining(CountdownState timer){return timer.Running?Math.Max(0,(timer.EndUtc-Now.UtcTicks)/(double)TimeSpan.TicksPerSecond):Math.Max(0,timer.Remaining);}
  public void ToggleTimer(CountdownState timer){if(timer.Running){timer.Remaining=Remaining(timer);timer.Running=false;}else{if(timer.Remaining<=0)timer.Remaining=timer.Total;timer.EndUtc=Now.AddSeconds(timer.Remaining).UtcTicks;timer.Running=true;}Save();}
  public void ResetTimer(CountdownState timer,double seconds){timer.Running=false;timer.Total=timer.Remaining=Math.Max(1,seconds);Save();}
  public double StopwatchElapsed{get{return Math.Max(0,Data.StopwatchSeconds+(Data.StopwatchRunning?Math.Max(0,(Now.UtcTicks-Data.StopwatchStartUtc)/(double)TimeSpan.TicksPerSecond):0));}}
  public void ToggleStopwatch(){if(Data.StopwatchRunning){Data.StopwatchSeconds=StopwatchElapsed;Data.StopwatchRunning=false;}else{Data.StopwatchStartUtc=Now.UtcTicks;Data.StopwatchRunning=true;}Save();}
  public void ResetStopwatch(){Data.StopwatchRunning=false;Data.StopwatchSeconds=0;Data.Laps.Clear();Save();}
  public string Activity {get{if(Data.Countdown.Running)return "Sayaç · "+Format(Remaining(Data.Countdown));if(Data.Pomodoro.Running)return (Data.FocusPhase?"Odak · ":"Mola · ")+Format(Remaining(Data.Pomodoro));if(Data.StopwatchRunning)return "Kronometre · "+Format(StopwatchElapsed);return "";}}
  public static string Format(double seconds){var t=TimeSpan.FromSeconds(Math.Max(0,seconds));return t.TotalHours>=1?((int)t.TotalHours)+":"+t.Minutes.ToString("00")+":"+t.Seconds.ToString("00"):((int)t.TotalMinutes).ToString("00")+":"+t.Seconds.ToString("00");}
  public List<UtilityNotice> Tick(){var notices=new List<UtilityNotice>();bool changed=NewDay();
   foreach(var timer in new[]{Data.Pomodoro,Data.Countdown})if(timer.Running&&Remaining(timer)<=0){timer.Running=false;timer.Remaining=0;bool focus=timer==Data.Pomodoro;if(focus&&Data.FocusPhase)Data.PomodoroSessions++;notices.Add(new UtilityNotice{Kind=focus?"pomodoro":"countdown",Title=focus?(Data.FocusPhase?"Odak tamamlandı":"Mola tamamlandı"):"Süre doldu",Detail=focus?(Data.FocusPhase?"Kısa bir mola verebilirsin.":"Yeni bir odak turuna hazırsın."):"Zamanlayıcın tamamlandı.",Icon="timer",Route="time",Sound=Data.TimerSound});changed=true;}
   foreach(var task in Data.Tasks)if(!task.Done&&!task.Notified&&task.DueUtc>0&&task.DueUtc<=Now.UtcTicks){task.Notified=true;notices.Add(new UtilityNotice{Kind="task-"+task.Id,Title="Bir hatırlatman var",Detail=task.Title,Icon="check",Route="tasks"});changed=true;}
   if(Data.WaterEnabled&&WaterHours&&WaterMl<Data.WaterGoal&&Data.WaterNextUtc<=Now.UtcTicks){notices.Add(new UtilityNotice{Kind="water",Title="Bir bardak suya ne dersin?",Detail=WaterMl+" / "+Data.WaterGoal+" ml · Gününe küçük bir ara.",Icon="drop",Route="water",Sound=Data.WaterSound,Value=WaterMl*100.0/Data.WaterGoal});Data.WaterNextUtc=Now.AddMinutes(Data.WaterInterval).UtcTicks;changed=true;}
   if(changed)Save();return notices;
  }
  public static string ShelfRoot{get{return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"Veilix","Raf");}}
  static bool Inside(string path,string folder){return System.IO.Path.GetFullPath(path).TrimEnd(System.IO.Path.DirectorySeparatorChar).StartsWith(System.IO.Path.GetFullPath(folder).TrimEnd(System.IO.Path.DirectorySeparatorChar)+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);}
  static void CopyFolder(string source,string destination){Directory.CreateDirectory(destination);foreach(var file in Directory.GetFiles(source)){if((File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)continue;File.Copy(file,System.IO.Path.Combine(destination,System.IO.Path.GetFileName(file)));}foreach(var folder in Directory.GetDirectories(source)){if((File.GetAttributes(folder)&FileAttributes.ReparsePoint)!=0)continue;CopyFolder(folder,System.IO.Path.Combine(destination,System.IO.Path.GetFileName(folder)));}}
  internal static ShelfEntry CopyOne(string source,string root){source=System.IO.Path.GetFullPath(source);root=System.IO.Path.GetFullPath(root);if(!File.Exists(source)&&!Directory.Exists(source))throw new IOException("Dosya artık bulunamıyor.");if((File.GetAttributes(source)&FileAttributes.ReparsePoint)!=0)throw new IOException("Bu bağlantı yerine asıl dosyayı ekle.");string name=System.IO.Path.GetFileName(source.TrimEnd(System.IO.Path.DirectorySeparatorChar));if(String.IsNullOrEmpty(name))throw new IOException("Bir dosya veya klasör seç.");bool folder=Directory.Exists(source);if(folder&&(Inside(root,source)||String.Equals(source,root,StringComparison.OrdinalIgnoreCase)))throw new IOException("Rafın kendisini içeren klasör buraya kopyalanamaz.");Directory.CreateDirectory(root);string temp=System.IO.Path.Combine(root,".veilix-"+Guid.NewGuid().ToString("N")+".partial");try{if(folder)CopyFolder(source,temp);else File.Copy(source,temp);for(int copy=1;copy<10000;copy++){string fileName=copy==1?name:folder?name+" ("+copy+")":System.IO.Path.GetFileNameWithoutExtension(name)+" ("+copy+")"+System.IO.Path.GetExtension(name);string target=System.IO.Path.Combine(root,fileName);if(File.Exists(target)||Directory.Exists(target))continue;try{if(folder)Directory.Move(temp,target);else File.Move(temp,target);return new ShelfEntry{Name=fileName,Path=target,Original=source,Folder=folder,Bytes=folder?0:new FileInfo(target).Length};}catch(IOException){if(File.Exists(target)||Directory.Exists(target))continue;throw;}}throw new IOException("Bu ada sahip çok fazla dosya var.");}finally{if(Inside(temp,root)){if(File.Exists(temp))File.Delete(temp);else if(Directory.Exists(temp))Directory.Delete(temp,true);}}}
  public async Task<List<string>> Import(string[] paths){if(Copying)return new List<string>{"Önce devam eden kopyalama bitsin."};Copying=true;var errors=new List<string>();try{foreach(string path in paths)try{var entry=await Task.Run(()=>CopyOne(path,ShelfRoot));Data.Shelf.Add(entry);Save();}catch(Exception ex){errors.Add(System.IO.Path.GetFileName(path)+": "+ex.Message);}}finally{Copying=false;var handler=ShelfChanged;if(handler!=null)handler();}return errors;}
 }
}
