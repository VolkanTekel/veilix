// GPL-3.0-or-later. Hover clock, content transitions and a recoverable edge handle.
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Ada {
 public sealed partial class AdaWindow {
  bool headerHovered,edgeHidden,benchmarkEnvironment;Border edgeHandle;Grid headerRight;FrameworkElement animatedBody;
  readonly Dictionary<IntPtr,uint> occludingWindows=new Dictionary<IntPtr,uint>();
  bool clockTargetVisible,clockMotionRunning;int clockMotionGeneration;
  readonly DispatcherTimer hoverExitTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(220)};
  bool ClockVisible{get{return Preferences.ShowClock&&!edgeHidden&&(headerHovered||route!="");}}
  void InitInteractions(){
   InitEdgeMotion();
   var line=new Border{Width=46,Height=2,CornerRadius=new CornerRadius(1),Opacity=.9,VerticalAlignment=VerticalAlignment.Center};line.SetResourceReference(Border.BackgroundProperty,"Accent");
   edgeHandle=new Border{Visibility=Visibility.Collapsed,CornerRadius=new CornerRadius(0,0,4,4),Child=line,Cursor=System.Windows.Input.Cursors.Hand,ToolTip="Veilix'i göstermek için üzerine gel"};edgeHandle.SetResourceReference(Border.BackgroundProperty,"Popup");AutomationProperties.SetName(edgeHandle,"Veilix üst kenar tutamağı");host.Children.Add(edgeHandle);
   MouseEnter+=delegate{if(!testing)HoverEntered();};MouseLeave+=delegate{if(!testing){hoverExitTimer.Stop();hoverExitTimer.Start();}};
   hoverExitTimer.Tick+=delegate{hoverExitTimer.Stop();if(!IsMouseOver&&!themeEditorOpen){SetHeaderHovered(false);CheckFullscreen();}};
  }
  void HoverEntered(){hoverExitTimer.Stop();headerHovered=true;if(edgeHidden)SetEdgeHidden(false);else UpdateHeaderLayout(true);}
  void SetHeaderHovered(bool value){headerHovered=value;UpdateHeaderLayout(true);}
  void UpdateHeaderLayout(bool animate){
   if(header==null||clock==null)return;miniArtist.FontSize=Math.Max(12,12/EffectiveScale);date.FontSize=Math.Max(15,12/EffectiveScale);bool show=ClockVisible;
   if(!animate||!MotionAllowed||!IsLoaded||edgeHidden){StopClockMotion();clockTargetVisible=show;ApplyClockLayout(show);return;}
   if(show==clockTargetVisible){if(!clockMotionRunning)ApplyClockLayout(show);return;}
   bool wasVisible=clock.Visibility==Visibility.Visible;double opacity=wasVisible?clock.Opacity:0;var previousShift=clock.RenderTransform as TranslateTransform;double from=wasVisible?(previousShift!=null?previousShift.Y:0):6;
   StopClockMotion();clockTargetVisible=show;int generation=clockMotionGeneration;ApplyClockLayout(true);clock.Opacity=opacity;var shift=new TranslateTransform(0,from);clock.RenderTransform=shift;clockMotionRunning=true;
   // Keep the clock slot until fade-out finishes, then return all of it to
   // the title. This avoids cutting the exit animation by collapsing early.
   var duration=TimeSpan.FromMilliseconds(show?190:160);
   var fade=new DoubleAnimation(opacity,show?1:0,duration){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.HoldEnd};
   var slide=new DoubleAnimation(from,show?0:-6,duration){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.HoldEnd};
   Timeline.SetDesiredFrameRate(fade,TargetFps);Timeline.SetDesiredFrameRate(slide,TargetFps);
   fade.Completed+=delegate{if(generation!=clockMotionGeneration)return;StopClockMotion();ApplyClockLayout(clockTargetVisible);};
   shift.BeginAnimation(TranslateTransform.YProperty,slide);clock.BeginAnimation(UIElement.OpacityProperty,fade);
  }
  void ApplyClockLayout(bool show){header.ColumnDefinitions[1].Width=new GridLength(104);header.ColumnDefinitions[2].Width=new GridLength(Math.Max(174,((main.Width>0&&!Double.IsNaN(main.Width)?main.Width:IslandWidth)-104)/2));clock.Visibility=show?Visibility.Visible:Visibility.Collapsed;clock.Opacity=show?1:0;clock.RenderTransform=Transform.Identity;UpdateVisualizerState(show);}
  void StopClockMotion(){clockMotionGeneration++;clockMotionRunning=false;if(clock==null)return;clock.BeginAnimation(UIElement.OpacityProperty,null);var shift=clock.RenderTransform as TranslateTransform;if(shift!=null)shift.BeginAnimation(TranslateTransform.YProperty,null);}
  void StartContentMotion(){
   StopContentMotion();if(!MotionAllowed||!IsLoaded||content==null)return;
   var body=content;animatedBody=body;bool horizontal=pageMotionDirection!=0;double distance=horizontal?pageMotionDirection*24:9;var shift=new TranslateTransform(horizontal?distance:0,horizontal?0:distance);body.RenderTransform=shift;
   var slide=new DoubleAnimation(distance,0,TimeSpan.FromMilliseconds(230)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.Stop};
   var fade=new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(170)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.Stop};
   Timeline.SetDesiredFrameRate(slide,TargetFps);Timeline.SetDesiredFrameRate(fade,TargetFps);slide.Completed+=delegate{if(animatedBody==body)StopContentMotion();};shift.BeginAnimation(horizontal?TranslateTransform.XProperty:TranslateTransform.YProperty,slide);body.BeginAnimation(UIElement.OpacityProperty,fade);
  }
  void StopContentMotion(){if(animatedBody==null)return;animatedBody.BeginAnimation(UIElement.OpacityProperty,null);var shift=animatedBody.RenderTransform as TranslateTransform;if(shift!=null){shift.BeginAnimation(TranslateTransform.YProperty,null);shift.BeginAnimation(TranslateTransform.XProperty,null);}animatedBody.RenderTransform=Transform.Identity;animatedBody=null;}
  FrameworkElement retiringPage;
  void RetirePage(FrameworkElement body){retiringPage=body;body.IsHitTestVisible=false;body.VerticalAlignment=VerticalAlignment.Top;body.Height=body.ActualHeight;Grid.SetRow(body,1);main.Children.Add(body);var fade=new DoubleAnimation(1,0,TimeSpan.FromMilliseconds(150)){FillBehavior=FillBehavior.HoldEnd};Timeline.SetDesiredFrameRate(fade,TargetFps);fade.Completed+=delegate{if(retiringPage==body)StopRetiringPage();};body.BeginAnimation(UIElement.OpacityProperty,fade);}
  void StopRetiringPage(){if(retiringPage==null)return;retiringPage.BeginAnimation(UIElement.OpacityProperty,null);main.Children.Remove(retiringPage);retiringPage=null;}
  void SetEdgeHidden(bool hidden){ChangeEdgeState(hidden);}
  void ResizeEdgeHandle(){shadowProgress=0;double scale=EffectiveScale;Width=Math.Round(112*scale);Height=Math.Max(7,Math.Round(8*scale));visualWidth=Width;visualHeight=Height;reveal.RadiusX=reveal.RadiusY=4*scale;reveal.Rect=new Rect(0,0,Width,Height);bodyReveal.RadiusX=bodyReveal.RadiusY=4*scale;bodyReveal.Rect=reveal.Rect;topReveal.Rect=new Rect(0,0,Width,Height/2);Native.Crop(this,reveal.Rect,4*scale);UpdateClippedBackdrop(reveal.Rect,4*scale,0);Place();}
  Native.Rect HeaderScreenBounds(){double scale=LayoutMetrics.Scale(selected,Preferences.Scale)*selected.Dpi/96.0;int width=(int)Math.Round((IslandWidth+NotchShoulder*2)*scale),height=(int)Math.Round(HeaderHeight*scale);int left=selected.Bounds.Left+(selected.Bounds.Width-width)/2;return new Native.Rect{Left=left,Top=selected.Bounds.Top,Right=left+width,Bottom=selected.Bounds.Top+height};}
  void ApplyOcclusion(bool covered){if(headerHovered||themeEditorOpen)return;SetEdgeHidden(covered);}
  void CheckOcclusion(){
   if((testing&&!benchmarkEnvironment)||inspection||closing||selected==null||headerHovered||themeEditorOpen)return;
   UpdateOcclusionTracking(Native.GetForegroundWindow());
  }
  bool CoversAda(Native.Rect bounds){var display=selected.Bounds;bool full=Math.Abs(bounds.Left-display.Left)<=3&&Math.Abs(bounds.Top-display.Top)<=3&&Math.Abs(bounds.Right-display.Right)<=3&&Math.Abs(bounds.Bottom-display.Bottom)<=3;return full?Preferences.HideFullscreen:Preferences.AutoHideOverlap&&Native.Overlaps(HeaderScreenBounds(),bounds);}
  void UpdateOcclusionTracking(IntPtr foreground){
   var own=new WindowInteropHelper(this).Handle;var expired=new List<IntPtr>();Native.Rect bounds;
   // Focus may move elsewhere while the original window still covers Ada.
   // Remember every blocker and validate its own visibility/bounds until gone.
   foreach(var item in occludingWindows)if(Native.WindowProcessId(item.Key)!=item.Value||!Native.ForeignWindowBounds(item.Key,own,out bounds)||!CoversAda(bounds))expired.Add(item.Key);
   foreach(var handle in expired)occludingWindows.Remove(handle);
   if(Native.ForeignWindowBounds(foreground,own,out bounds)&&CoversAda(bounds))occludingWindows[foreground]=Native.WindowProcessId(foreground);
   ApplyOcclusion(occludingWindows.Count>0);
  }
 }
}
