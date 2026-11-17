using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Ada;
namespace Veilix;
public sealed partial class MainWindow {
 [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")]static extern IntPtr TestWindowOwner(IntPtr hwnd,int index,IntPtr value);
 [DllImport("user32.dll")]static extern IntPtr GetWindow(IntPtr hwnd,uint relation);
 [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int w,int h,uint flags);
 [DllImport("user32.dll")]static extern bool IsWindowEnabled(IntPtr hwnd);
 [DllImport("user32.dll")]static extern IntPtr SendMessageW(IntPtr hwnd,uint msg,IntPtr w,IntPtr l);
 [DllImport("user32.dll")]static extern int GetWindowRgnBox(IntPtr hwnd,out Native.Rect bounds);
 async Task CheckWindowLayers(List<string> report,string folder){
  string material=prefs.Material;bool reduced=prefs.ReducedMotion;prefs.ReducedMotion=true;prefs.Material="Düz";ApplyTheme();var original=GetWindow(host.Hwnd,4);prefs.Material="Cam";ApplyTheme();Navigate("");SetHidden(false);root.UpdateLayout();await Task.Delay(60);
  Check(SharedGlass&&unifiedGlass.ForegroundAboveGlass,"Initial foreground/backdrop ownership");
  Check(IsWindowEnabled(unifiedGlass.Hwnd)&&SendMessageW(unifiedGlass.Hwnd,0x84,IntPtr.Zero,IntPtr.Zero)==new IntPtr(-1),"Glass can intercept clicks or produce disabled-window beep");
  // Reproduce the captured state: independent topmost glass in front of a
  // foreground window whose presenter dropped WS_EX_TOPMOST.
  TestWindowOwner(host.Hwnd,-8,original);SetWindowPos(host.Hwnd,new IntPtr(-2),0,0,0,0,0x0213);SetWindowPos(unifiedGlass.Hwnd,new IntPtr(-1),0,0,0,0,0x0213);
  Check(!unifiedGlass.ForegroundAboveGlass,"Regression fixture did not reproduce order loss");host.EnsureTopmost();unifiedGlass.Sync();
  Check(host.IsTopmost&&unifiedGlass.ForegroundAboveGlass,"Captured blank-glass state did not recover");
  for(int i=0;i<8;i++){SetWindowPos(unifiedGlass.Hwnd,new IntPtr(-1),0,0,0,0,0x0213);unifiedGlass.Sync();Check(unifiedGlass.ForegroundAboveGlass,"Backdrop promotion covered foreground");}
  SetHidden(true);
  Check(edgeGrip.Opacity==1&&tint.Opacity==0&&glass.Opacity==0&&unifiedGlass.BodyOpacity==0,
   "A rectangular panel surface is still painted behind the hidden grip");
  var expected=HiddenHandleRegion(TargetScale).Value;
  foreach(var hwnd in new[]{host.Hwnd,unifiedGlass.Hwnd}){
   Check(GetWindowRgnBox(hwnd,out var region)==2&&region.Left==expected.Left&&region.Top==0&&
    region.Right==expected.Right&&region.Bottom==expected.Bottom,"Hidden window still has an input surface outside the handle");
  }
  Check(expected.Right-expected.Left<=Math.Ceiling(48*TargetScale)+1,"Hidden mouse target still spans the old rectangular panel");
  SetHidden(false);
  Check(edgeGrip.Visibility==Microsoft.UI.Xaml.Visibility.Collapsed&&tint.Opacity==1&&glass.Opacity==1&&unifiedGlass.BodyOpacity==1,
   "Reopening did not restore the panel surface");
  Check(GetWindowRgnBox(host.Hwnd,out _)==0&&GetWindowRgnBox(unifiedGlass.Hwnd,out _)==0,"Reopening kept the hidden handle region");
  foreach(string mode in new[]{"Cam","Mika","Şeffaf","Düz"}){
   prefs.Material=mode;ApplyTheme();SetHidden(true);root.UpdateLayout();
   await CheckHiddenGripPixels();
   SetHidden(false);
  }
  report.Add("Hidden handle: native regions follow the 48 DIP input target. Actual WinUI pixels show only the grip in all four materials; revealing restores panel surfaces.");
  prefs.Material="Düz";ApplyTheme();Check(WindowHost.IsWindow(host.Hwnd)&&GetWindow(host.Hwnd,4)==original,"Destroying glass destroyed foreground or retained dead owner");
  prefs.Material="Cam";ApplyTheme();Check(unifiedGlass.ForegroundAboveGlass,"Recreated glass covered foreground");prefs.Material=material;ApplyTheme();prefs.ReducedMotion=reduced;
  tools.Data.Notices.Add(new NoticeEntry{Kind="system-caps",Title="Caps Lock açık",AtUtc=DateTimeOffset.UtcNow.UtcTicks});tools.NormalizeNotices();Check(!tools.NoticeEnabled("system-caps")&&tools.Data.Notices.TrueForAll(n=>n.Kind!="system-caps"),"Caps Lock still enabled or shown in history");
  File.WriteAllText(Path.Combine(folder,"window-layer-check.txt"),"Reproduced unlinked TOPMOST backdrop above non-TOPMOST foreground; recovery restored native owner chain and foreground TOPMOST. Eight backdrop promotions stayed behind foreground. Hit test returned HTTRANSPARENT; background enabled. Disposal restored previous owner without destroying WinUI window.");
  report.Add("Window layers: captured blank-glass z-order reproduced and recovered; foreground is owned by backdrop, enforcing foreground above it, 8 backdrop promotions cannot cover it, glass input passes through, material disposal restores original owner. Caps Lock generation disabled and history filtered.");
 }
}
