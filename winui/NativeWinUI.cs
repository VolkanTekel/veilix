using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Ada {
 public sealed class Display {
  public string Id,Name,HardwareId; public Native.Rect Bounds,Work;public int Hz=60;public double Dpi=96; public IntPtr Handle;public bool Primary;
  public override string ToString(){return Veilix.UiLanguage.DisplayName(Name)+" · "+Bounds.Width+" × "+Bounds.Height+" · "+Hz+" Hz"+(Primary?Veilix.UiLanguage.L(" · Ana ekran"):"");}
 }
 public static class Native {
  static readonly uint OwnProcessId=(uint)System.Diagnostics.Process.GetCurrentProcess().Id;
  [StructLayout(LayoutKind.Sequential)]public struct Rect {public int Left,Top,Right,Bottom;public int Width{get{return Right-Left;}}public int Height{get{return Bottom-Top;}}}
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct MonitorInfo {public int Size;public Rect Monitor,Work;public uint Flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string Device;}
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct DisplayDevice {public int Size;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string DeviceName;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string DeviceString;public int StateFlags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string DeviceId;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string DeviceKey;}
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct DevMode {
   [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string Device;public ushort SpecVersion,DriverVersion,Size,DriverExtra;public uint Fields;public int X,Y;public uint Orientation,FixedOutput;public short Color,Duplex,YResolution,TTOption,Collate;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string Form;public ushort LogPixels;public uint BitsPerPel,PelsWidth,PelsHeight,Flags,Frequency,ICMMethod,ICMIntent,MediaType,DitherType,Reserved1,Reserved2,PanningWidth,PanningHeight;
  }
  [StructLayout(LayoutKind.Sequential)]public struct Margins {public int Left,Right,Top,Bottom;}
  delegate bool MonitorEnum(IntPtr monitor,IntPtr hdc,IntPtr rect,IntPtr data);
  delegate bool WindowEnum(IntPtr window,IntPtr data);
  [DllImport("user32.dll")]static extern bool EnumWindows(WindowEnum callback,IntPtr data);
  [DllImport("user32.dll")]static extern bool EnumDisplayMonitors(IntPtr hdc,IntPtr clip,MonitorEnum proc,IntPtr data);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern bool GetMonitorInfo(IntPtr handle,ref MonitorInfo info);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern bool EnumDisplaySettings(string device,int number,ref DevMode mode);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern bool EnumDisplayDevices(string device,uint number,ref DisplayDevice display,uint flags);
  [DllImport("shcore.dll")]static extern int GetDpiForMonitor(IntPtr monitor,int type,out uint x,out uint y);
  [DllImport("dwmapi.dll")]public static extern int DwmSetWindowAttribute(IntPtr handle,int attribute,ref int value,int size);
  [DllImport("dwmapi.dll")]public static extern int DwmExtendFrameIntoClientArea(IntPtr handle,ref Margins margins);
  [DllImport("gdi32.dll")]static extern IntPtr CreateRoundRectRgn(int l,int t,int r,int b,int w,int h);
  [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr obj);
  [DllImport("gdi32.dll")]static extern IntPtr CreateRectRgn(int left,int top,int right,int bottom);
  [DllImport("gdi32.dll")]static extern int CombineRgn(IntPtr dest,IntPtr a,IntPtr b,int mode);
  [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]static extern IntPtr GetWindowLongPtr(IntPtr hwnd,int index);
  [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")]static extern IntPtr SetWindowLongPtr(IntPtr hwnd,int index,IntPtr value);
  [DllImport("user32.dll")]static extern int SetWindowRgn(IntPtr handle,IntPtr region,bool redraw);
  internal static bool TrySetWindowRegion(IntPtr hwnd,(int Left,int Right,int Bottom)? bounds){
   if(bounds is not { } rect)return SetWindowRgn(hwnd,IntPtr.Zero,true)!=0;
   IntPtr region=CreateRectRgn(rect.Left,0,rect.Right,rect.Bottom);
   if(region==IntPtr.Zero)return false;
   if(SetWindowRgn(hwnd,region,true)!=0)return true;
   DeleteObject(region);
   return false;
  }
  [DllImport("user32.dll")]public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")]public static extern bool GetWindowRect(IntPtr handle,out Rect rect);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr hwnd,StringBuilder name,int max);
  [DllImport("user32.dll")]static extern bool IsIconic(IntPtr handle);
  [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr handle);
  [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr handle,out uint processId);
  [DllImport("dwmapi.dll")]static extern int DwmGetWindowAttribute(IntPtr handle,int attribute,out Rect value,int size);
  [DllImport("dwmapi.dll")]static extern int DwmGetWindowAttribute(IntPtr handle,int attribute,out int value,int size);
  [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int cx,int cy,uint flags);
  [StructLayout(LayoutKind.Sequential)]struct AccentPolicy{public int State,Flags;public uint Color;public int Animation;}
  [StructLayout(LayoutKind.Sequential)]struct CompositionData{public int Attribute;public IntPtr Data;public int Size;}
  [DllImport("user32.dll")]static extern bool SetWindowCompositionAttribute(IntPtr hwnd,ref CompositionData data);
  [StructLayout(LayoutKind.Sequential)]struct BlurBehind{public uint Flags;[MarshalAs(UnmanagedType.Bool)]public bool Enable;public IntPtr Region;[MarshalAs(UnmanagedType.Bool)]public bool Transition;}
  [DllImport("dwmapi.dll")]static extern int DwmEnableBlurBehindWindow(IntPtr hwnd,ref BlurBehind blur);
  static bool LegacyMaterial(IntPtr hwnd,int state){var policy=new AccentPolicy{State=state,Flags=0,Color=0};var ptr=Marshal.AllocHGlobal(Marshal.SizeOf(policy));try{Marshal.StructureToPtr(policy,ptr,false);var data=new CompositionData{Attribute=19,Data=ptr,Size=Marshal.SizeOf(policy)};return SetWindowCompositionAttribute(hwnd,ref data);}catch{return false;}finally{Marshal.FreeHGlobal(ptr);}}
  public static List<Display> Displays(){
   var list=new List<Display>();EnumDisplayMonitors(IntPtr.Zero,IntPtr.Zero,delegate(IntPtr h,IntPtr dc,IntPtr r,IntPtr data){
    var info=new MonitorInfo();info.Size=Marshal.SizeOf(info);if(!GetMonitorInfo(h,ref info)||info.Monitor.Width<=0||info.Monitor.Height<=0||info.Work.Width<=0||info.Work.Height<=0)return true;
    var mode=new DevMode();mode.Size=(ushort)Marshal.SizeOf(mode);EnumDisplaySettings(info.Device,-1,ref mode);
    var dd=new DisplayDevice();dd.Size=Marshal.SizeOf(dd);EnumDisplayDevices(info.Device,0,ref dd,0);
    uint dx=96,dy=96;try{if(GetDpiForMonitor(h,0,out dx,out dy)!=0||dx<48||dx>960)dx=96;}catch{dx=96;}
    list.Add(new Display{Id=info.Device,Name="Ekran "+(list.Count+1)+" — "+(String.IsNullOrEmpty(dd.DeviceString)?info.Device:dd.DeviceString),HardwareId=dd.DeviceId??"",Bounds=info.Monitor,Work=info.Work,Hz=mode.Frequency>1?(int)mode.Frequency:60,Dpi=dx,Handle=h,Primary=(info.Flags&1)!=0});return true;
   },IntPtr.Zero);return list;
  }
  public static bool FullScreenOn(Display display,IntPtr own){
   var hwnd=GetForegroundWindow();if(hwnd==IntPtr.Zero||hwnd==own||IsIconic(hwnd))return false;
   var name=new StringBuilder(64);GetClassName(hwnd,name,64);if(name.ToString()=="Progman"||name.ToString()=="WorkerW"||name.ToString()=="Shell_TrayWnd")return false;
   Rect r;if(!GetWindowRect(hwnd,out r))return false;var b=display.Bounds;
   return Math.Abs(r.Left-b.Left)<=2&&Math.Abs(r.Top-b.Top)<=2&&Math.Abs(r.Right-b.Right)<=2&&Math.Abs(r.Bottom-b.Bottom)<=2;
  }
  public static bool Overlaps(Rect a,Rect b){return Math.Min(a.Right,b.Right)-Math.Max(a.Left,b.Left)>=8&&Math.Min(a.Bottom,b.Bottom)-Math.Max(a.Top,b.Top)>=8;}
  public static bool ForegroundBounds(IntPtr own,out Rect bounds){
   return ForeignWindowBounds(GetForegroundWindow(),own,out bounds);
  }
  public static uint WindowProcessId(IntPtr hwnd){uint pid;GetWindowThreadProcessId(hwnd,out pid);return pid;}
  internal static bool Intersects(Rect a,Rect b)=>a.Left<b.Right&&b.Left<a.Right&&a.Top<b.Bottom&&b.Top<a.Bottom;
  internal static IntPtr FindCoveringWindow(IntPtr own,Rect island,Func<Rect,bool> allowsHide)
  {
   IntPtr covering=IntPtr.Zero;
   EnumWindows((hwnd,_) =>
   {
    if(!IsWindowVisible(hwnd)||!GetWindowRect(hwnd,out var outer)||!Intersects(island,outer)||
       !ForeignWindowBounds(hwnd,own,out var frame)||!Intersects(island,frame))return true;
    if(allowsHide(frame))covering=hwnd;
    return false;
   },IntPtr.Zero);
   return covering;
  }
  public static bool ForeignWindowBounds(IntPtr hwnd,IntPtr own,out Rect bounds){
   bounds=new Rect();if(hwnd==IntPtr.Zero||hwnd==own||IsIconic(hwnd)||!IsWindowVisible(hwnd))return false;
   uint pid;GetWindowThreadProcessId(hwnd,out pid);if(pid==OwnProcessId)return false;
   if(DwmGetWindowAttribute(hwnd,14,out int cloaked,sizeof(int))==0&&cloaked!=0)return false;
   const long transparentLayered=0x80020;
   if((GetWindowLongPtr(hwnd,-20).ToInt64()&transparentLayered)==transparentLayered)return false;
   var name=new StringBuilder(64);GetClassName(hwnd,name,64);string cls=name.ToString();if(cls=="Progman"||cls=="WorkerW"||cls=="Shell_TrayWnd"||cls=="Shell_SecondaryTrayWnd")return false;
   if(DwmGetWindowAttribute(hwnd,9,out bounds,Marshal.SizeOf(typeof(Rect)))!=0&&!GetWindowRect(hwnd,out bounds))return false;
   return bounds.Width>0&&bounds.Height>0;
  }
 }

 [ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]internal class DeviceEnumerator {}
 [ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]internal interface IMMDeviceEnumerator {
  [PreserveSig]int EnumAudioEndpoints(int flow,uint mask,out IntPtr devices);
  [PreserveSig]int GetDefaultAudioEndpoint(int flow,int role,out IMMDevice device);
  [PreserveSig]int GetDevice([MarshalAs(UnmanagedType.LPWStr)]string id,out IMMDevice device);
  [PreserveSig]int RegisterEndpointNotificationCallback(IntPtr callback);
  [PreserveSig]int UnregisterEndpointNotificationCallback(IntPtr callback);
 }
 [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]internal interface IMMDevice {
  [PreserveSig]int Activate(ref Guid iid,uint context,IntPtr activation,[MarshalAs(UnmanagedType.IUnknown)]out object result);
  [PreserveSig]int OpenPropertyStore(int access,out IntPtr props);
  [PreserveSig]int GetId([MarshalAs(UnmanagedType.LPWStr)]out string id);
  [PreserveSig]int GetState(out uint state);
 }
 [ComImport,Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface IEndpointVolume {
  [PreserveSig]int RegisterControlChangeNotify(IntPtr callback);[PreserveSig]int UnregisterControlChangeNotify(IntPtr callback);[PreserveSig]int GetChannelCount(out uint channels);
  [PreserveSig]int SetMasterVolumeLevel(float db,ref Guid context);[PreserveSig]int SetMasterVolumeLevelScalar(float value,ref Guid context);[PreserveSig]int GetMasterVolumeLevel(out float db);[PreserveSig]int GetMasterVolumeLevelScalar(out float value);
  [PreserveSig]int SetChannelVolumeLevel(uint channel,float db,ref Guid context);[PreserveSig]int SetChannelVolumeLevelScalar(uint channel,float value,ref Guid context);[PreserveSig]int GetChannelVolumeLevel(uint channel,out float db);[PreserveSig]int GetChannelVolumeLevelScalar(uint channel,out float value);
  [PreserveSig]int SetMute([MarshalAs(UnmanagedType.Bool)]bool mute,ref Guid context);[PreserveSig]int GetMute([MarshalAs(UnmanagedType.Bool)]out bool mute);
 }
 public sealed partial class AudioService : IDisposable {
  IEndpointVolume endpoint;public double Volume;public bool Muted;public string Error="";
  public bool Refresh(){ReleaseEndpoint();IMMDeviceEnumerator enumerator=null;IMMDevice device=null;try{enumerator=(IMMDeviceEnumerator)new DeviceEnumerator();Marshal.ThrowExceptionForHR(enumerator.GetDefaultAudioEndpoint(0,1,out device));var id=typeof(IEndpointVolume).GUID;object o;Marshal.ThrowExceptionForHR(device.Activate(ref id,23,IntPtr.Zero,out o));endpoint=(IEndpointVolume)o;float value;bool mute;Marshal.ThrowExceptionForHR(endpoint.GetMasterVolumeLevelScalar(out value));Marshal.ThrowExceptionForHR(endpoint.GetMute(out mute));Volume=value*100;Muted=mute;Error="";AttachVolumeNotice();return true;}catch(Exception ex){Error="Ses aygıtı kullanılamıyor: "+ex.Message;return false;}finally{if(device!=null)Marshal.ReleaseComObject(device);if(enumerator!=null)Marshal.ReleaseComObject(enumerator);}}
  public bool Set(double value){try{if(endpoint==null&&!Refresh())return false;var context=Guid.Empty;Marshal.ThrowExceptionForHR(endpoint.SetMasterVolumeLevelScalar((float)Math.Max(0,Math.Min(1,value/100)),ref context));Volume=value;Error="";return true;}catch(Exception ex){Error=ex.Message;return false;}}
  public bool ToggleMute(){try{if(endpoint==null&&!Refresh())return false;var context=Guid.Empty;bool muted;Marshal.ThrowExceptionForHR(endpoint.GetMute(out muted));Marshal.ThrowExceptionForHR(endpoint.SetMute(!muted,ref context));Muted=!muted;return true;}catch(Exception ex){Error=ex.Message;return false;}}
  public void Dispose(){StopNotifications();}
 }
}



