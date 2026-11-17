using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Diagnostics;
using Ada;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using static Veilix.UiLanguage;
namespace Veilix;
public sealed partial class MainWindow {
 readonly Border edgeGrip=new(){
  Width=32,Height=2.4,CornerRadius=new CornerRadius(1.2),Padding=new Thickness(4,.7,4,.7),
  HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Top,
  Margin=new Thickness(0,CompactHeight-5.2,0,0),Visibility=Visibility.Collapsed,IsHitTestVisible=false,
  Child=new Border{CornerRadius=new CornerRadius(.5)}
 };
 void ApplyGripTheme(ThemePalette palette){
  edgeGrip.Background=B(ThemePalette.Mix(palette.Start,palette.Accent,.41));
  ((Border)edgeGrip.Child).Background=B(ThemePalette.Mix(palette.Start,palette.Accent,.68));
 }
 void UpdateEdgePresentation(){
  // Keep the curved tint joined to the body while it slides. Once the top
  // strip is above the display edge, hide both foreground and glass curves.
  double shouldersAlpha=positionOffset>-16?1:0;
  double gripAlpha=Math.Clamp((-positionOffset-26)/20,0,1);
  double panelAlpha=1-gripAlpha;
  if(shoulderLayer.Opacity!=shouldersAlpha)shoulderLayer.Opacity=shouldersAlpha;
  unifiedGlass?.SetCurveOpacity((float)shouldersAlpha);
  unifiedGlass?.SetBodyOpacity((float)panelAlpha);
  if(edgeGrip.Opacity!=gripAlpha)edgeGrip.Opacity=gripAlpha;
  var gripVisibility=gripAlpha>0?Visibility.Visible:Visibility.Collapsed;
  if(edgeGrip.Visibility!=gripVisibility)edgeGrip.Visibility=gripVisibility;
  if(header.Opacity!=panelAlpha)header.Opacity=panelAlpha;
  if(tint.Opacity!=panelAlpha)tint.Opacity=panelAlpha;
  if(glass.Opacity!=panelAlpha)glass.Opacity=panelAlpha;
 }
 void BuildHeader(){
  header.Background=B("#00000000");header.Padding=new Thickness(10,0,12,0);miniCover.Width=miniCover.Height=34;miniCover.CornerRadius=new CornerRadius(8);miniCover.Child=miniArt;var info=new StackPanel{VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(9,0,5,0),Spacing=1};TextStyle(miniTitle,14.5,true);TextStyle(miniArtist,11.5,false,true);var titleViewport=new Grid{Height=20};titleViewport.Children.Add(miniTitle);headerMarquee=new Marquee(miniTitle,titleViewport,()=>prefs.Marquee&&media.Playing&&Motion&&route==""&&!hidden&&!perfQuiet);info.Children.Add(titleViewport);info.Children.Add(miniArtist);mediaArea.Children.Add(info);
  TextStyle(clock,30,true);clock.FontFamily=new FontFamily("Bahnschrift");clock.HorizontalAlignment=HorizontalAlignment.Center;clock.VerticalAlignment=VerticalAlignment.Center;clock.Opacity=1;clock.IsHitTestVisible=false;waves.IsHitTestVisible=false;clockArea.Children.Add(clock);for(int i=0;i<9;i++){var bar=new Microsoft.UI.Xaml.Shapes.Rectangle{Width=3,Height=3,RadiusX=1.5,RadiusY=1.5,VerticalAlignment=VerticalAlignment.Center};bars.Add(bar);waves.Children.Add(bar);}clockArea.Children.Add(waves);
  batteryArea.Width=batteryArea.Height=28;var weatherButton=new Button{Padding=new Thickness(0),Background=B("#00000000"),BorderThickness=new Thickness(0)};var weatherRow=new StackPanel{Orientation=Orientation.Horizontal,Spacing=5};weatherIconPlate=new Border{Width=20,Height=20,CornerRadius=new CornerRadius(5),Child=Icons.Get("cloud",17,B("#F0F3F8"))};weatherRow.Children.Add(weatherIconPlate);TextStyle(weatherText,13);weatherRow.Children.Add(weatherText);weatherButton.Content=weatherRow;weatherButton.Click+=(_,e)=>Navigate(route=="weather"?"":"weather");weatherButton.Margin=new Thickness(9,0,0,0);weatherButton.VerticalAlignment=VerticalAlignment.Center;TextStyle(date,13,true);date.VerticalAlignment=VerticalAlignment.Center;date.Margin=new Thickness(12,0,0,0);
  compactElements.Add("cover",miniCover);
  compactElements.Add("media",mediaArea);
  compactElements.Add("clock",clockArea);
  compactElements.Add("battery",batteryArea);
  compactElements.Add("weather",weatherButton);
  compactElements.Add("date",date);
  foreach(var element in compactElements.Values)header.Children.Add(element);
  ApplyCompactLayout();
  header.Tapped+=(_,e)=>{if(FindAncestor<Button>(e.OriginalSource as DependencyObject)!=null)return;Navigate(route==""?(notice?.Kind=="water"?"water":String.IsNullOrEmpty(media.Title)?"controls":"media"):"");e.Handled=true;};
 }
 void BuildNavigation(){var nav=sectionNavigation=new Grid{Margin=new Thickness(14,3,14,7),Padding=new Thickness(4),CornerRadius=new CornerRadius(15),Background=B("#32101822")};for(int i=0;i<5;i++)nav.ColumnDefinitions.Add(new ColumnDefinition{Width=i==2?new GridLength(1,GridUnitType.Star):GridLength.Auto});var bell=notificationBell=IconButton(L("Bildirimler"),"bell",()=>Navigate(route=="notifications"?"media":"notifications"));var back=previousSectionButton=IconButton(L("Önceki sayfa"),"arrow-left",()=>MovePage(-1));var next=nextSectionButton=IconButton(L("Sonraki sayfa"),"arrow-right",()=>MovePage(1));var settings=settingsSectionButton=IconButton(L("Ayarlar"),"settings",()=>Navigate(route=="settings"?"media":"settings"));FrameworkElement[] cells={bell,back,BuildSectionMenu(),next,settings};for(int i=0;i<cells.Length;i++){Grid.SetColumn(cells[i],i);nav.Children.Add(cells[i]);}nav.PointerWheelChanged+=(_,e)=>{MovePage(e.GetCurrentPoint(nav).Properties.MouseWheelDelta>0?-1:1);e.Handled=true;};body.Children.Add(nav);scroller.PointerWheelChanged+=(_,e)=>{if(scroller.ScrollableHeight<1&&FindAncestor<Slider>(e.OriginalSource as DependencyObject)==null&&FindAncestor<TextBox>(e.OriginalSource as DependencyObject)==null&&FindAncestor<ComboBox>(e.OriginalSource as DependencyObject)==null&&FindAncestor<NumberBox>(e.OriginalSource as DependencyObject)==null){MovePage(e.GetCurrentPoint(scroller).Properties.MouseWheelDelta>0?-1:1);e.Handled=true;}};}
 static T FindAncestor<T>(DependencyObject o)where T:DependencyObject{while(o!=null){if(o is T t)return t;o=VisualTreeHelper.GetParent(o);}return null;}
 void MovePage(int delta){if(sectionMenuOpen)return;long now=Environment.TickCount64;if(now-lastNavigation<180)return;lastNavigation=now;int index=Array.IndexOf(Routes,route);Navigate(Routes[(Math.Max(0,index)+delta+Routes.Length)%Routes.Length],delta);}
 void Navigate(string next,int direction=1){
  if(disposed)return;
  if(next!=""&&hidden)SetHidden(false);
  if(next!="notifications")noticePageSize=15;
  bool changed=route!=next;
  route=next;
  if(!testing&&!benchmarking)host.WatchOutside(route!="");
  if(changed){
   if(route!=""){
    body.Visibility=Visibility.Visible;
    ElementCompositionPreview.GetElementVisual(body).Opacity=1;
   }else FadeBodyOut();
  }
  Render(direction,animate:changed);
  UpdateHeader();
 }
 void Render(int direction=1,bool refresh=false,bool animate=false){if(route==""){Resize(54);return;}if(route!="themes")themeCards.Clear();if(route!="custom"){gradientPreview=null;customError=null;applyCustom=null;}page.Children.Clear();timeValue=waterValue=waterGoalValue=positionLabel=null;progress=volume=null;playButton=null;int index=Array.IndexOf(Routes,route);pageTitle.Text=index>=0?L(Names[index]):route=="settings"?L("Ayarlar"):route=="themes"?L("Renkler"):route=="custom"?L("Kendi teman"):route=="weather"?L("Hava durumu"):route=="notifications"?L("Bildirimler"):route=="compact-layout"?L("Panel düzeni"):"";UpdateSectionMenu(index);switch(route){case "media":RenderMedia();break;case "water":RenderWater();break;case "controls":RenderControls();break;case "bluetooth":RenderBluetooth();break;case "time":RenderTime();break;case "tasks":RenderTasks();break;case "shelf":RenderShelf();break;case "clipboard":RenderClipboard();break;case "compact-layout":RenderCompactLayout();break;case "weather":RenderWeather();break;case "notifications":RenderNotifications();break;case "settings":RenderSettings();break;case "themes":RenderThemes();break;case "custom":RenderCustomTheme();break;}if(!refresh)scroller.ChangeView(null,0,null,true);double max=AvailableHeight;double extra=132+5/host.Dpi;scroller.Margin=new Thickness(2.5/host.Dpi);scroller.MaxHeight=max-104-5/host.Dpi;root.UpdateLayout();page.Measure(new Size(width-36-5/host.Dpi,double.PositiveInfinity));Resize(PanelHeight(extra+page.DesiredSize.Height,max));if(animate)AnimateContent(page,direction);}
 void Place(double height)
 {
  UpdateEdgePresentation();
  double scale=TargetScale;
  var region=HiddenHandleRegion(scale);
  if(region==null)ApplyHandleRegion(null);
  host.Place(monitor,(int)Math.Ceiling((width+40)*scale),(int)Math.Ceiling((height+20)*scale),(int)Math.Round(positionOffset*scale));
  if(region!=null)ApplyHandleRegion(region);
 }
 // Composition clipping is visual only. Exclude the transparent area from both HWNDs
 // after the slide finishes, so Windows routes input to other processes underneath.
 (int Left,int Right,int Bottom)? HiddenHandleRegion(double scale){
  if(!hidden||resizing||positionOffset!=HiddenOffset)return null;
  double left=20+(width-WindowHost.HandleWidth)/2;
  return ((int)Math.Floor(left*scale),(int)Math.Ceiling((left+WindowHost.HandleWidth)*scale),(int)Math.Ceiling(CompactHeight*scale));
 }
 void ApplyHandleRegion((int Left,int Right,int Bottom)? bounds){
  host.SetHandleRegion(bounds);
  unifiedGlass?.SetHandleRegion(bounds);
 }
 async void Resize(double height){
  double scale=TargetScale;
  if(height==targetHeight&&resizeScale==scale&&shadowShape.Size.X==(float)width&&(Motion||!resizing))return;
  resizeScale=scale;
  int version=++resizeVersion;clipGeometry.StopAnimation("Offset");clipGeometry.Offset=new Vector2(0,-24);double from=VisibleHeight();double shadowStart=VisibleShadow();targetHeight=height;resizeFrom=from;resizeTo=height;resizeStart=frameWatch.Elapsed.TotalMilliseconds;resizeDuration=height>from?340:280;resizing=Motion&&ready&&Math.Abs(height-from)>.01;double extent=resizing?Math.Max(from,height):height;surface.Height=extent;UpdateShoulders();shadowShape.Size=shadowSprite.Size=new Vector2((float)width,(float)extent);shadowSurface.SourceSize=shadowShape.Size;Place(extent);var size=new Vector2((float)width,(float)(height+24));float opacity=height>55?.16f:0;shadowFrom=shadowStart;shadowTo=opacity;if(!resizing){SyncGlassBounds(extent,from,height,false);clipGeometry.Size=shadowGeometry.Size=size;shadow.Opacity=opacity;currentHeight=height;if(route==""){body.Visibility=Visibility.Collapsed;ReleasePage();}if(hidden)AnimateHandleClip();Place(height);return;}
  var motion=compositor.CreateVector2KeyFrameAnimation();motion.InsertKeyFrame(0,new Vector2((float)width,(float)(from+24)));motion.InsertKeyFrame(1,size,compositor.CreateCubicBezierEasingFunction(new Vector2(1f/3,1),new Vector2(2f/3,1)));motion.Duration=TimeSpan.FromMilliseconds(height>from?340:280);clipGeometry.Size=shadowGeometry.Size=size;clipGeometry.StartAnimation("Size",motion);shadowGeometry.StartAnimation("Size",motion);SyncGlassBounds(extent,from,height,true);var fade=compositor.CreateScalarKeyFrameAnimation();fade.InsertKeyFrame(0,(float)shadowStart);fade.InsertKeyFrame(1,opacity,compositor.CreateCubicBezierEasingFunction(new Vector2(1f/3,1),new Vector2(2f/3,1)));fade.Duration=motion.Duration;shadow.Opacity=opacity;shadow.StartAnimation("Opacity",fade);currentHeight=height;await Task.Delay(motion.Duration);if(disposed||version!=resizeVersion)return;clipGeometry.StopAnimation("Size");shadowGeometry.StopAnimation("Size");shadow.StopAnimation("Opacity");clipGeometry.Size=shadowGeometry.Size=size;shadow.Opacity=opacity;surface.Height=height;SyncGlassBounds(height,height,height,false);UpdateShoulders();resizing=false;if(route==""){body.Visibility=Visibility.Collapsed;ReleasePage();}if(hidden)AnimateHandleClip();Place(height);
 }
 void AnimateContent(FrameworkElement element,int direction=1){if(!Motion||!ready)return;ElementCompositionPreview.SetIsTranslationEnabled(element,true);var visual=ElementCompositionPreview.GetElementVisual(element);using var move=compositor.CreateVector3KeyFrameAnimation();using var easing=compositor.CreateCubicBezierEasingFunction(new Vector2(.15f,.8f),new Vector2(.2f,1));move.InsertKeyFrame(0,new Vector3(direction*12,0,0));move.InsertKeyFrame(1,Vector3.Zero,easing);move.Duration=TimeSpan.FromMilliseconds(240);visual.StartAnimation("Translation",move);using var fade=compositor.CreateScalarKeyFrameAnimation();fade.InsertKeyFrame(0,.1f);fade.InsertKeyFrame(1,1);fade.Duration=TimeSpan.FromMilliseconds(200);visual.StartAnimation("Opacity",fade);}
 void SetHovered(bool value)
 {
  if(value&&hidden&&pointerAtHide is WindowHost.Point previous&&
     WindowHost.GetCursorPos(out var current)&&current.X==previous.X&&current.Y==previous.Y)return;
  if(hovered==value)return;
  hovered=value;
  if(value&&hidden)SetHidden(false);
  UpdateClock();
  if(!value&&coveringWindow!=IntPtr.Zero&&route=="")SetHidden(true);
 }
 bool? clockShown;
 void UpdateClock(){if(compositor==null)return;bool show=prefs.ShowClock&&(hovered||route!="");string time=DateTime.Now.ToString("HH:mm");if(clock.Text!=time)clock.Text=time;if(clockShown!=show){clockShown=show;TransitionClock(show);}SyncPeakSampling();}
 void DrawPeak(){perfWaveTicks++;double now=frameWatch.Elapsed.TotalSeconds,dt=Math.Min(.1,now-lastPeakTime);lastPeakTime=now;double raw=media.Playing&&!audio.Muted&&audio.Volume>.001?audio.Peak():0;if(perfForceWave)raw=.5+.5*Math.Sin(now*8);double peak=NormalizePeak(raw);DrawLevels(now,dt,peak);var interval=TimeSpan.FromMilliseconds(peak>0?1000.0/Math.Min(prefs.FrameLimit,monitor.Hz):50);if(peakTimer.Interval!=interval)peakTimer.Interval=interval;}
 const int CompactHeight=54,HiddenOffset=-46;
 void SetHidden(bool value){
  if(hidden==value)return;
  hidden=value;
  if(value)hovered=false;
  pointerAtHide=value&&WindowHost.GetCursorPos(out var cursor)?cursor:null;
  if(value&&route!="")Navigate("");
  else if(Motion)AnimateHandleClip();
  AnimatePosition(value?HiddenOffset:0);
  UpdateClock();
 }
 int positionVersion;double positionOffset;
 void SettleEdgePosition(){
  ++positionVersion;
  positionOffset=hidden?HiddenOffset:0;
  if(route=="")AnimateHandleClip();
  Place(surface.Height);
 }
 async void AnimatePosition(double offset){
  if(!Motion){SettleEdgePosition();return;}
  int version=++positionVersion;
  double from=positionOffset;
  var watch=Stopwatch.StartNew();
  while(!disposed&&version==positionVersion&&watch.ElapsedMilliseconds<280){
   double t=watch.Elapsed.TotalMilliseconds/280;
   positionOffset=from+(offset-from)*(1-Math.Pow(1-t,3));
   Place(surface.Height);
   await Task.Delay(Math.Max(8,1000/Math.Min(prefs.FrameLimit,monitor.Hz)));
  }
  if(!disposed&&version==positionVersion){
   positionOffset=offset;
   Place(surface.Height);
  }
 }
}















