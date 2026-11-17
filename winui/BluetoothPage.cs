using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ada;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Veilix.UiLanguage;
namespace Veilix;
public sealed partial class MainWindow {
 readonly BluetoothConnector bluetoothConnector=new();
 string connectingBluetooth,bluetoothMessage="",bluetoothPageSignature="";bool bluetoothError,disconnectingBluetooth;
 void BluetoothDevicesChanged(){
  if(connectingBluetooth==null)bluetoothConnector.ReleaseDisconnected(bluetooth.Paired);
  string signature=String.Join("|",bluetooth.Paired.Select(d=>$"{d.Id}:{d.Name}:{d.Connected}:{d.Battery}"));
  if(signature==bluetoothPageSignature)return;bluetoothPageSignature=signature;
  if(route=="bluetooth")Render(refresh:true);
 }
 void RenderBluetooth(System.Collections.Generic.IEnumerable<BluetoothEntry> preview=null){
  Heading(L("Bluetooth cihazları"));
  if(!String.IsNullOrEmpty(bluetoothMessage)){var message=Text(bluetoothMessage,13);if(bluetoothError)message.Foreground=IconBrush("error");page.Children.Add(Card(message));}
  var devices=preview?.ToList()??bluetooth.Paired;
  if(devices.Count==0){var empty=new StackPanel{Spacing=12,Padding=new Thickness(12,24,12,24),HorizontalAlignment=HorizontalAlignment.Center};empty.Children.Add(Icons.Get("bluetooth",36,IconBrush("bluetooth")));empty.Children.Add(Text(L("Henüz kayıtlı cihaz görünmüyor."),14,true));empty.Children.Add(Text(L("Cihazını Windows'ta eşleştirdikten sonra burada bulabilirsin."),13,false,true));page.Children.Add(Card(empty));}
  foreach(var device in devices){
   bool connected=device.Connected||bluetoothConnector.HasActiveSession(device);
   var row=new Grid{ColumnSpacing=12,MinHeight=48};row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
   bool busy=connectingBluetooth==device.Key;
   var plate=new Border{Width=42,Height=42,CornerRadius=new CornerRadius(13),Background=B(ThemePalette.Get(prefs).Light?"#10007AFF":"#1F0A84FF"),Child=Icons.Get(device.Icon,23,connected?IconBrush("connected"):IconBrush(device.Icon))};row.Children.Add(plate);
   var info=new StackPanel{Spacing=3,VerticalAlignment=VerticalAlignment.Center};info.Children.Add(Text(device.Name,14,true));var state=Text(busy?(disconnectingBluetooth?L("Bağlantı kesiliyor…"):L("Bağlanıyor…")):connected?L("Bağlı")+(device.Battery.HasValue?$" · {device.Battery}%":""):L("Bağlı değil"),12,false,true);if(connected&&!busy)state.Foreground=IconBrush("connected");info.Children.Add(state);Grid.SetColumn(info,1);row.Children.Add(info);
   if(busy){var spinner=new ProgressRing{Width=22,Height=22,IsActive=true,Foreground=IconBrush("bluetooth")};Grid.SetColumn(spinner,2);row.Children.Add(spinner);}
   else{var action=Button(connected?L("Bağlantıyı kes"):L("Bağlan"),()=>_ =SetBluetoothConnection(device,!connected));action.IsEnabled=connectingBluetooth==null;action.VerticalAlignment=VerticalAlignment.Center;action.MinWidth=76;Grid.SetColumn(action,2);row.Children.Add(action);}
   page.Children.Add(Card(row));
  }
  var windows=Button(L("Yeni cihaz eşleştir"),()=>_ =Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:bluetooth")),"add");page.Children.Add(windows);
 }
 internal static async Task<bool> WaitForBluetooth(Func<bool> connected,Func<Task> poll,CancellationToken cancel,int attempts=24){for(int i=0;i<attempts;i++){cancel.ThrowIfCancellationRequested();if(connected())return true;await poll();}return connected();}
 internal static async Task<bool> WaitForBluetoothUpdate(Func<bool> matches,Action<Action> subscribe,Action<Action> unsubscribe,TimeSpan timeout,CancellationToken cancel){
  cancel.ThrowIfCancellationRequested();
  var signal=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
  void Observe(){if(matches())signal.TrySetResult();}
  subscribe(Observe);
  try{
   Observe();
   await signal.Task.WaitAsync(timeout,cancel);
   return true;
  }catch(TimeoutException){return matches();}
  finally{unsubscribe(Observe);}
 }
 readonly CancellationTokenSource bluetoothLifetime=new();
 async Task SetBluetoothConnection(BluetoothEntry device,bool connect){
  if(disposed||testing||connectingBluetooth!=null)return;
  connectingBluetooth=device.Key;disconnectingBluetooth=!connect;bluetoothMessage="";bluetoothError=false;Render(refresh:true);
  var timing=System.Diagnostics.Stopwatch.StartNew();
  try{
   string method=connect?await bluetoothConnector.Request(device):await bluetoothConnector.Disconnect(device);
   long nativeMs=timing.ElapsedMilliseconds;
   var token=bluetoothLifetime.Token;
   bool confirmed;
   if(device.LowEnergy){
    confirmed=connect?bluetoothConnector.HasActiveSession(device):await WaitForBluetoothUpdate(
     ()=>!bluetooth.Paired.Any(d=>d.Key.Equals(device.Key,StringComparison.OrdinalIgnoreCase)&&d.Connected),
     callback=>bluetooth.Changed+=callback,callback=>bluetooth.Changed-=callback,BluetoothConnector.ConfirmationTimeout,token);
   }else{
    Task<bool> ReadState()=>Task.Run(()=>BluetoothClassic.Connected(device.Address),token);
    bool current=await ReadState();
    int polls=0;
    async Task Poll(){await Task.Delay(polls++<6?150:500,token);current=await ReadState();}
    confirmed=await WaitForBluetooth(()=>current==connect,Poll,token,29);
   }
   if(!disposed)Program.Trace($"Bluetooth {(connect?"connect":"disconnect")}: method={method}; native={nativeMs} ms; total={timing.ElapsedMilliseconds} ms; confirmed={confirmed}");
   if(disposed)return;
   bluetoothError=!confirmed;bluetoothMessage=confirmed?(connect?Format("{0} bağlandı.",device.Name):Format("{0} bağlantısı kesildi.",device.Name)):(connect?L("Bağlantı doğrulanamadı. Cihazı açıp yeniden deneyebilirsin."):L("Windows bağlantıyı açık tutuyor. Windows Bluetooth ayarlarından kesebilirsin."));
  }catch(OperationCanceledException){return;}catch(Exception ex){if(disposed)return;UiFaultBoundary.Report(ex,connect?"bluetooth connect":"bluetooth disconnect",false);bluetoothError=true;bluetoothMessage=ex is System.ComponentModel.Win32Exception native?Format("Windows işlemi tamamlayamadı (kod: {0:X}). Cihazın durumunu kontrol edip tekrar deneyebilirsin.",unchecked((uint)native.NativeErrorCode)):ex.Message;}
  finally{connectingBluetooth=null;disconnectingBluetooth=false;if(!disposed){bluetooth.Schedule();bluetoothConnector.ReleaseDisconnected(bluetooth.Paired);if(route=="bluetooth")Render(refresh:true);}}
 }
}
