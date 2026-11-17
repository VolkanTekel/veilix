// GPL-3.0-or-later. A stalled COM call must not lock the recovery loop forever.
using System;
using System.Threading;
using System.Threading.Tasks;
namespace Ada {
 internal sealed class MediaTransport {
  readonly SemaphoreSlim slots=new SemaphoreSlim(2,2);readonly TimeSpan timeout;
  public MediaTransport(TimeSpan timeout){this.timeout=timeout;}
  public Task<T> Read<T>(Func<T> action){return Run(()=>Task.FromResult(action()));}
  public async Task<T> Run<T>(Func<Task<T>> action,Action<T> lateResult=null){
   // Quarantine at most two unfinished native operations. The dispatcher can
   // recover on a fresh connection without spawning unbounded blocked workers.
   if(!await slots.WaitAsync(timeout))throw new TimeoutException("Windows medya çağrıları hâlâ yanıt vermiyor.");
   Task<T> operation;
   try{operation=Task.Run(action);}catch{slots.Release();throw;}
   ObserveCompletion(operation);
   if(await Task.WhenAny(operation,Task.Delay(timeout))!=operation){
    if(lateResult!=null)CleanUpLateResult(operation,lateResult);
    throw new TimeoutException("Windows medya çağrısı zaman aşımına uğradı.");
   }
   return await operation;
  }
  void ObserveCompletion<T>(Task<T> operation){operation.ContinueWith(t=>{if(t.IsFaulted){var ignored=t.Exception;}slots.Release();},CancellationToken.None,TaskContinuationOptions.ExecuteSynchronously,TaskScheduler.Default);}
  static void CleanUpLateResult<T>(Task<T> operation,Action<T> action){operation.ContinueWith(t=>{if(t.Status==TaskStatus.RanToCompletion)try{action(t.Result);}catch{}},TaskScheduler.Default);}
 }
}
