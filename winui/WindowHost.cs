using System;
using System.Runtime.InteropServices;
using Ada;
namespace Veilix;
internal struct WindowRegion {
 (int Left,int Right,int Bottom)? current;
 internal void Apply(IntPtr hwnd,(int Left,int Right,int Bottom)? bounds){
  if(current!=bounds&&Native.TrySetWindowRegion(hwnd,bounds))current=bounds;
 }
}
internal sealed class WindowHost:IDisposable {
 internal const int HandleWidth=48;
 WindowRegion region;
 internal void SetHandleRegion((int Left,int Right,int Bottom)? bounds)=>region.Apply(Hwnd,bounds);
 internal readonly IntPtr Hwnd;readonly SubclassProc proc;readonly MouseProc mouseProc;IntPtr mouseHook,foregroundHook,locationHook,visibilityHook;WinEventProc windowEvent;readonly Action outside,devices,display;readonly Microsoft.UI.Xaml.Window window;readonly bool inspect;internal Func<double> Height,Width;internal Func<bool> PopupOpen,Narrow;internal Action PositionChanged,ClipboardChanged;bool clipboardListening;internal Action<bool> HoverChanged;
 delegate IntPtr SubclassProc(IntPtr hwnd,uint msg,IntPtr w,IntPtr l,UIntPtr id,IntPtr data);
 delegate void WinEventProc(IntPtr hook,uint kind,IntPtr hwnd,int objectId,int childId,uint thread,uint timestamp);
 [DllImport("user32.dll")]static extern IntPtr SetWinEventHook(uint first,uint last,IntPtr module,WinEventProc callback,uint process,uint thread,uint flags);
 [DllImport("user32.dll")]static extern bool UnhookWinEvent(IntPtr hook);
 internal void WatchOcclusion(Action<IntPtr,uint> changed)
 {
  windowEvent=(_,kind,hwnd,objectId,childId,_,_)=>
  {
   if(objectId==0&&childId==0&&hwnd!=IntPtr.Zero&&GetAncestor(hwnd,2)==hwnd)changed(hwnd,kind);
  };
  const uint skipOwnProcess=2;
  foregroundHook=SetWinEventHook(3,3,IntPtr.Zero,windowEvent,0,0,skipOwnProcess);
  locationHook=SetWinEventHook(0x800B,0x800B,IntPtr.Zero,windowEvent,0,0,skipOwnProcess);
  visibilityHook=SetWinEventHook(0x8002,0x8003,IntPtr.Zero,windowEvent,0,0,skipOwnProcess);
 }
 delegate IntPtr MouseProc(int code,IntPtr w,IntPtr l);
 [DllImport("comctl32.dll")]static extern bool SetWindowSubclass(IntPtr hwnd,SubclassProc proc,UIntPtr id,IntPtr data);
 [DllImport("comctl32.dll")]static extern bool RemoveWindowSubclass(IntPtr hwnd,SubclassProc proc,UIntPtr id);
 [DllImport("comctl32.dll")]static extern IntPtr DefSubclassProc(IntPtr hwnd,uint msg,IntPtr w,IntPtr l);
 [DllImport("user32.dll")]static extern IntPtr SetWindowsHookEx(int id,MouseProc proc,IntPtr module,uint thread);
 [DllImport("user32.dll")]static extern bool UnhookWindowsHookEx(IntPtr hook);
 [DllImport("user32.dll")]static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr w,IntPtr l);
 [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]static extern IntPtr GetLong(IntPtr hwnd,int index);
 [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")]static extern IntPtr SetLong(IntPtr hwnd,int index,IntPtr value);
 [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int w,int h,uint flags);
 [DllImport("user32.dll")]internal static extern bool GetCursorPos(out Point p);
 [DllImport("user32.dll")]internal static extern uint GetDpiForWindow(IntPtr hwnd);
 [DllImport("user32.dll")]internal static extern bool IsWindow(IntPtr hwnd);
 [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(Point point);
 [DllImport("user32.dll")]static extern IntPtr GetAncestor(IntPtr hwnd,uint flags);
 [StructLayout(LayoutKind.Sequential)]internal struct Point {public int X,Y;}
 double lastDpi=1;internal double Dpi{get{uint value=GetDpiForWindow(Hwnd);if(value>=48&&value<=960)lastDpi=value/96.0;return lastDpi;}}
 internal bool ContainsPointer(){if(!GetCursorPos(out var point)||!Native.GetWindowRect(Hwnd,out var rect))return false;double dpi=Math.Max(.5,Dpi),x=(point.X-rect.Left)/dpi-20,y=(point.Y-rect.Top)/dpi,width=Width?.Invoke()??500;double left=Narrow?.Invoke()==true?(width-HandleWidth)/2:0,right=Narrow?.Invoke()==true?left+HandleWidth:width;return x>=left&&x<=right&&y>=0&&y<=(Height?.Invoke()??54);}
 internal bool UsesToolWindowStyle {get{long ex=GetLong(Hwnd,-20).ToInt64();return (ex&0x80L)!=0&&(ex&0x40000L)==0;}}
 internal WindowHost(Microsoft.UI.Xaml.Window window,Action outside,Action devices,Action display){
  this.window=window;inspect=Array.IndexOf(Environment.GetCommandLineArgs(),"--inspect")>=0;
  Hwnd=WinRT.Interop.WindowNative.GetWindowHandle(window);this.outside=outside;this.devices=devices;this.display=display;proc=Handle;mouseProc=Mouse;SetWindowSubclass(Hwnd,proc,(UIntPtr)19,IntPtr.Zero);
  long style=GetLong(Hwnd,-16).ToInt64()&~(0x00C00000L|0x00040000L|0x00800000L|0x00010000L|0x00020000L);SetLong(Hwnd,-16,(IntPtr)style);
  long ex=GetLong(Hwnd,-20).ToInt64();ex=(ex|0x00200080L)&~0x00040000L;SetLong(Hwnd,-20,(IntPtr)ex);
  ApplySwitcherPolicy();window.Activated+=Activated;
  int off=1,border=unchecked((int)0xFFFFFFFE);Native.DwmSetWindowAttribute(Hwnd,2,ref off,4);Native.DwmSetWindowAttribute(Hwnd,33,ref off,4);Native.DwmSetWindowAttribute(Hwnd,34,ref border,4);
  var margins=new Native.Margins{Left=-1,Right=-1,Top=-1,Bottom=-1};Native.DwmExtendFrameIntoClientArea(Hwnd,ref margins);
 }
 void Activated(object sender,Microsoft.UI.Xaml.WindowActivatedEventArgs e){if(e.WindowActivationState!=Microsoft.UI.Xaml.WindowActivationState.Deactivated&&IsWindow(Hwnd))UiFaultBoundary.Run(()=>{ApplySwitcherPolicy();EnsureTopmost();PositionChanged?.Invoke();},"window activation");}
 internal bool IsTopmost=>(GetLong(Hwnd,-20).ToInt64()&8)!=0;
 internal void EnsureTopmost(){if(IsWindow(Hwnd)&&!IsTopmost)SetWindowPos(Hwnd,new IntPtr(-1),0,0,0,0,0x0213);}
 void ApplySwitcherPolicy(){
  // AppWindow owns shell visibility in WinUI. Native styles alone can be
  // overwritten when the window is activated or its presenter is updated.
  if(window.AppWindow.IsShownInSwitchers!=inspect)window.AppWindow.IsShownInSwitchers=inspect;
  long before=GetLong(Hwnd,-20).ToInt64(),after=inspect?(before&~0x80L)|0x40000L:(before|0x80L)&~0x40000L;
  if(before!=after){SetLong(Hwnd,-20,(IntPtr)after);SetWindowPos(Hwnd,IntPtr.Zero,0,0,0,0,0x0037);}
 }
 [DllImport("user32.dll")]static extern bool AddClipboardFormatListener(IntPtr hwnd);
 [DllImport("user32.dll")]static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
 internal void ListenToClipboard(bool enabled){
  if(enabled&&!clipboardListening)clipboardListening=AddClipboardFormatListener(Hwnd);
  else if(!enabled&&clipboardListening){RemoveClipboardFormatListener(Hwnd);clipboardListening=false;}
 }
 IntPtr Handle(IntPtr hwnd,uint msg,IntPtr w,IntPtr l,UIntPtr id,IntPtr data){
  if(msg==0x031D)ClipboardChanged?.Invoke();if(msg==0x219)devices();
  if(msg==0x84){int x=(short)(l.ToInt64()&65535),y=(short)((l.ToInt64()>>16)&65535);Native.GetWindowRect(Hwnd,out var r);double dx=(x-r.Left)/Dpi-20,dy=(y-r.Top)/Dpi;double width=Width?.Invoke()??500,height=Height?.Invoke()??54;double left=Narrow?.Invoke()==true?(width-HandleWidth)/2:0,right=Narrow?.Invoke()==true?left+HandleWidth:width;if(dx<left||dx>right||dy<0||dy>height){HoverChanged?.Invoke(ContainsPointer());return (IntPtr)(-1);}}
  var result=DefSubclassProc(hwnd,msg,w,l);if(msg==0x7E||msg==0x2E0)display();if(msg==0x47||msg==0x18)PositionChanged?.Invoke();if(msg==0x200||msg==0x2A3||msg==0x2A2)HoverChanged?.Invoke(ContainsPointer());return result;
 }
 IntPtr Mouse(int code,IntPtr w,IntPtr l){if(code>=0&&(w.ToInt32()==0x201||w.ToInt32()==0x204)){var p=Marshal.PtrToStructure<Point>(l);IntPtr target=GetAncestor(WindowFromPoint(p),2);if(target!=Hwnd&&Native.WindowProcessId(target)!=Native.WindowProcessId(Hwnd)&&!(PopupOpen?.Invoke()??false))outside();}return CallNextHookEx(mouseHook,code,w,l);}
 internal void WatchOutside(bool enabled){if(enabled&&mouseHook==IntPtr.Zero)mouseHook=SetWindowsHookEx(14,mouseProc,IntPtr.Zero,0);else if(!enabled&&mouseHook!=IntPtr.Zero){UnhookWindowsHookEx(mouseHook);mouseHook=IntPtr.Zero;}}
 internal void Place(Display monitor,int width,int height,int offset=0)
 {
  int x=monitor.Bounds.Left+(monitor.Bounds.Width-width)/2,y=monitor.Bounds.Top+offset;
  if(IsTopmost&&Native.GetWindowRect(Hwnd,out var current)&&current.Left==x&&current.Top==y&&current.Width==width&&current.Height==height)return;
  SetWindowPos(Hwnd,new IntPtr(-1),x,y,width,height,0x0050);
 }
 public void Dispose(){window.Activated-=Activated;if(foregroundHook!=IntPtr.Zero)UnhookWinEvent(foregroundHook);if(locationHook!=IntPtr.Zero)UnhookWinEvent(locationHook);if(visibilityHook!=IntPtr.Zero)UnhookWinEvent(visibilityHook);WatchOutside(false);ListenToClipboard(false);RemoveWindowSubclass(Hwnd,proc,(UIntPtr)19);}
}

