using System;
using System.Runtime.CompilerServices;
namespace Veilix;
public sealed partial class MainWindow {
 void EnqueueUi(Action action,[CallerArgumentExpression(nameof(action))]string source=null)=>UiFaultBoundary.Enqueue(DispatcherQueue,()=>{if(!disposed)action();},source);
 void InvalidateUiAfterFault(){if(disposed)return;clockShown=null;headerMarquee?.Invalidate();barVisuals=null;barDpi=0;}
 static double PanelHeight(double desired,double available){double max=Math.Max(54,double.IsFinite(available)?available:300);double min=Math.Min(300,max);return Math.Clamp(double.IsFinite(desired)?desired:min,min,max);}
}
