using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.UI.Dispatching;
namespace Veilix;
internal static class UiFaultBoundary {
 static readonly object gate=new();static readonly Dictionary<string,long> recent=new();
 internal static Func<string> Snapshot;internal static Action Recovered;
 internal static long RecoveredCount;
 static bool IsTest=>Array.IndexOf(Environment.GetCommandLineArgs(),"--self-test")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"--stability-test")>=0;
 internal static string LogPath=>Path.Combine(AppContext.BaseDirectory,IsTest?"qa":"data",IsTest?"isolated-ui-faults.log":"ui-faults.log");
 internal static bool Recoverable(Exception error)=>error.HResult==unchecked((int)0x80070057)||error.HResult==unchecked((int)0x80000013)||error is ObjectDisposedException;
 internal static void Report(Exception error,string source,bool recovered){
  // Diagnostics must never replace the exception being diagnosed or grow
  // without bound. No screenshots, media payloads or network uploads.
  try{lock(gate){string key=source+":"+error.HResult;long now=Environment.TickCount64;if(recent.TryGetValue(key,out long previous)&&now-previous<10000)return;if(recent.Count>=64)recent.Clear();recent[key]=now;
   string path=LogPath;Directory.CreateDirectory(Path.GetDirectoryName(path));if(File.Exists(path)&&new FileInfo(path).Length>512*1024)File.Move(path,path+".previous",true);
   string state="";try{state=Snapshot?.Invoke()??"";}catch{}
   File.AppendAllText(path,$"{DateTimeOffset.Now:O} PID={Environment.ProcessId} thread={Environment.CurrentManagedThreadId} recovered={recovered} source={source} HRESULT=0x{error.HResult:X8}\n{state}\n{error}\n\n");
  }}catch{}
 }
 internal static bool Run(Action action,string source){try{action();return true;}catch(Exception error)when(Recoverable(error)){Interlocked.Increment(ref RecoveredCount);Report(error,source,true);Recovered?.Invoke();return false;}catch(Exception error){Report(error,source,false);throw;}}
 internal static bool Enqueue(DispatcherQueue queue,Action action,[CallerArgumentExpression(nameof(action))]string source=null,DispatcherQueuePriority priority=DispatcherQueuePriority.Normal)=>queue.TryEnqueue(priority,()=>Run(action,"queue: "+source));
}
internal sealed class GuardedUiContext:SynchronizationContext {
 readonly SynchronizationContext inner;
 internal GuardedUiContext(SynchronizationContext inner){this.inner=inner;}
 public override SynchronizationContext CreateCopy()=>this;
 public override void OperationStarted()=>inner.OperationStarted();
 public override void OperationCompleted()=>inner.OperationCompleted();
 public override void Post(SendOrPostCallback callback,object state)=>inner.Post(_=>{
  var previous=Current;SetSynchronizationContext(this);try{UiFaultBoundary.Run(()=>callback(state),"async: "+callback.Method.DeclaringType?.FullName+"."+callback.Method.Name);}finally{SetSynchronizationContext(previous);}
 },null);
 public override void Send(SendOrPostCallback callback,object state)=>inner.Send(_=>UiFaultBoundary.Run(()=>callback(state),"sync: "+callback.Method.Name),null);
}
