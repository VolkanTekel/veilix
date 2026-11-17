// GPL-3.0-or-later. High-resolution frame timing, alive only during native motion.
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Ada {
 public sealed class FramePump : IDisposable {
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr CreateWaitableTimerExW(IntPtr attributes,string name,uint flags,uint access);
  [DllImport("kernel32.dll")]static extern bool SetWaitableTimer(IntPtr timer,ref long due,int period,IntPtr completion,IntPtr argument,bool resume);
  [DllImport("kernel32.dll")]static extern uint WaitForMultipleObjects(uint count,IntPtr[] handles,bool all,uint milliseconds);
  [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr handle);
  readonly CancellationTokenSource stop=new CancellationTokenSource();public static int ActiveCount;
  FramePump(IntPtr timer,Dispatcher dispatcher,int fps,Action frame){
   var token=stop.Token;Task.Factory.StartNew(delegate{Interlocked.Increment(ref ActiveCount);try{
    var handles=new[]{timer,token.WaitHandle.SafeWaitHandle.DangerousGetHandle()};double interval=1000.0/fps,next=0;var watch=Stopwatch.StartNew();
    while(!token.IsCancellationRequested){
     next+=interval;if(next<watch.Elapsed.TotalMilliseconds)next=watch.Elapsed.TotalMilliseconds+interval;
     long due=-(long)(Math.Max(.2,next-watch.Elapsed.TotalMilliseconds)*10000);
     if(!SetWaitableTimer(timer,ref due,0,IntPtr.Zero,IntPtr.Zero,false)||WaitForMultipleObjects(2,handles,false,1000)!=0)break;
     dispatcher.Invoke(DispatcherPriority.Render,new Action(delegate{if(!token.IsCancellationRequested)frame();}));
    }
   }catch(OperationCanceledException){}catch(InvalidOperationException){}finally{CloseHandle(timer);stop.Dispose();Interlocked.Decrement(ref ActiveCount);}},CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default);
  }
  public static FramePump TryStart(Dispatcher dispatcher,int fps,Action frame){try{var timer=CreateWaitableTimerExW(IntPtr.Zero,null,2,0x1F0003);return timer==IntPtr.Zero?null:new FramePump(timer,dispatcher,fps,frame);}catch(EntryPointNotFoundException){return null;}}
  public void Dispose(){try{stop.Cancel();}catch(ObjectDisposedException){}}
 }
}
