// GPL-3.0-or-later. All visible controls and text use WinUI 3.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ada;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.UI;
using VirtualKey=Windows.System.VirtualKey;
using static Veilix.UiLanguage;
namespace Veilix;
public sealed partial class MainWindow:Window {
 Microsoft.UI.Xaml.Media.ImageSource headerArtwork;bool volumeFeedback;double pendingVolume;Marquee headerMarquee;Microsoft.UI.Dispatching.DispatcherQueueTimer saveDebounce;long lastUtilitySecond=-1;readonly Settings prefs=Settings.Load();readonly UtilitySession tools;readonly MediaService media=new();readonly AudioService audio=new();readonly BrightnessService brightness=new();readonly WeatherService weather=new();
 readonly Grid root=new(),scene=new(),header=new(),body=new();readonly Border surface=new();readonly StackPanel page=new(){Spacing=12};readonly ScrollViewer scroller=new(){VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
 readonly TextBlock clock=new(),date=new(),weatherText=new(),miniTitle=new(),miniArtist=new(),pageTitle=new();readonly Grid clockArea=new(),mediaArea=new(),batteryArea=new();readonly Image miniArt=new(){Stretch=Stretch.UniformToFill};readonly Border miniCover=new();readonly StackPanel waves=new(){Orientation=Orientation.Horizontal,Spacing=4,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};readonly List<Microsoft.UI.Xaml.Shapes.Rectangle> bars=new();
 readonly List<Display> displays=Native.Displays();Display monitor;readonly WindowHost host;readonly BluetoothMonitor bluetooth;readonly DispatcherQueueTimer tick,peakTimer,occlusionTimer,noticeTimer;CultureInfo tr=>Culture;IntPtr coveringWindow;WindowHost.Point? pointerAtHide;
 CompositionGeometricClip bodyMaterialClip;readonly Compositor compositor;readonly CompositionRoundedRectangleGeometry clipGeometry,shadowGeometry;readonly DropShadow shadow;readonly SpriteVisual shadowSprite;readonly ShapeVisual shadowShape;readonly CompositionVisualSurface shadowSurface;
 SystemBackdropElement glass;Border tint,weatherIconPlate;string route="",settingsTab="Görünüm",timeTab="Pomodoro",taskFilter="Açık",lastTrack="";double width=500,currentHeight=54,targetHeight=54;bool hovered,hidden,disposed,popupOpen,ready;int resizeVersion,artVersion;Ada.Artwork currentBitmap;Microsoft.UI.Xaml.Media.ImageSource art;TextBlock timeValue,waterValue,waterGoalValue,positionLabel;Slider progress,volume;Button playButton;UtilityNotice notice;DateTime lastForecast=DateTime.MinValue;double[] peakLevels=new double[9];Stopwatch frameWatch=Stopwatch.StartNew();double lastPeakTime;long lastNavigation;
 static readonly string[] Routes={"media","water","controls","bluetooth","time","tasks","shelf","clipboard"};static readonly string[] Names={"Oynatıcı","Su takibi","Hızlı ayarlar","Bluetooth","Zamanlayıcılar","Görevler","Dosya rafı","Pano geçmişi"};
 internal bool Motion=>!prefs.ReducedMotion;
 public MainWindow(){
  Title="Veilix";if(testing)prefs.Language="tr";ApplyLanguage();UiFaultBoundary.Snapshot=()=>$"route={route}; material={prefs.Material}; hidden={hidden}; resizing={resizing}; clockSwitching={clockSwitching}; disposed={disposed}";UiFaultBoundary.Recovered=InvalidateUiAfterFault;tools=new UtilitySession(testing);if(tools.PruneOldNotices()&&!testing)tools.Save();tools.NormalizeNotices();monitor=displays.FirstOrDefault(d=>d.Id==prefs.DisplayId)??displays.FirstOrDefault(d=>!d.Primary)??displays[0];width=BodyWidth();root.RequestedTheme=ElementTheme.Dark;Content=root;SystemBackdrop=new WinUIEx.TransparentTintBackdrop(Microsoft.UI.Colors.Transparent);
  host=new WindowHost(this,()=>EnqueueUi(()=>{if(!PopupActive&&!benchmarking)Navigate("");}),()=>EnqueueUi(()=>bluetooth?.Schedule()),()=>EnqueueUi(DisplayChanged));host.ClipboardChanged=()=>EnqueueUi(()=>_ =RecordClipboard());host.Width=()=>width;host.Height=VisibleHeight;host.PopupOpen=()=>PopupActive;host.Narrow=()=>hidden;host.PositionChanged=()=>UiFaultBoundary.Run(()=>unifiedGlass?.Sync(),"glass position");host.HoverChanged=value=>{if(!testing&&!benchmarking)UiFaultBoundary.Run(()=>SetHovered(value),"native pointer");};
  root.Children.Add(scene);scene.HorizontalAlignment=HorizontalAlignment.Left;scene.VerticalAlignment=VerticalAlignment.Top;scene.Margin=new Thickness(20,0,20,20);scene.Width=width;surface.HorizontalAlignment=HorizontalAlignment.Left;surface.VerticalAlignment=VerticalAlignment.Top;surface.Width=width;surface.Height=54;surface.CornerRadius=new CornerRadius(0,0,24,24);scene.Children.Add(surface);
  var layers=new Grid();surface.Child=layers;glass=new SystemBackdropElement{CornerRadius=surface.CornerRadius,IsHitTestVisible=false};layers.Children.Add(glass);tint=new Border{CornerRadius=surface.CornerRadius,IsHitTestVisible=false};layers.Children.Add(tint);var layout=new Grid();layout.RowDefinitions.Add(new RowDefinition{Height=new GridLength(54)});layout.RowDefinitions.Add(new RowDefinition());layers.Children.Add(layout);layout.Children.Add(header);Grid.SetRow(body,1);layout.Children.Add(body);body.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});body.RowDefinitions.Add(new RowDefinition());BuildHeader();layers.Children.Add(edgeGrip);BuildNavigation();scroller.Content=page;scroller.Padding=new Thickness(18,8,18,20);Grid.SetRow(scroller,1);body.Children.Add(scroller);body.Visibility=Visibility.Collapsed;
  compositor=ElementCompositionPreview.GetElementVisual(root).Compositor;ElementCompositionPreview.GetElementVisual(root).BorderMode=CompositionBorderMode.Soft;ElementCompositionPreview.GetElementVisual(surface).BorderMode=CompositionBorderMode.Soft;clipGeometry=compositor.CreateRoundedRectangleGeometry();clipGeometry.CornerRadius=new Vector2(24);clipGeometry.Offset=new Vector2(0,-24);clipGeometry.Size=new Vector2((float)width,78);bodyMaterialClip=compositor.CreateGeometricClip(clipGeometry);ElementCompositionPreview.GetElementVisual(surface).Clip=bodyMaterialClip;glass.Loaded+=(_,e)=>ClipBodyBackdrop();glass.LayoutUpdated+=(_,e)=>ClipBodyBackdrop();
  shadowGeometry=compositor.CreateRoundedRectangleGeometry();shadowGeometry.CornerRadius=new Vector2(24);shadowGeometry.Offset=new Vector2(0,-24);shadowGeometry.Size=clipGeometry.Size;var mask=compositor.CreateSpriteShape(shadowGeometry);mask.FillBrush=compositor.CreateColorBrush(Microsoft.UI.Colors.White);shadowShape=compositor.CreateShapeVisual();shadowShape.Size=new Vector2((float)width,54);shadowShape.Shapes.Add(mask);shadowSurface=compositor.CreateVisualSurface();shadowSurface.SourceVisual=shadowShape;shadowSurface.SourceSize=shadowShape.Size;shadow=compositor.CreateDropShadow();shadow.Color=Microsoft.UI.Colors.Black;shadow.BlurRadius=10;shadow.Offset=new Vector3(0,3,0);shadow.Opacity=0;shadow.Mask=compositor.CreateSurfaceBrush(shadowSurface);shadowSprite=compositor.CreateSpriteVisual();shadowSprite.Size=shadowShape.Size;shadowSprite.Shadow=shadow;var shadowHost=new Grid{IsHitTestVisible=false};scene.Children.Insert(0,shadowHost);ElementCompositionPreview.SetElementChildVisual(shadowHost,shadowSprite);
  BuildShoulders();tick=Timer(1000,Tick);peakTimer=Timer(1000.0/Math.Min(prefs.FrameLimit,monitor.Hz),DrawPeak);occlusionTimer=Timer(500,CheckOcclusion);noticeTimer=Timer(12000,()=>{noticeTimer.Stop();notice=null;UpdateHeader();});bluetooth=new BluetoothMonitor(DispatcherQueue);bluetooth.Changed+=UpdateBattery;bluetooth.Changed+=BluetoothDevicesChanged;media.Changed+=OnMediaChanged;weather.Changed+=()=>EnqueueUi(()=>{UpdateHeader();if(route=="weather")Render();});audio.Changed+=(v,m)=>EnqueueUi(()=>{if(volume!=null&&Math.Abs(volume.Value-v)>.6){volumeFeedback=true;volume.Value=v;volumeFeedback=false;}if(tools.Data.SystemNotices)ShowNotice(new UtilityNotice{Kind="system-volume",Title=(m?"Sessiz":"Ses · %"+Math.Round(v)),Icon="volume",Value=v});});audio.DeviceChanged+=()=>EnqueueUi(()=>audio.Refresh());
  root.PointerEntered+=(_,e)=>{if(!testing&&!benchmarking)SetHovered(host.ContainsPointer());};root.PointerExited+=(_,e)=>{if(!testing&&!benchmarking)SetHovered(host.ContainsPointer());};root.KeyDown+=KeyDown;root.AllowDrop=true;root.DragOver+=ShelfDrag;root.Drop+=ShelfDrop;Closed+=(_,e)=>Dispose();root.Loaded+=async (_,e)=>{if(ready)return;ready=true;ApplyTheme();Place(54);if(testing){await RunChecks();return;}StartClipboardHistory();audio.Refresh();audio.StartNotifications();bluetooth.Start();tick.Start();occlusionTimer.Start();host.WatchOcclusion(OnOcclusionEvent);StartSystemNotices();_ =RefreshBrightness();if(Environment.GetCommandLineArgs().Contains("--bluetooth"))Navigate("bluetooth");media.SetOtherSources(prefs.OtherMediaSources);await media.Start();await weather.Refresh(prefs);lastForecast=DateTime.UtcNow;UpdateHeader();if(Environment.GetCommandLineArgs().Contains("--benchmark"))await RunBenchmark();};ApplyTheme();UpdateHeader();Place(54);if(!testing)InitTray();
 }
 DispatcherQueueTimer Timer(double ms,Action action,[System.Runtime.CompilerServices.CallerArgumentExpression(nameof(action))]string source=null){var t=DispatcherQueue.CreateTimer();t.Interval=TimeSpan.FromMilliseconds(ms);long retryAfter=0;t.Tick+=(_,e)=>{if(disposed||Environment.TickCount64<retryAfter)return;if(!UiFaultBoundary.Run(action,"timer: "+source))retryAfter=Environment.TickCount64+250;};return t;}
 double TargetScale=>monitor.Dpi/96.0;
 double BodyWidth()=>Math.Min(Math.Round(500*prefs.Scale),Math.Max(1,Math.Floor(monitor.Bounds.Width/TargetScale)-40));
 void DisplayChanged(){if(disposed)return;var connected=Native.Displays();if(connected.Count==0)return;displays.Clear();displays.AddRange(connected);monitor=displays.FirstOrDefault(d=>d.Id==prefs.DisplayId)??displays.FirstOrDefault(d=>!d.Primary)??displays[0];width=BodyWidth();scene.Width=surface.Width=width;UpdateShoulders();peakTimer.Interval=TimeSpan.FromMilliseconds(1000.0/Math.Min(prefs.FrameLimit,monitor.Hz));Render();Place(surface.Height);}
 void Tick(){long second=DateTimeOffset.UtcNow.UtcTicks/TimeSpan.TicksPerSecond;if(lastUtilitySecond!=second){lastUtilitySecond=second;if(tools.PruneOldNotices()){UpdateNotificationBell();SaveNotices();}UpdateHeader();PollSystemNotices();foreach(var n in tools.Tick())ShowNotice(n);}media.RefreshTimeline();if(progress!=null&&progress.FocusState!=FocusState.Pointer)progress.Value=media.Duration.TotalSeconds>0?media.Position.TotalSeconds/media.Duration.TotalSeconds*100:0;if(positionLabel!=null)positionLabel.Text=UtilitySession.Format(media.Position.TotalSeconds)+"  /  "+UtilitySession.Format(media.Duration.TotalSeconds);UpdateTime();if(DateTime.UtcNow-lastForecast>TimeSpan.FromMinutes(15)){lastForecast=DateTime.UtcNow;_ =weather.Refresh(prefs);}}
 void ShowNotice(UtilityNotice value){if(disposed||!tools.NoticeEnabled(value.Kind))return;tools.RecordNotice(value);SaveNotices();UpdateNotificationBell();if(value.Sound&&!testing)System.Media.SystemSounds.Asterisk.Play();if(!tools.Data.NoticeBanners||(notice?.Kind=="water"&&value.Kind.StartsWith("system")))return;notice=value;noticeTimer.Interval=TimeSpan.FromSeconds(value.Kind=="water"?12:4);noticeTimer.Stop();noticeTimer.Start();UpdateHeader();if(hidden)tray?.ShowBalloonTip(5000,Status(value.Title),NoticeBalloonText(value),System.Windows.Forms.ToolTipIcon.Info);}
 void UpdateHeader(){date.Text=DateTime.Now.ToString(English?"ddd, MMM d":"dd MMM ddd",tr);weatherText.Text=prefs.ShowWeather?L(weather.Summary):"";miniTitle.Text=notice!=null?Status(notice.Title):media.Playing?media.Title:"";miniArtist.Text=notice!=null?NoticeDetail(notice.Kind,notice.Detail)??"":media.Playing?media.Artist:"";miniCover.Opacity=notice==null&&media.Playing&&!String.IsNullOrEmpty(media.Title)?1:0;miniCover.Child=null;if(headerArtwork!=art){headerArtwork=art;miniCover.Background=art!=null?new ImageBrush{ImageSource=art,Stretch=Stretch.UniformToFill}:Tile();}UpdateClock();headerMarquee?.Sync();}
 async void OnMediaChanged(){if(disposed)return;string track=media.Title+"\n"+media.Artist;bool changed=lastTrack!=track;lastTrack=track;if(media.Cover!=currentBitmap){currentBitmap=media.Cover;int version=++artVersion;var bitmap=currentBitmap;if(bitmap!=null){byte[] bytes=bitmap.Png;var image=new BitmapImage{DecodePixelWidth=512,DecodePixelType=DecodePixelType.Physical};using var random=new Windows.Storage.Streams.InMemoryRandomAccessStream();using(var writer=new Windows.Storage.Streams.DataWriter(random.GetOutputStreamAt(0))){writer.WriteBytes(bytes);await writer.StoreAsync();}random.Seek(0);await image.SetSourceAsync(random);if(disposed||version!=artVersion)return;art=image;}else art=null;}UpdateHeader();if(changed){AnimateContent(mediaArea);if(route=="media")Render();}else if(route=="media"&&playButton!=null){UpdatePlaybackButton();}}
 string batterySignature="";void UpdateBattery(){string signature=String.Join("|",bluetooth.Devices.Select(x=>x.Id+":"+x.Connected+":"+x.Percent+":"+x.Name));if(signature==batterySignature)return;batterySignature=signature;if(route=="controls")Render();batteryArea.Children.Clear();var device=bluetooth.Devices.Where(x=>x.Connected&&x.Percent.HasValue).OrderBy(x=>x.Percent).FirstOrDefault();if(device==null)return;var canvas=new Grid{Width=28,Height=28};canvas.Children.Add(new Microsoft.UI.Xaml.Shapes.Ellipse{Stroke=B("#405C6C7C"),StrokeThickness=3.5});var path=new Microsoft.UI.Xaml.Shapes.Path{Stroke=B(GradientModel.BatteryColor(device.Percent.Value)),StrokeThickness=3.5,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round};double f=Math.Clamp(device.Percent.Value/100.0,0,.9999),a=f*2*Math.PI;var geometry=new PathGeometry();var figure=new PathFigure{StartPoint=new Point(14,1.75),IsClosed=false};figure.Segments.Add(new ArcSegment{Point=new Point(14+12.25*Math.Sin(a),14-12.25*Math.Cos(a)),Size=new Size(12.25,12.25),SweepDirection=SweepDirection.Clockwise,IsLargeArc=f>.5});geometry.Figures.Add(figure);path.Data=geometry;canvas.Children.Add(path);var button=new Button{Content=canvas,Padding=new Thickness(0),Background=B("#00000000"),BorderThickness=new Thickness(0)};ToolTipService.SetToolTip(button,string.Join("\n",bluetooth.Devices.Select(x=>x.Name+" · "+(x.Percent.HasValue?x.Percent.Value.ToString(tr)+"%":L("Pil bilgisi yok")))));button.Click+=(_,e)=>Navigate("bluetooth");batteryArea.Children.Add(button);}
 bool occlusionCheckQueued;
 bool WatchForOverlap=>prefs.AutoHideOverlap||prefs.HideFullscreen;
 Native.Rect IslandBounds()
 {
  double scale=TargetScale;
  int outerWidth=(int)Math.Ceiling((width+40)*scale);
  int left=monitor.Bounds.Left+(monitor.Bounds.Width-outerWidth)/2;
  return new Native.Rect
  {
   Left=left+(int)Math.Floor(20*scale),Top=monitor.Bounds.Top,
   Right=left+(int)Math.Ceiling((width+20)*scale),
   Bottom=monitor.Bounds.Top+(int)Math.Ceiling(54*scale)
  };
 }
 bool AllowsHide(Native.Rect frame)
 {
  var screen=monitor.Bounds;
  bool full=frame.Left<=screen.Left+2&&frame.Top<=screen.Top+2&&frame.Right>=screen.Right-2&&frame.Bottom>=screen.Bottom-2;
  return full?prefs.HideFullscreen:prefs.AutoHideOverlap;
 }
 void OnOcclusionEvent(IntPtr hwnd,uint kind)
 {
  if(disposed||benchmarking||occlusionCheckQueued||!WatchForOverlap)return;
  if(kind!=3&&hwnd!=coveringWindow&&
     (!Native.GetWindowRect(hwnd,out var frame)||!Native.Intersects(IslandBounds(),frame)))return;
  occlusionCheckQueued=true;
  if(!DispatcherQueue.TryEnqueue(()=>
  {
   occlusionCheckQueued=false;
   UiFaultBoundary.Run(CheckOcclusion,"occlusion event");
  }))occlusionCheckQueued=false;
 }
 void CheckOcclusion()
 {
  if(disposed||benchmarking)return;
  host.EnsureTopmost();
  unifiedGlass?.Sync();
  if(PopupActive)return;
  IntPtr previous=coveringWindow;
  coveringWindow=WatchForOverlap?Native.FindCoveringWindow(host.Hwnd,IslandBounds(),AllowsHide):IntPtr.Zero;
  if(coveringWindow!=IntPtr.Zero)
  {
   // Recheck the pointer because another window can prevent XAML's leave event.
   if(previous!=coveringWindow||(route==""&&!host.ContainsPointer()))SetHidden(true);
  }
  else if(!prefs.KeepHiddenAfterOverlap||!WatchForOverlap)SetHidden(false);
 }
 bool PopupActive=>popupOpen||sectionMenuOpen;
 void KeyDown(object sender,KeyRoutedEventArgs e){if(sectionMenuOpen)return;if(e.Key==VirtualKey.Escape){Navigate("");e.Handled=true;}else if(e.Key==VirtualKey.Left||e.Key==VirtualKey.Right){if(FindAncestor<TextBox>(e.OriginalSource as DependencyObject)==null&&FindAncestor<Slider>(e.OriginalSource as DependencyObject)==null){MovePage(e.Key==VirtualKey.Left?-1:1);e.Handled=true;}}}
 void Dispose(){if(disposed)return;disposed=true;UiFaultBoundary.Snapshot=null;UiFaultBoundary.Recovered=null;tick.Stop();peakTimer.Stop();occlusionTimer.Stop();noticeTimer.Stop();noticeSave?.Stop();DisposeClockTransition();StopSystemNotices();saveDebounce?.Stop();volumeWrite?.Stop();foreach(var t in brightnessWrites.Values)t.Stop();unifiedGlass?.Dispose();host.Dispose();bluetoothLifetime.Cancel();bluetoothLifetime.Dispose();bluetoothConnector.Dispose();bluetooth.Dispose();media.Dispose();audio.Dispose();brightness.Dispose();weather.Dispose();if(!testing){tools.Save();prefs.Save();}tray?.Dispose();DisposeShoulders();shadowSurface.Dispose();shadowShape.Dispose();shadowSprite.Dispose();shadow.Dispose();}
 void Save(){if(testing)return;saveDebounce??=Timer(300,()=>{saveDebounce.Stop();prefs.Save();});saveDebounce.Stop();saveDebounce.Start();}
 System.Windows.Forms.NotifyIcon tray;
 void InitTray(){tray=new System.Windows.Forms.NotifyIcon{Text="Veilix",Icon=System.Drawing.Icon.ExtractAssociatedIcon(Path.Combine(AppContext.BaseDirectory,"Veilix.exe")),Visible=true};UpdateTrayMenu();tray.DoubleClick+=(_,e)=>EnqueueUi(()=>Navigate("media"));}
 void UpdateTrayMenu(){var previous=tray.ContextMenuStrip;var menu=new System.Windows.Forms.ContextMenuStrip();foreach(var entry in new[]{new[]{Names[0],"media"},new[]{"Bildirimler","notifications"},new[]{Names[2],"controls"},new[]{"Bluetooth","bluetooth"},new[]{Names[7],"clipboard"},new[]{"Ayarlar","settings"}}){string r=entry[1];menu.Items.Add(L(entry[0]),null,(_,e)=>EnqueueUi(()=>Navigate(r)));}menu.Items.Add(L("Çıkış"),null,(_,e)=>EnqueueUi(()=>Close()));tray.ContextMenuStrip=menu;previous?.Dispose();}
}















