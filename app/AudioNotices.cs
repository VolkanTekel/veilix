// GPL-3.0-or-later. Native audio callbacks; never blocks inside a callback.
using System;
using System.Runtime.InteropServices;
namespace Ada {
 [ComVisible(true),Guid("657804FA-D6AD-4496-8A60-352752AF4F89"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]public interface IVolumeNoticeSink {[PreserveSig]int OnNotify(IntPtr data);}
 [StructLayout(LayoutKind.Sequential)]public struct AudioPropertyKey {public Guid Id;public uint Key;}
 [ComVisible(true),Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]public interface IDeviceNoticeSink {
  [PreserveSig]int OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)]string id,uint state);[PreserveSig]int OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)]string id);[PreserveSig]int OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)]string id);[PreserveSig]int OnDefaultDeviceChanged(int flow,int role,[MarshalAs(UnmanagedType.LPWStr)]string id);[PreserveSig]int OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)]string id,AudioPropertyKey key);
 }
 [ComVisible(true),ClassInterface(ClassInterfaceType.None)]public sealed class VolumeNoticeSink:IVolumeNoticeSink {
  readonly Action<double,bool> action;[StructLayout(LayoutKind.Sequential)]struct Notice {public Guid Context;public int Muted;public float Volume;public uint Channels;}
  public VolumeNoticeSink(Action<double,bool> action){this.action=action;}
  public int OnNotify(IntPtr data){try{var notice=(Notice)Marshal.PtrToStructure(data,typeof(Notice));action(notice.Volume*100,notice.Muted!=0);}catch{}return 0;}
 }
 [ComVisible(true),ClassInterface(ClassInterfaceType.None)]public sealed class DeviceNoticeSink:IDeviceNoticeSink {
  readonly Action action;public DeviceNoticeSink(Action action){this.action=action;}public int OnDeviceStateChanged(string id,uint state){return 0;}public int OnDeviceAdded(string id){return 0;}public int OnDeviceRemoved(string id){return 0;}public int OnPropertyValueChanged(string id,AudioPropertyKey key){return 0;}public int OnDefaultDeviceChanged(int flow,int role,string id){if(flow==0&&role==1)try{action();}catch{}return 0;}
 }
 public sealed partial class AudioService {
  bool listening;VolumeNoticeSink volumeSink;DeviceNoticeSink deviceSink;IntPtr volumeSinkPointer,deviceSinkPointer;IMMDeviceEnumerator noticeEnumerator;
  public event Action<double,bool> Changed;public event Action DeviceChanged;
  public bool NoticesRegistered{get{return volumeSinkPointer!=IntPtr.Zero;}}
  public void StartNotifications(){listening=true;if(endpoint==null)Refresh();AttachVolumeNotice();if(noticeEnumerator!=null)return;try{noticeEnumerator=(IMMDeviceEnumerator)new DeviceEnumerator();deviceSink=new DeviceNoticeSink(delegate{var handler=DeviceChanged;if(handler!=null)handler();});deviceSinkPointer=Marshal.GetComInterfaceForObject(deviceSink,typeof(IDeviceNoticeSink));Marshal.ThrowExceptionForHR(noticeEnumerator.RegisterEndpointNotificationCallback(deviceSinkPointer));}catch{StopDeviceNotice();}}
  void AttachVolumeNotice(){if(!listening||endpoint==null||volumeSinkPointer!=IntPtr.Zero)return;try{volumeSink=new VolumeNoticeSink(delegate(double value,bool muted){Volume=value;Muted=muted;var handler=Changed;if(handler!=null)handler(value,muted);});volumeSinkPointer=Marshal.GetComInterfaceForObject(volumeSink,typeof(IVolumeNoticeSink));Marshal.ThrowExceptionForHR(endpoint.RegisterControlChangeNotify(volumeSinkPointer));}catch{DetachVolumeNotice();}}
  void DetachVolumeNotice(){if(volumeSinkPointer!=IntPtr.Zero){try{if(endpoint!=null)endpoint.UnregisterControlChangeNotify(volumeSinkPointer);}catch{}Marshal.Release(volumeSinkPointer);volumeSinkPointer=IntPtr.Zero;volumeSink=null;}}
  void StopDeviceNotice(){if(deviceSinkPointer!=IntPtr.Zero){try{if(noticeEnumerator!=null)noticeEnumerator.UnregisterEndpointNotificationCallback(deviceSinkPointer);}catch{}Marshal.Release(deviceSinkPointer);deviceSinkPointer=IntPtr.Zero;deviceSink=null;}if(noticeEnumerator!=null){Marshal.ReleaseComObject(noticeEnumerator);noticeEnumerator=null;}}
  void ReleaseEndpoint(){ReleaseMeter();DetachVolumeNotice();if(endpoint!=null){Marshal.ReleaseComObject(endpoint);endpoint=null;}}
  void StopNotifications(){listening=false;StopDeviceNotice();ReleaseEndpoint();}
 }
}
