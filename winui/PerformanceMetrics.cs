using System;
using System.Runtime.InteropServices;
namespace Veilix;
// Read only this process. Process.Refresh gathers additional process/thread
// information that is unnecessary inside a resource-use measurement loop.
internal static class PerformanceMetrics {
 [StructLayout(LayoutKind.Sequential)]struct MemoryCounters {
  public uint Size,PageFaults;
  public ulong PeakWorkingSet,WorkingSet,PeakPagedPool,PagedPool,PeakNonPagedPool,NonPagedPool,PageFile,PeakPageFile,Private;
 }
 [DllImport("kernel32.dll")]static extern bool GetProcessTimes(IntPtr process,out long created,out long exited,out long kernel,out long user);
 [DllImport("psapi.dll")]static extern bool GetProcessMemoryInfo(IntPtr process,ref MemoryCounters counters,uint size);
 internal static TimeSpan Cpu(){if(!GetProcessTimes(new IntPtr(-1),out _,out _,out long kernel,out long user))throw new InvalidOperationException("Process CPU counter unavailable");return TimeSpan.FromTicks(kernel+user);}
 internal static (long WorkingSet,long Private) Memory(){var counters=new MemoryCounters{Size=(uint)Marshal.SizeOf<MemoryCounters>()};if(!GetProcessMemoryInfo(new IntPtr(-1),ref counters,counters.Size))throw new InvalidOperationException("Process memory counter unavailable");return ((long)counters.WorkingSet,(long)counters.Private);}
}
