using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Ada;
namespace Veilix;
public sealed partial class MainWindow {
 async Task CheckCallbackRecovery(List<string> report){
  Check(UiFaultBoundary.RecoveredCount==0,"Unexpected startup callback recovery");Check(SynchronizationContext.Current is GuardedUiContext,"UI async continuations do not use the guarded context");
  string marker="qa-fault-"+Guid.NewGuid().ToString("N");long before=UiFaultBoundary.RecoveredCount;int ticks=0;
  var timer=Timer(15,()=>{ticks++;if(ticks==1)throw new ArgumentException("Injected callback argument failure.");},marker);timer.Start();await Task.Delay(400);timer.Stop();Check(ticks>=2,"Native timer did not survive the failed callback");
  bool nextQueued=false;UiFaultBoundary.Enqueue(DispatcherQueue,()=>throw new ArgumentException("Injected queued failure."),marker+"-queue");UiFaultBoundary.Enqueue(DispatcherQueue,()=>nextQueued=true,marker+"-next");
  async void ThrowLater(){await Task.Delay(15);throw new ArgumentException("Injected async continuation failure.");}ThrowLater();await Task.Delay(100);
  Check(nextQueued,"Native dispatch queue stopped after a failed callback");Check(!UiFaultBoundary.Run(()=>throw new COMException("Injected closed UI object.",unchecked((int)0x80000013)),marker+"-closed"),"Closed-object recovery");
  bool fatalPropagated=false;try{UiFaultBoundary.Run(()=>throw new InvalidOperationException("Injected nonrecoverable failure."),marker+"-fatal");}catch(InvalidOperationException){fatalPropagated=true;}
  Check(fatalPropagated&&UiFaultBoundary.RecoveredCount-before==4,"Unexpected exceptions were swallowed or expected failures escaped");Check(File.ReadAllText(UiFaultBoundary.LogPath).Contains(marker),"UI failure context was not written");
  foreach(var pair in new[]{(500.0,250.0),(double.NaN,250.0),(double.PositiveInfinity,250.0),(800.0,800.0),(54.0,54.0)}){double height=PanelHeight(pair.Item1,pair.Item2);Check(double.IsFinite(height)&&height>=54&&height<=pair.Item2,"Unsafe panel height for a small/invalid measurement");}
  report.Add("Native callback recovery: injected E_INVALIDARG in an actual DispatcherQueueTimer, queued action and async continuation; later callbacks still run. RO_E_CLOSED is contained, unexpected InvalidOperationException propagates. Context/stack log verified in isolated QA file.");
  report.Add("Panel height: available height below 300 DIP, NaN and infinite desired size produce finite bounded geometry instead of min>max ArgumentException.");
 }
 async Task CheckStability(List<string> report){
  long before=UiFaultBoundary.RecoveredCount;bool motion=prefs.ReducedMotion,force=perfForceWave;string material=prefs.Material;
  var work=monitor.Work;var fixture=new System.Drawing.Bitmap(64,64);using(var paint=System.Drawing.Graphics.FromImage(fixture))paint.Clear(System.Drawing.Color.CornflowerBlue);var cover=ArtworkCodec.FromBitmap(fixture);fixture.Dispose();
  try{tick.Start();occlusionTimer.Start();prefs.ReducedMotion=false;perfForceWave=true;string[] pages={"media","controls","water","time","tasks","shelf","notifications","themes"};string[] materials={"Düz","Cam","Mika","Şeffaf"};
   for(int i=0;i<32;i++){
    if(i%8==0){prefs.Material=materials[(i/8)%materials.Length];ApplyTheme();}
    media.Title=i%3==0?"":i%3==1?"QA 🎵 — 長いタイトル — "+new string('W',140):"QA short track";media.Artist="QA";media.Cover=i%2==0?cover:null;OnMediaChanged();
    if(i%16==0){monitor.Work.Bottom=monitor.Bounds.Top+(int)(275*monitor.Dpi/96);}else monitor.Work=work;
    Navigate(pages[i%pages.Length]);await Task.Delay(110);Navigate("");SetHovered(true);await Task.Delay(35);SetHovered(false);await Task.Delay(35);SetHovered(true);await Task.Delay(35);SetHovered(false);
    DrawLevels(frameWatch.Elapsed.TotalSeconds,.016,i%2==0?.7:0);SetHidden(i%9==0);await Task.Delay(650);
    if(i%8==7){GC.Collect();GC.WaitForPendingFinalizers();}
    Check(UiFaultBoundary.RecoveredCount==before,"Unexpected callback failure during stability run; inspect isolated-ui-faults.log");
   }
   report.Add("Stability: 32 panel/clock cycles with live main/occlusion timers and edge hide/reveal, 4 materials, long/empty/Unicode titles, alternating artwork, small work area, and explicit QA-only GC/finalizers; no unexpected recovered UI errors. This accelerated minute-long run is not an hours-long usage guarantee.");
  }finally{tick.Stop();occlusionTimer.Stop();monitor.Work=work;prefs.Material=material;prefs.ReducedMotion=motion;perfForceWave=force;Navigate("");}
 }
}
