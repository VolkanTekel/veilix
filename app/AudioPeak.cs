// GPL-3.0-or-later. Read-only endpoint peak meter. No audio capture or sample storage.
using System;using System.Runtime.InteropServices;
namespace Ada {
 [ComImport,Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface IAudioPeak { [PreserveSig]int GetPeakValue(out float value);[PreserveSig]int GetMeteringChannelCount(out uint count);[PreserveSig]int GetChannelsPeakValues(uint count,IntPtr values);[PreserveSig]int QueryHardwareSupport(out uint mask); }
 public sealed partial class AudioService {
  IAudioPeak meter;bool meterTried;
  public double Peak(){try{if(Muted)return 0;if(!meterTried){meterTried=true;IMMDeviceEnumerator e=null;IMMDevice d=null;try{e=(IMMDeviceEnumerator)new DeviceEnumerator();Marshal.ThrowExceptionForHR(e.GetDefaultAudioEndpoint(0,1,out d));var id=typeof(IAudioPeak).GUID;object value;Marshal.ThrowExceptionForHR(d.Activate(ref id,23,IntPtr.Zero,out value));meter=(IAudioPeak)value;}finally{if(d!=null)Marshal.ReleaseComObject(d);if(e!=null)Marshal.ReleaseComObject(e);}}float peak;return meter!=null&&meter.GetPeakValue(out peak)>=0?Math.Max(0,Math.Min(1,peak)):0;}catch{return 0;}}
  void ReleaseMeter(){if(meter!=null){Marshal.ReleaseComObject(meter);meter=null;}meterTried=false;}
 }
}
