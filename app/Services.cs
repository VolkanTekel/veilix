using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Ada {
 public sealed class Settings {
  public string DisplayId="",Theme="Grafit",Material="Cam",MediaLayout="Dengeli",WeatherSide="Sağ";
  public double Tint=0.78,Scale=1;
  public string CustomStart="#202630",CustomEnd="#384552",CustomAccent="#A5C7C5";
#if WINUI
  public List<ThemeStop> CustomStops=new List<ThemeStop>();
#endif
#if WINUI
  public string Language="tr";
  public bool ClipboardHistoryEnabled=true, OtherMediaSources, KeepHiddenAfterOverlap;
  public List<string> CompactOrder=new List<string>();
#endif
  public int DesignVersion=2;
  public int FrameLimit=120;
  public bool ReducedMotion=false,HideFullscreen=true,AutoHideOverlap=true,ShowWeather=true,ShowClock=true,Gradient=true,Marquee=true;
  public string City="";public double Latitude,Longitude;
  public static readonly string DirectoryPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data");
  public static readonly string FilePath=Path.Combine(DirectoryPath,"settings.json");
  public static Settings Load(){try{string raw=File.ReadAllText(FilePath);var s=new JavaScriptSerializer().Deserialize<Settings>(raw);if(raw.IndexOf("DesignVersion",StringComparison.Ordinal)<0){s.Scale=1;s.WeatherSide="Sağ";}s.Validate();return s;}catch{return new Settings();}}
  public void Validate(){if(Theme=="Gece")Theme="Grafit";if(Theme=="Aydınlık")Theme="Porselen";if(Theme=="Orman")Theme="Adaçayı";if(Theme=="Porselen")Theme="Kömür";if(Theme=="Lavanta")Theme="İndigo";if(Theme=="Gül")Theme="Bordo";if(Theme=="Kum")Theme="Kehribar";if(Theme=="Buz")Theme="Petrol";if(Theme!="Özel"&&!ThemePalette.Presets.Any(p=>p.Name==Theme))Theme="Grafit";if(!ThemePalette.ValidHex(CustomStart))CustomStart="#202630";if(!ThemePalette.ValidHex(CustomEnd))CustomEnd="#384552";if(!ThemePalette.ValidHex(CustomAccent))CustomAccent="#A5C7C5";if(!new[]{"Cam","Mika","Düz","Şeffaf"}.Contains(Material))Material="Düz";if(MediaLayout=="Klasik")MediaLayout="Dengeli";if(!new[]{"Dengeli","Kapak","Minimal"}.Contains(MediaLayout))MediaLayout="Dengeli";if(WeatherSide!="Sol")WeatherSide="Sağ";Tint=Clamp(Tint,.25,1);Scale=Clamp(Scale,.85,1.2);if(FrameLimit!=60&&FrameLimit!=90&&FrameLimit!=120)FrameLimit=120;Latitude=Clamp(Latitude,-90,90);Longitude=Clamp(Longitude,-180,180);DesignVersion=2;}
  static double Clamp(double v,double lo,double hi){return Double.IsNaN(v)||Double.IsInfinity(v)?lo:Math.Max(lo,Math.Min(hi,v));}
  public void Save(){Validate();Directory.CreateDirectory(DirectoryPath);var tmp=FilePath+".tmp";File.WriteAllText(tmp,new JavaScriptSerializer().Serialize(this));if(File.Exists(FilePath))File.Replace(tmp,FilePath,null);else File.Move(tmp,FilePath);}
 }
 public sealed class BrightnessItem {
  public string Name,DisplayId,Method,Error="",Instance;public int Value;internal IntPtr Physical;internal uint Min,Max;internal bool Vcp;
  public bool Supported{get{return Method!="Yok";}}
 }
 public sealed class BrightnessService : IDisposable {
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct PhysicalMonitor{public IntPtr Handle;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string Description;}
  [DllImport("dxva2.dll")]static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr h,out uint count);
  [DllImport("dxva2.dll",CharSet=CharSet.Unicode)]static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr h,uint count,[Out]PhysicalMonitor[] monitors);
  [DllImport("dxva2.dll")]static extern bool DestroyPhysicalMonitor(IntPtr h);
  [DllImport("dxva2.dll")]static extern bool GetMonitorBrightness(IntPtr h,out uint min,out uint current,out uint max);
  [DllImport("dxva2.dll")]static extern bool SetMonitorBrightness(IntPtr h,uint value);
  [DllImport("dxva2.dll")]static extern bool GetVCPFeatureAndVCPFeatureReply(IntPtr h,byte code,out uint type,out uint current,out uint max);
  [DllImport("dxva2.dll")]static extern bool SetVCPFeature(IntPtr h,byte code,uint value);
  readonly object gate=new object();public List<BrightnessItem> Items=new List<BrightnessItem>();bool disposed;
  public event Action Changed;
  public Task Discover(List<Display> displays){var snapshot=displays.ToArray();return Task.Run(delegate{lock(gate){if(disposed)return;Release();var discovered=new List<BrightnessItem>();
   foreach(var display in snapshot){
    bool found=false;uint count;
    if(GetNumberOfPhysicalMonitorsFromHMONITOR(display.Handle,out count)&&count>0&&count<16){var physical=new PhysicalMonitor[count];if(GetPhysicalMonitorsFromHMONITOR(display.Handle,count,physical))foreach(var p in physical){
     uint min=0,current=0,max=0,type;bool vcp=false,ok=false;
     for(int attempt=0;attempt<3&&!ok;attempt++){ok=GetMonitorBrightness(p.Handle,out min,out current,out max);if(!ok||max<=min){min=0;ok=GetVCPFeatureAndVCPFeatureReply(p.Handle,0x10,out type,out current,out max);vcp=ok;}if(!ok&&attempt<2)Thread.Sleep(120);}
     if(ok&&max>min){discovered.Add(new BrightnessItem{Name=display.Name,DisplayId=display.Id,Physical=p.Handle,Min=min,Max=max,Value=(int)((current-min)*100/(max-min)),Method="DDC/CI",Vcp=vcp});found=true;}else DestroyPhysicalMonitor(p.Handle);
    }}
    if(!found){var item=new BrightnessItem{Name=display.Name,DisplayId=display.Id,Method="Yok",Error="Bu ekran parlaklık kontrolünü bildirmiyor. Monitörde DDC/CI açık olmalı."};
     try{using(var search=new ManagementObjectSearcher("root\\WMI","SELECT InstanceName, CurrentBrightness FROM WmiMonitorBrightness"))using(var results=search.Get()){
      foreach(ManagementObject m in results){using(m){string instance=(string)m["InstanceName"];var parts=(display.HardwareId??"").Split('\\');string model=parts.Length>1?parts[1]:"";
       if(model.Length>0&&instance.IndexOf("\\"+model+"\\",StringComparison.OrdinalIgnoreCase)>=0){item.Instance=instance;item.Value=Convert.ToInt32(m["CurrentBrightness"]);item.Method="Dahili ekran";item.Error="";break;}
      }}
     }}catch{}discovered.Add(item);
    }
   }Items=discovered;
  }Notify();});}
  public Task<bool> Set(BrightnessItem item,double value){return Task.Run(delegate{lock(gate){if(disposed||!Items.Contains(item))return false;try{
   bool ok=false;int level=(int)Math.Round(Math.Max(0,Math.Min(100,value)));
   if(item.Physical!=IntPtr.Zero){uint raw=item.Min+(uint)((item.Max-item.Min)*level/100);ok=item.Vcp?SetVCPFeature(item.Physical,0x10,raw):SetMonitorBrightness(item.Physical,raw);}
   else if(item.Instance!=null){using(var search=new ManagementObjectSearcher("root\\WMI","SELECT * FROM WmiMonitorBrightnessMethods"))using(var results=search.Get())foreach(ManagementObject m in results){using(m){if((string)m["InstanceName"]==item.Instance){m.InvokeMethod("WmiSetBrightness",new object[]{(uint)0,(byte)level});ok=true;break;}}}}
   if(ok){item.Value=level;item.Error="";}else item.Error="Monitör parlaklık komutunu kabul etmedi.";Notify();return ok;
  }catch(Exception ex){item.Error=ex.Message;Notify();return false;}}});}
  void Notify(){var h=Changed;if(h!=null)h();}
  void Release(){var previous=Items;Items=new List<BrightnessItem>();foreach(var i in previous)if(i.Physical!=IntPtr.Zero)DestroyPhysicalMonitor(i.Physical);}
  public void Dispose(){lock(gate){disposed=true;Release();}}
 }
 public sealed class Place {
  public string Label,Name;public double Latitude,Longitude;
  public override string ToString(){return Label;}
 }
}
