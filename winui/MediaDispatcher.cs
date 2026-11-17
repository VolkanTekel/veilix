// Adapt the tested media recovery engine to WinUI's queue, with no WPF UI thread.
using System;
using Microsoft.UI.Dispatching;
namespace Veilix;
internal enum MediaPriority {Background}
internal sealed class MediaDispatcher {
 internal readonly DispatcherQueue Queue=DispatcherQueue.GetForCurrentThread();
 internal static MediaDispatcher CurrentDispatcher=>new MediaDispatcher();
 internal bool HasShutdownStarted=>Microsoft.UI.Xaml.Application.Current==null;
 internal void BeginInvoke(MediaPriority priority,Action action){if(!HasShutdownStarted)UiFaultBoundary.Enqueue(Queue,action,"media dispatch",DispatcherQueuePriority.Low);}
}
internal sealed class MediaTimer {
 readonly DispatcherQueueTimer timer;
 internal MediaTimer(MediaPriority priority,MediaDispatcher dispatcher){timer=dispatcher.Queue.CreateTimer();timer.Tick+=(_,e)=>UiFaultBoundary.Run(()=>Tick?.Invoke(this,EventArgs.Empty),"media watchdog");}
 internal TimeSpan Interval {get=>timer.Interval;set=>timer.Interval=value;}
 internal event EventHandler Tick;
 internal void Start()=>timer.Start();
 internal void Stop()=>timer.Stop();
}
