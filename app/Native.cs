using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Ada {
 public sealed class Display {
  public string Id,Name,HardwareId; public Native.Rect Bounds;public int Hz=60;public double Dpi=96; public IntPtr Handle;public bool Primary;
  public override string ToString(){return Name+" · "+Bounds.Width+" × "+Bounds.Height+" · "+Hz+" Hz"+(Primary?" · Ana ekran":"");}
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
  [DllImport("user32.dll")]public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")]public static extern bool GetWindowRect(IntPtr handle,out Rect rect);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr hwnd,StringBuilder name,int max);
  [DllImport("user32.dll")]static extern bool IsIconic(IntPtr handle);
  [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr handle);
  [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr handle,out uint processId);
  [DllImport("dwmapi.dll")]static extern int DwmGetWindowAttribute(IntPtr handle,int attribute,out Rect value,int size);
  [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int cx,int cy,uint flags);
  [StructLayout(LayoutKind.Sequential)]struct AccentPolicy{public int State,Flags;public uint Color;public int Animation;}
  [StructLayout(LayoutKind.Sequential)]struct CompositionData{public int Attribute;public IntPtr Data;public int Size;}
  [DllImport("user32.dll")]static extern bool SetWindowCompositionAttribute(IntPtr hwnd,ref CompositionData data);
  [StructLayout(LayoutKind.Sequential)]struct BlurBehind{public uint Flags;[MarshalAs(UnmanagedType.Bool)]public bool Enable;public IntPtr Region;[MarshalAs(UnmanagedType.Bool)]public bool Transition;}
  [DllImport("dwmapi.dll")]static extern int DwmEnableBlurBehindWindow(IntPtr hwnd,ref BlurBehind blur);
  static bool LegacyMaterial(IntPtr hwnd,int state){var policy=new AccentPolicy{State=state,Flags=0,Color=0};var ptr=Marshal.AllocHGlobal(Marshal.SizeOf(policy));try{Marshal.StructureToPtr(policy,ptr,false);var data=new CompositionData{Attribute=19,Data=ptr,Size=Marshal.SizeOf(policy)};return SetWindowCompositionAttribute(hwnd,ref data);}catch{return false;}finally{Marshal.FreeHGlobal(ptr);}}
  public static void Position(Window w,Display display,double offsetY=0){var hwnd=new WindowInteropHelper(w).Handle;if(hwnd==IntPtr.Zero)return;var source=HwndSource.FromHwnd(hwnd);if(source==null||source.CompositionTarget==null)return;double dpi=source.CompositionTarget.TransformToDevice.M11;int width=(int)Math.Round(w.Width*dpi);int x=display.Bounds.Left+(display.Bounds.Width-width)/2;SetWindowPos(hwnd,IntPtr.Zero,x,display.Bounds.Top+(int)Math.Round(offsetY*dpi),0,0,0x0015);}
  public static bool IsLayered(Window window){var hwnd=new WindowInteropHelper(window).Handle;return hwnd!=IntPtr.Zero&&(GetWindowLongPtr(hwnd,-20).ToInt64()&0x00080000L)!=0;}
  public static void RemoveFrame(Window window){if(window.AllowsTransparency)return;var hwnd=new WindowInteropHelper(window).Handle;if(hwnd==IntPtr.Zero)return;long style=GetWindowLongPtr(hwnd,-16).ToInt64();style&=~(0x00C00000L|0x00040000L|0x00800000L);SetWindowLongPtr(hwnd,-16,new IntPtr(style));int noClient=1;DwmSetWindowAttribute(hwnd,2,ref noClient,4);int noBorder=unchecked((int)0xFFFFFFFE);DwmSetWindowAttribute(hwnd,34,ref noBorder,4);int corners=1;DwmSetWindowAttribute(hwnd,33,ref corners,4);SetWindowPos(hwnd,IntPtr.Zero,0,0,0,0,0x0037);}
  public static void Crop(Window window,System.Windows.Rect rect,double radius){if(window.AllowsTransparency)return;var hwnd=new WindowInteropHelper(window).Handle;if(hwnd==IntPtr.Zero||rect.Width<1||rect.Height<1)return;var source=PresentationSource.FromVisual(window);if(source==null)return;double dpi=source.CompositionTarget.TransformToDevice.M11;int x=(int)Math.Round(rect.X*dpi),y=(int)Math.Round(rect.Y*dpi),width=(int)Math.Round(rect.Width*dpi),height=(int)Math.Round(rect.Height*dpi),r=(int)Math.Round(radius*dpi);var rounded=CreateRoundRectRgn(x,y,x+width,y+height,r*2,r*2);var top=CreateRectRgn(x,y,x+width,y+Math.Min(r,height));CombineRgn(rounded,rounded,top,2);DeleteObject(top);if(SetWindowRgn(hwnd,rounded,true)==0)DeleteObject(rounded);}
  [StructLayout(LayoutKind.Sequential)]struct RegionPoint{public int X,Y;}
  [DllImport("gdi32.dll")]static extern IntPtr CreatePolygonRgn(RegionPoint[] points,int count,int mode);
  public static void CropNotch(Window window,System.Windows.Rect rect,double radius,double shoulder){if(window.AllowsTransparency)return;var hwnd=new WindowInteropHelper(window).Handle;var source=PresentationSource.FromVisual(window);if(hwnd==IntPtr.Zero||source==null||rect.Width<1||rect.Height<1)return;double dpi=source.CompositionTarget.TransformToDevice.M11;CropBackdrop(hwnd,rect,radius,shoulder,dpi);}
  internal static void CropBackdrop(IntPtr hwnd,System.Windows.Rect rect,double radius,double shoulder,double dpi){var outline=NotchSurface.Outline(rect.Width*dpi,rect.Height*dpi,shoulder*dpi,radius*dpi).GetFlattenedPathGeometry(.2,ToleranceType.Absolute);var points=new List<RegionPoint>();foreach(var figure in outline.Figures){points.Add(new RegionPoint{X=(int)Math.Round((rect.X*dpi)+figure.StartPoint.X),Y=(int)Math.Round(rect.Y*dpi+figure.StartPoint.Y)});foreach(var segment in figure.Segments){var poly=segment as PolyLineSegment;if(poly!=null)foreach(var point in poly.Points)points.Add(new RegionPoint{X=(int)Math.Round(rect.X*dpi+point.X),Y=(int)Math.Round(rect.Y*dpi+point.Y)});var line=segment as LineSegment;if(line!=null)points.Add(new RegionPoint{X=(int)Math.Round(rect.X*dpi+line.Point.X),Y=(int)Math.Round(rect.Y*dpi+line.Point.Y)});}}if(points.Count<3)return;var region=CreatePolygonRgn(points.ToArray(),points.Count,2);if(region!=IntPtr.Zero){if(SetWindowRgn(hwnd,region,true)==0)DeleteObject(region);}}
  [DllImport("user32.dll")]static extern int GetWindowRgn(IntPtr handle,IntPtr region);
  [DllImport("gdi32.dll")]static extern bool PtInRegion(IntPtr region,int x,int y);
  public static bool RegionContains(Window window,double x,double y){var region=CreateRectRgn(0,0,0,0);try{if(GetWindowRgn(new WindowInteropHelper(window).Handle,region)<=0)throw new InvalidOperationException("Window region unavailable.");double dpi=PresentationSource.FromVisual(window).CompositionTarget.TransformToDevice.M11;return PtInRegion(region,(int)Math.Round(x*dpi),(int)Math.Round(y*dpi));}finally{DeleteObject(region);}}
  public static List<Display> Displays(){
   var list=new List<Display>();EnumDisplayMonitors(IntPtr.Zero,IntPtr.Zero,delegate(IntPtr h,IntPtr dc,IntPtr r,IntPtr data){
    var info=new MonitorInfo();info.Size=Marshal.SizeOf(info);if(!GetMonitorInfo(h,ref info))return true;
    var mode=new DevMode();mode.Size=(ushort)Marshal.SizeOf(mode);EnumDisplaySettings(info.Device,-1,ref mode);
    var dd=new DisplayDevice();dd.Size=Marshal.SizeOf(dd);EnumDisplayDevices(info.Device,0,ref dd,0);
    uint dx=96,dy=96;try{GetDpiForMonitor(h,0,out dx,out dy);}catch{}
    list.Add(new Display{Id=info.Device,Name="Ekran "+(list.Count+1)+" — "+(String.IsNullOrEmpty(dd.DeviceString)?info.Device:dd.DeviceString),HardwareId=dd.DeviceId??"",Bounds=info.Monitor,Hz=mode.Frequency>1?(int)mode.Frequency:60,Dpi=dx,Handle=h,Primary=(info.Flags&1)!=0});return true;
   },IntPtr.Zero);return list;
  }
  internal static bool ConfigureBackdropHost(IntPtr hwnd){int noFrame=1,noBorder=unchecked((int)0xFFFFFFFE),none=1;DwmSetWindowAttribute(hwnd,2,ref noFrame,4);DwmSetWindowAttribute(hwnd,34,ref noBorder,4);DwmSetWindowAttribute(hwnd,38,ref none,4);int useHost=1;bool modern=DwmSetWindowAttribute(hwnd,17,ref useHost,4)==0;return LegacyMaterial(hwnd,5)||modern;}
  public static bool RegionContainsHandle(IntPtr hwnd,double x,double y,double dpi){var region=CreateRectRgn(0,0,0,0);try{if(GetWindowRgn(hwnd,region)<=0)throw new InvalidOperationException("Backdrop region unavailable.");return PtInRegion(region,(int)Math.Round(x*dpi),(int)Math.Round(y*dpi));}finally{DeleteObject(region);}}
  public static bool Backdrop(Window w,string material,bool light){
   var hwnd=new WindowInteropHelper(w).Handle;if(hwnd==IntPtr.Zero)return false;
   var island=w as AdaWindow;
   if(material=="Cam")return island!=null&&island.EnableClippedBackdrop();
   if(island!=null)island.EnableClippedBackdrop(false);
   if(w.AllowsTransparency)return true;
   int dark=light?0:1;DwmSetWindowAttribute(hwnd,20,ref dark,4);
   var source=HwndSource.FromHwnd(hwnd);if(source!=null)source.CompositionTarget.BackgroundColor=Colors.Transparent;
   var margin=new Margins{Left=0,Top=0,Right=0,Bottom=0};DwmExtendFrameIntoClientArea(hwnd,ref margin);
   var disabledBlur=new BlurBehind{Flags=1,Enable=false};DwmEnableBlurBehindWindow(hwnd,ref disabledBlur);LegacyMaterial(hwnd,0);int clearBackdrop=1;DwmSetWindowAttribute(hwnd,38,ref clearBackdrop,4);int type=material=="Mika"?2:1;
   bool modern=DwmSetWindowAttribute(hwnd,38,ref type,4)==0;
   if(material=="Düz")return true;
   if(material=="Şeffaf")return false;
   if(modern)return true;
   // Windows 10 has no DWMWA_SYSTEMBACKDROP_TYPE. Its native blur is opt-in,
   // isolated here, and falls back to an opaque surface if unsupported.
   return false;
  }
  public static void Round(Window w,double radius){
   var hwnd=new WindowInteropHelper(w).Handle;if(hwnd==IntPtr.Zero)return;
   var transform=PresentationSource.FromVisual(w).CompositionTarget.TransformToDevice;
   int sx=(int)Math.Ceiling(w.ActualWidth*transform.M11),sy=(int)Math.Ceiling(w.ActualHeight*transform.M22);if(sx<=0||sy<=0)return;
   var region=CreateRoundRectRgn(0,0,sx+1,sy+1,(int)(radius*2*transform.M11),(int)(radius*2*transform.M22));if(SetWindowRgn(hwnd,region,true)==0)DeleteObject(region);
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
  public static bool ForeignWindowBounds(IntPtr hwnd,IntPtr own,out Rect bounds){
   bounds=new Rect();if(hwnd==IntPtr.Zero||hwnd==own||IsIconic(hwnd)||!IsWindowVisible(hwnd))return false;
   uint pid;GetWindowThreadProcessId(hwnd,out pid);if(pid==OwnProcessId)return false;
   var name=new StringBuilder(64);GetClassName(hwnd,name,64);string cls=name.ToString();if(cls=="Progman"||cls=="WorkerW"||cls=="Shell_TrayWnd"||cls=="Shell_SecondaryTrayWnd")return false;
   if(DwmGetWindowAttribute(hwnd,9,out bounds,Marshal.SizeOf(typeof(Rect)))!=0&&!GetWindowRect(hwnd,out bounds))return false;
   return bounds.Width>0&&bounds.Height>0;
  }
 }

 [ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]class DeviceEnumerator {}
 [ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface IMMDeviceEnumerator {
  [PreserveSig]int EnumAudioEndpoints(int flow,uint mask,out IntPtr devices);
  [PreserveSig]int GetDefaultAudioEndpoint(int flow,int role,out IMMDevice device);
  [PreserveSig]int GetDevice([MarshalAs(UnmanagedType.LPWStr)]string id,out IMMDevice device);
  [PreserveSig]int RegisterEndpointNotificationCallback(IntPtr callback);
  [PreserveSig]int UnregisterEndpointNotificationCallback(IntPtr callback);
 }
 [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface IMMDevice {
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

