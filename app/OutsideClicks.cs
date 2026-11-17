// GPL-3.0-or-later. A menu-scoped observer; never consumes or records input.
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;

namespace Ada {
 sealed class OutsideClickWatcher : IDisposable {
  [StructLayout(LayoutKind.Sequential)]struct Point {public int X,Y;}
  [StructLayout(LayoutKind.Sequential)]struct Message {public IntPtr Hwnd;public uint Id;public UIntPtr W;public IntPtr L;public uint Time;public Point Pt;public uint Private;}
  delegate IntPtr Hook(int code,IntPtr message,IntPtr data);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SetWindowsHookEx(int id,Hook callback,IntPtr module,uint thread);
  [DllImport("user32.dll")]static extern bool UnhookWindowsHookEx(IntPtr hook);
  [DllImport("user32.dll")]static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
  [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(Point point);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetMessage(out Message message,IntPtr window,uint min,uint max);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern bool PeekMessage(out Message message,IntPtr window,uint min,uint max,uint remove);
  [DllImport("user32.dll")]static extern bool PostThreadMessage(uint thread,uint message,UIntPtr w,IntPtr l);
  [DllImport("kernel32.dll")]static extern uint GetCurrentThreadId();
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr GetModuleHandle(string module);
  readonly object gate=new object();readonly Action<uint> clicked;uint threadId;bool disposed;
  public bool Installed{get;private set;}
  public OutsideClickWatcher(Action<uint> clicked){this.clicked=clicked;var worker=new Thread(Run){IsBackground=true,Name="Veilix menu clicks"};worker.Start();}
  void Run(){
   Message message;PeekMessage(out message,IntPtr.Zero,0,0,0);IntPtr handle=IntPtr.Zero;
   // This callback lives on its own message thread, so a busy WPF layout cannot
   // stall system mouse input. Only button-down events are forwarded to WPF.
   Hook callback=delegate(int code,IntPtr w,IntPtr data){
    int id=unchecked((int)w.ToInt64());
    if(code>=0&&(id==0x201||id==0x204||id==0x207||id==0x20B)){
     var point=(Point)Marshal.PtrToStructure(data,typeof(Point));uint pid=Native.WindowProcessId(WindowFromPoint(point));clicked(pid);
    }
    return CallNextHookEx(handle,code,w,data);
   };
   try{lock(gate){if(disposed)return;threadId=GetCurrentThreadId();handle=SetWindowsHookEx(14,callback,GetModuleHandle(null),0);Installed=handle!=IntPtr.Zero;}if(handle==IntPtr.Zero)return;while(GetMessage(out message,IntPtr.Zero,0,0)>0){} }
   finally{if(handle!=IntPtr.Zero)UnhookWindowsHookEx(handle);lock(gate){threadId=0;Installed=false;}GC.KeepAlive(callback);}
  }
  public void Dispose(){lock(gate){disposed=true;if(threadId!=0)PostThreadMessage(threadId,0x12,UIntPtr.Zero,IntPtr.Zero);}}
 }
 public sealed partial class AdaWindow {
  OutsideClickWatcher outsideClicks;int outsideGeneration;
  static readonly uint WidgetProcessId=(uint)System.Diagnostics.Process.GetCurrentProcess().Id;
  void UpdateOutsideClicks(){
   if(route==""||testing){StopOutsideClicks();return;}if(outsideClicks!=null)return;int generation=++outsideGeneration;
   outsideClicks=new OutsideClickWatcher(delegate(uint pid){if(pid==WidgetProcessId||Dispatcher.HasShutdownStarted)return;try{Dispatcher.BeginInvoke(DispatcherPriority.Input,new Action(delegate{if(generation==outsideGeneration)HandleOutsideClick(pid);}));}catch(InvalidOperationException){} });
  }
  void HandleOutsideClick(uint pid){if(pid==WidgetProcessId||route==""||closing||replacingWindow||themeEditorOpen)return;SetHeaderHovered(false);Navigate("");}
  void StopOutsideClicks(){outsideGeneration++;var watcher=outsideClicks;outsideClicks=null;if(watcher!=null)watcher.Dispose();}
 }
}
