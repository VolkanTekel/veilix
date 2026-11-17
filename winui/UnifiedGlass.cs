// One Windows Composition backdrop for the body AND both inverse curves.
// WinUI owns text/controls; it never receives a second desktop target on its HWND.
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Ada;
using Microsoft.Graphics.Canvas.Geometry;
using WinRT;
using WC=Windows.UI.Composition;
namespace Veilix;
[ComImport,Guid("29E691FA-4567-4DCA-B319-D0F207EB6807"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IGlassDesktopInterop {void CreateDesktopWindowTarget(IntPtr hwnd,[MarshalAs(UnmanagedType.Bool)]bool topmost,out IntPtr target);}
internal sealed class UnifiedGlass:IDisposable {
 [StructLayout(LayoutKind.Sequential)]struct QueueOptions{public int Size,ThreadType,Apartment;}
 [StructLayout(LayoutKind.Sequential)]struct Accent{public int State,Flags;public uint Color;public int Animation;}
 [StructLayout(LayoutKind.Sequential)]struct CompositionData{public int Attribute;public IntPtr Data;public int Size;}
 [DllImport("CoreMessaging.dll")]static extern int CreateDispatcherQueueController(QueueOptions options,out IntPtr controller);
 [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr CreateWindowExW(uint ex,string cls,string title,uint style,int x,int y,int w,int h,IntPtr parent,IntPtr menu,IntPtr instance,IntPtr param);
 [DllImport("user32.dll")]static extern bool DestroyWindow(IntPtr hwnd);
 [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr hwnd,int command);
 [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int w,int h,uint flags);
 [DllImport("user32.dll")]static extern IntPtr GetWindow(IntPtr hwnd,uint relation);
 [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]static extern IntPtr GetLong(IntPtr hwnd,int index);
 [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW",SetLastError=true)]static extern IntPtr SetLong(IntPtr hwnd,int index,IntPtr value);
 [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr hwnd,out Native.Rect rect);
 [DllImport("comctl32.dll")]static extern bool SetWindowSubclass(IntPtr hwnd,GlassProc proc,UIntPtr id,IntPtr data);
 [DllImport("comctl32.dll")]static extern bool RemoveWindowSubclass(IntPtr hwnd,GlassProc proc,UIntPtr id);
 [DllImport("comctl32.dll")]static extern IntPtr DefSubclassProc(IntPtr hwnd,uint msg,IntPtr w,IntPtr l);
 delegate IntPtr GlassProc(IntPtr hwnd,uint msg,IntPtr w,IntPtr l,UIntPtr id,IntPtr data);
 [StructLayout(LayoutKind.Sequential)]struct PixelPoint{public int X,Y;}
 [DllImport("user32.dll")]static extern bool GetClientRect(IntPtr hwnd,out Native.Rect rect);
 [DllImport("user32.dll")]static extern bool ClientToScreen(IntPtr hwnd,ref PixelPoint point);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr hwnd);
 [DllImport("user32.dll")]static extern bool IsIconic(IntPtr hwnd);
 [DllImport("user32.dll")]static extern bool SetWindowCompositionAttribute(IntPtr hwnd,ref CompositionData data);
 static IntPtr queueController;
 readonly IntPtr owner,originalOwner;readonly GlassProc inputProc;IntPtr hwnd;WC.Compositor compositor;Windows.UI.Composition.Desktop.DesktopWindowTarget target;WC.ContainerVisual root;WC.CompositionBackdropBrush brush;WC.SpriteVisual body;WC.CompositionRoundedRectangleGeometry bodyGeometry;WC.CompositionGeometricClip bodyClip;
 readonly List<WC.CompositionObject> resources=new();readonly List<WC.SpriteVisual> curves=new();
 internal bool SharedSource {get{if(body?.Brush!=brush||curves.Count!=2)return false;foreach(var curve in curves)if(curve.Brush!=brush)return false;return true;}}
 internal Vector2 ClipSize=>bodyGeometry.Size;
 bool visible,disposed,syncing;float width=500,dpi=1;internal bool Visible=>visible;internal string Error="";
 internal UnifiedGlass(IntPtr owner,CanvasGeometry left,CanvasGeometry right){this.owner=owner;originalOwner=GetWindow(owner,4);inputProc=Input;try{
  if(Windows.System.DispatcherQueue.GetForCurrentThread()==null&&queueController==IntPtr.Zero)Marshal.ThrowExceptionForHR(CreateDispatcherQueueController(new QueueOptions{Size=Marshal.SizeOf<QueueOptions>(),ThreadType=2,Apartment=2},out queueController));
  hwnd=CreateWindowExW(0x082000A8,"Static","Veilix glass",0x80000000,0,0,1,1,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);if(hwnd==IntPtr.Zero)throw new System.ComponentModel.Win32Exception();
  if(!SetWindowSubclass(hwnd,inputProc,(UIntPtr)27,IntPtr.Zero))throw new System.ComponentModel.Win32Exception();
  // The foreground is OWNED BY the backdrop: Windows keeps an owned window
  // above its owner even if WinUI's presenter resets TOPMOST. The reverse
  // relationship would put the glass above the text and swallow all input.
  LinkForeground();
  int off=1,noBorder=unchecked((int)0xFFFFFFFE),useHost=1;Native.DwmSetWindowAttribute(hwnd,2,ref off,4);Native.DwmSetWindowAttribute(hwnd,34,ref noBorder,4);Native.DwmSetWindowAttribute(hwnd,17,ref useHost,4);
  var accent=new Accent{State=5};var ptr=Marshal.AllocHGlobal(Marshal.SizeOf<Accent>());try{Marshal.StructureToPtr(accent,ptr,false);var data=new CompositionData{Attribute=19,Data=ptr,Size=Marshal.SizeOf<Accent>()};if(!SetWindowCompositionAttribute(hwnd,ref data))throw new InvalidOperationException("Host backdrop unavailable");}finally{Marshal.FreeHGlobal(ptr);}
  compositor=new WC.Compositor();var interop=compositor.As<IGlassDesktopInterop>();interop.CreateDesktopWindowTarget(hwnd,false,out var raw);try{target=WinRT.MarshalInspectable<Windows.UI.Composition.Desktop.DesktopWindowTarget>.FromAbi(raw);}finally{Marshal.Release(raw);}
  root=compositor.CreateContainerVisual();root.BorderMode=WC.CompositionBorderMode.Soft;target.Root=root;brush=compositor.CreateHostBackdropBrush();
  bodyGeometry=compositor.CreateRoundedRectangleGeometry();bodyGeometry.CornerRadius=new Vector2(24);bodyGeometry.Offset=new Vector2(0,-24);bodyGeometry.Size=new Vector2(width,78);bodyClip=compositor.CreateGeometricClip(bodyGeometry);body=compositor.CreateSpriteVisual();body.BorderMode=WC.CompositionBorderMode.Soft;body.Brush=brush;body.Clip=bodyClip;root.Children.InsertAtBottom(body);
  foreach(var shape in new[]{left,right}){var path=new WC.CompositionPath(shape);var geometry=compositor.CreatePathGeometry(path);var clip=compositor.CreateGeometricClip(geometry);var sprite=compositor.CreateSpriteVisual();sprite.Size=new Vector2(14);sprite.BorderMode=WC.CompositionBorderMode.Soft;sprite.Brush=brush;sprite.Clip=clip;root.Children.InsertAtTop(sprite);resources.Add(sprite);resources.Add(clip);resources.Add(geometry);curves.Add(sprite);}
 }catch(Exception ex){Error=ex.GetType().Name+": "+ex.Message;Dispose();throw;}}
 internal void SetVisible(bool value){if(visible==value)return;visible=value;if(!value){ShowWindow(hwnd,0);return;}Sync();}
 IntPtr Input(IntPtr h,uint msg,IntPtr w,IntPtr l,UIntPtr id,IntPtr data)=>msg==0x84?new IntPtr(-1):msg==0x21?new IntPtr(3):DefSubclassProc(h,msg,w,l);
 void LinkForeground(){if(GetWindow(owner,4)==hwnd)return;Marshal.SetLastPInvokeError(0);SetLong(owner,-8,hwnd);if(GetWindow(owner,4)!=hwnd)throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError(),"Cam ve içerik pencereleri bağlanamadı.");}
 internal IntPtr Hwnd=>hwnd;
 WindowRegion region;
 internal void SetHandleRegion((int Left,int Right,int Bottom)? bounds)=>region.Apply(hwnd,bounds);
 internal void SetCurveOpacity(float opacity){
  foreach(var curve in curves)if(curve.Opacity!=opacity)curve.Opacity=opacity;
 }
 internal bool CurvesMatchOpacity(float opacity)=>curves.TrueForAll(curve=>curve.Opacity==opacity);
 internal float BodyOpacity=>body.Opacity;
 internal void SetBodyOpacity(float opacity){if(body.Opacity!=opacity)body.Opacity=opacity;}
 internal bool ForegroundAboveGlass {get{if(hwnd==IntPtr.Zero||GetWindow(owner,4)!=hwnd)return false;var next=GetWindow(owner,2);for(int i=0;i<1024&&next!=IntPtr.Zero;i++,next=GetWindow(next,2))if(next==hwnd)return true;return false;}}
 internal void Sync(){if(disposed||!visible||syncing)return;syncing=true;try{LinkForeground();if(!IsWindowVisible(owner)||IsIconic(owner)){if(IsWindowVisible(hwnd))ShowWindow(hwnd,0);return;}if(!GetClientRect(owner,out var r))return;var origin=new PixelPoint();if(!ClientToScreen(owner,ref origin))return;bool same=GetWindowRect(hwnd,out var current)&&current.Left==origin.X&&current.Top==origin.Y&&current.Width==r.Width&&current.Height==r.Height;if(!same||!IsWindowVisible(hwnd)||!ForegroundAboveGlass)SetWindowPos(hwnd,owner,origin.X,origin.Y,r.Width,r.Height,0x0250);}finally{syncing=false;}}
 internal void SetBounds(double bodyWidth,double extent,double from,double to,double scale,bool animate,double duration){if(disposed)return;width=(float)bodyWidth;dpi=(float)scale;root.Size=new Vector2(width+40,(float)extent+20);root.Scale=new Vector3(dpi,dpi,1);body.Offset=new Vector3(20,0,0);body.Size=new Vector2(width,(float)extent);curves[0].Offset=new Vector3(6,0,0);curves[1].Offset=new Vector3(20+width,0,0);curves[0].Size=curves[1].Size=new Vector2(14);
  bodyGeometry.StopAnimation("Size");bodyGeometry.StopAnimation("Offset");bodyGeometry.Offset=new Vector2(0,-24);bodyGeometry.Size=new Vector2(width,(float)(to+24));
  if(animate){var animation=compositor.CreateVector2KeyFrameAnimation();animation.InsertKeyFrame(0,new Vector2(width,(float)(from+24)));animation.InsertKeyFrame(1,new Vector2(width,(float)(to+24)),compositor.CreateCubicBezierEasingFunction(new Vector2(1f/3,1),new Vector2(2f/3,1)));animation.Duration=TimeSpan.FromMilliseconds(duration);bodyGeometry.StartAnimation("Size",animation);animation.Dispose();}Sync();
 }
 internal void SetNarrow(bool narrow,bool animate){if(disposed)return;float left=narrow?(width-WindowHost.HandleWidth)/2:0,w=narrow?WindowHost.HandleWidth:width;
  if(!animate){bodyGeometry.StopAnimation("Offset");bodyGeometry.StopAnimation("Size");bodyGeometry.Offset=new Vector2(left,-24);bodyGeometry.Size=new Vector2(w,78);return;}
  var move=compositor.CreateVector2KeyFrameAnimation();move.InsertKeyFrame(1,new Vector2(left,-24),compositor.CreateCubicBezierEasingFunction(new Vector2(1f/3,1),new Vector2(2f/3,1)));move.Duration=TimeSpan.FromMilliseconds(280);bodyGeometry.StartAnimation("Offset",move);var size=compositor.CreateVector2KeyFrameAnimation();size.InsertKeyFrame(1,new Vector2(w,78),compositor.CreateCubicBezierEasingFunction(new Vector2(1f/3,1),new Vector2(2f/3,1)));size.Duration=move.Duration;bodyGeometry.StartAnimation("Size",size);move.Dispose();size.Dispose();}
 public void Dispose(){if(disposed)return;disposed=true;visible=false;if(hwnd!=IntPtr.Zero&&GetWindow(owner,4)==hwnd)SetLong(owner,-8,originalOwner);try{if(target!=null)target.Root=null;}catch{}foreach(var resource in resources)resource.Dispose();body?.Dispose();bodyClip?.Dispose();bodyGeometry?.Dispose();brush?.Dispose();root?.Dispose();target?.Dispose();compositor?.Dispose();if(hwnd!=IntPtr.Zero){RemoveWindowSubclass(hwnd,inputProc,(UIntPtr)27);DestroyWindow(hwnd);hwnd=IntPtr.Zero;}}
}
