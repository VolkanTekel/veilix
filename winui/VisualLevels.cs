using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
namespace Veilix;
public sealed partial class MainWindow {
 Visual[] barVisuals;
 double barDpi;readonly int[] barPixels=new int[9];
 void DrawLevels(double now,double dt,double peak){double dpi=host.Dpi;
  if(barVisuals==null){barVisuals=new Visual[bars.Count];for(int i=0;i<bars.Count;i++){bars[i].Height=22;barVisuals[i]=ElementCompositionPreview.GetElementVisual(bars[i]);}}
  // Dimensions do not change with sound. Avoid thousands of redundant WinUI
  // property writes per second; rebuild these only after a monitor/DPI change.
  if(barDpi!=dpi){barDpi=dpi;double bw=Math.Max(2,Math.Round(3*dpi))/dpi;waves.Spacing=Math.Round(4*dpi)/dpi;for(int i=0;i<bars.Count;i++){bars[i].Width=bw;barVisuals[i].CenterPoint=new Vector3((float)(bw/2),11,0);barPixels[i]=-1;}}
  for(int i=0;i<bars.Count;i++){if(peak<=0)peakLevels[i]=0;else{double target=peak*(.7+.3*Math.Sin(now*11+i*.9)),speed=target>peakLevels[i]?38:15;peakLevels[i]+=(target-peakLevels[i])*(1-Math.Exp(-speed*dt));}int pixels=(int)Math.Round((3+peakLevels[i]*19)*dpi);if(pixels==barPixels[i])continue;barVisuals[i].Scale=new Vector3(1,(float)(pixels/dpi/22),1);barPixels[i]=pixels;}
 }
}
