using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using static Veilix.UiLanguage;
namespace Veilix;

internal sealed class BluetoothConnector:IDisposable {
 readonly Dictionary<string,GattSession> sessions=new(StringComparer.OrdinalIgnoreCase);
 readonly CancellationTokenSource lifetime=new();
 internal static readonly TimeSpan ConfirmationTimeout=TimeSpan.FromSeconds(12);
 internal bool HasActiveSession(BluetoothEntry entry)=>sessions.TryGetValue(entry.Key,out var session)&&session.SessionStatus==GattSessionStatus.Active;
 internal async Task<string> Disconnect(BluetoothEntry entry){
  if(!entry.LowEnergy)return await RequestClassic(entry,false);
  if(!sessions.Remove(entry.Key,out var session))throw new InvalidOperationException(L("Bu BLE bağlantısını Windows veya başka bir uygulama kullanıyor. Windows Bluetooth ayarlarından kesebilirsin."));
  session.MaintainConnection=false;session.Dispose();return "gatt";
 }
 internal async Task<string> Request(BluetoothEntry entry){
  if(!entry.LowEnergy)return await RequestClassic(entry,true);
  // Only a user-initiated connection owns a GATT session. Enumeration never opens one.
  GattSession session=null;
  using var timeout=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
  timeout.CancelAfter(ConfirmationTimeout);
  try{
   using var device=await BluetoothLEDevice.FromIdAsync(entry.Id).AsTask(timeout.Token);
   if(device==null)throw new InvalidOperationException(L("Cihaza erişilemiyor. Açık ve yakında olduğundan emin ol."));
   session=await GattSession.FromDeviceIdAsync(device.BluetoothDeviceId).AsTask(timeout.Token);
   if(session==null||!session.CanMaintainConnection)throw new InvalidOperationException(L("Bu cihazın bağlantısını Windows yönetiyor."));
   await ActivateSession(session,timeout.Token);
   lifetime.Token.ThrowIfCancellationRequested();
   if(sessions.Remove(entry.Key,out var previous))previous.Dispose();
   sessions[entry.Key]=session;session=null;
   return "gatt";
  }catch(OperationCanceledException)when(!lifetime.IsCancellationRequested&&timeout.IsCancellationRequested){
   throw new TimeoutException(L("Cihaz yanıt vermedi. Açık ve yakında olduğundan emin ol."));
  }finally{session?.Dispose();}
 }
 Task<string> RequestClassic(BluetoothEntry entry,bool connect)=>Task.Run(()=>{
  // A legacy disconnect uninstalled the profiles. Re-enable those first if
  // present, as an audio endpoint may not exist until they are restored.
  if(new BluetoothProfiles().Get(entry.Address).Length==0&&BluetoothAudio.TryRequest(entry,connect))return "audio-driver";
  if(connect)BluetoothClassic.Reconnect(entry.Address);else BluetoothClassic.Disconnect(entry.Address);
  return "classic-profiles";
 },lifetime.Token);
 static async Task ActivateSession(GattSession session,CancellationToken cancel){
  var active=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
  void StatusChanged(GattSession sender,GattSessionStatusChangedEventArgs args){
   if(args.Status==GattSessionStatus.Active)active.TrySetResult();
  }
  session.SessionStatusChanged+=StatusChanged;
  try{
   session.MaintainConnection=true;
   if(session.SessionStatus!=GattSessionStatus.Active)await active.Task.WaitAsync(cancel);
   if(session.SessionStatus!=GattSessionStatus.Active)throw new InvalidOperationException(L("Bluetooth bağlantısı kesildi. Yeniden deneyebilirsin."));
  }finally{session.SessionStatusChanged-=StatusChanged;}
 }
 internal void ReleaseDisconnected(IEnumerable<BluetoothEntry> entries){var paired=entries.Select(d=>d.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);foreach(var key in sessions.Keys.Where(k=>!paired.Contains(k)||sessions[k].SessionStatus!=GattSessionStatus.Active).ToArray()){sessions[key].Dispose();sessions.Remove(key);}}
 public void Dispose(){lifetime.Cancel();foreach(var session in sessions.Values)session.Dispose();sessions.Clear();lifetime.Dispose();}
}

// Windows exposes classic profile activation, rather than a generic device Connect API.
// Touch only profiles already installed on this paired device; always restore after disabling.
internal static class BluetoothClassic {
 [StructLayout(LayoutKind.Sequential)]struct SystemTime {public ushort Year,Month,DayOfWeek,Day,Hour,Minute,Second,Milliseconds;}
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]internal struct DeviceInfo {
  public uint Size;public ulong Address;public uint Class;
  [MarshalAs(UnmanagedType.Bool)]public bool Connected;
  [MarshalAs(UnmanagedType.Bool)]public bool Remembered;
  [MarshalAs(UnmanagedType.Bool)]public bool Authenticated;
  SystemTime Seen,Used;
  [MarshalAs(UnmanagedType.ByValTStr,SizeConst=248)]public string Name;
 }
 [DllImport("BluetoothAPIs.dll")]static extern uint BluetoothGetDeviceInfo(IntPtr radio,ref DeviceInfo device);
 [DllImport("BluetoothAPIs.dll")]static extern uint BluetoothEnumerateInstalledServices(IntPtr radio,ref DeviceInfo device,ref uint count,[Out]Guid[] services);
 [DllImport("BluetoothAPIs.dll")]static extern uint BluetoothSetServiceState(IntPtr radio,ref DeviceInfo device,ref Guid service,uint flags);
 [StructLayout(LayoutKind.Sequential)]struct SearchParams {
  public uint Size;
  [MarshalAs(UnmanagedType.Bool)]public bool Authenticated;
  [MarshalAs(UnmanagedType.Bool)]public bool Remembered;
  [MarshalAs(UnmanagedType.Bool)]public bool Unknown;
  [MarshalAs(UnmanagedType.Bool)]public bool Connected;
  [MarshalAs(UnmanagedType.Bool)]public bool Inquiry;
  public byte Timeout;public IntPtr Radio;
 }
 [DllImport("BluetoothAPIs.dll",SetLastError=true)]static extern IntPtr BluetoothFindFirstDevice(ref SearchParams search,ref DeviceInfo info);
 [DllImport("BluetoothAPIs.dll",SetLastError=true)]static extern bool BluetoothFindNextDevice(IntPtr search,ref DeviceInfo info);
 [DllImport("BluetoothAPIs.dll")]static extern bool BluetoothFindDeviceClose(IntPtr search);
 internal static List<BluetoothEntry> ReadPaired(){
  var rows=new List<BluetoothEntry>();var search=new SearchParams{Size=(uint)Marshal.SizeOf<SearchParams>(),Authenticated=true,Remembered=true,Connected=true,Inquiry=false};var info=new DeviceInfo{Size=(uint)Marshal.SizeOf<DeviceInfo>()};var handle=BluetoothFindFirstDevice(ref search,ref info);
  if(handle==IntPtr.Zero)return rows;
  try{do{if((info.Remembered||info.Authenticated)&&!String.IsNullOrWhiteSpace(info.Name))rows.Add(new("classic:"+info.Address.ToString("X12"),"",info.Name,info.Address,false,info.Connected));info.Size=(uint)Marshal.SizeOf<DeviceInfo>();}while(BluetoothFindNextDevice(handle,ref info));}
  finally{BluetoothFindDeviceClose(handle);}return rows;
 }
 internal static readonly Guid AudioSink=new("0000110b-0000-1000-8000-00805f9b34fb"),HandsFree=new("0000111e-0000-1000-8000-00805f9b34fb"),Hid=new("00001124-0000-1000-8000-00805f9b34fb");
 internal static Guid[] Supported(IEnumerable<Guid> services)=>services.Where(s=>s==AudioSink||s==HandsFree||s==Hid).Distinct().OrderBy(s=>s==AudioSink?0:s==HandsFree?1:2).ToArray();
 internal static void CycleProfile(Action disable,Action enable){try{disable();}finally{enable();}}
 static DeviceInfo Device(ulong address){
  if(address==0||address>0xFFFFFFFFFFFF)throw new InvalidOperationException(L("Bu cihaz için bağlantı adresi alınamadı."));
  var info=new DeviceInfo{Size=(uint)Marshal.SizeOf<DeviceInfo>(),Address=address};uint error=BluetoothGetDeviceInfo(IntPtr.Zero,ref info);if(error!=0)throw new Win32Exception((int)error);
  if(!info.Remembered&&!info.Authenticated)throw new InvalidOperationException(L("Önce bu cihazı Windows'ta eşleştir."));
  return info;
 }
 internal static bool Connected(ulong address)=>Device(address).Connected;
 static Guid[] Installed(ref DeviceInfo info){
  uint count=32;var services=new Guid[count];uint error=BluetoothEnumerateInstalledServices(IntPtr.Zero,ref info,ref count,services);
  if(error==234&&count<=256){services=new Guid[count];error=BluetoothEnumerateInstalledServices(IntPtr.Zero,ref info,ref count,services);}
  if(error!=0)throw new Win32Exception((int)error);
  return services.Take((int)count).Distinct().ToArray();
 }
 internal static void Disconnect(ulong address){
  var info=Device(address);if(!info.Connected)return;var store=new BluetoothProfiles();var profiles=Installed(ref info);if(profiles.Length==0)throw new InvalidOperationException(L("Windows bu cihaz için kesilebilir bir bağlantı profili sunmuyor."));
  // Persist BEFORE touching a driver, so closing Veilix never loses reconnect data.
  store.Set(address,store.Get(address).Concat(profiles));
  try{foreach(var profile in profiles){var id=profile;uint result=BluetoothSetServiceState(IntPtr.Zero,ref info,ref id,0);if(result!=0&&result!=0x80070057)throw new Win32Exception((int)result);}}
  catch{foreach(var profile in profiles){var id=profile;BluetoothSetServiceState(IntPtr.Zero,ref info,ref id,1);}throw;}
 }
 internal static void Reconnect(ulong address){
  var info=Device(address);var store=new BluetoothProfiles();var saved=store.Get(address);if(info.Connected&&saved.Length==0)return;
  var profiles=saved.Length>0?saved:Supported(Installed(ref info));if(profiles.Length==0)throw new InvalidOperationException(L("Bu cihaz için doğrudan bağlantı desteklenmiyor. Windows Bluetooth ayarlarını kullanabilirsin."));
  foreach(var profile in profiles){
   var id=profile;
   // Already enabled is E_INVALIDARG. Re-activate only that existing profile while offline.
   uint error=BluetoothSetServiceState(IntPtr.Zero,ref info,ref id,1);
   if(error==0x80070057&&saved.Length==0){if(BluetoothGetDeviceInfo(IntPtr.Zero,ref info)==0&&info.Connected)return;CycleProfile(()=>{uint result=BluetoothSetServiceState(IntPtr.Zero,ref info,ref id,0);if(result!=0)throw new Win32Exception((int)result);},()=>{uint result=BluetoothSetServiceState(IntPtr.Zero,ref info,ref id,1);if(result!=0)throw new Win32Exception((int)result);});}
   else if(error==0x80070057)continue;
   else if(error!=0)throw new Win32Exception((int)error);
   // Stop once Windows reports a link. Do not cycle another service on a connected device.
   if(saved.Length==0&&BluetoothGetDeviceInfo(IntPtr.Zero,ref info)==0&&info.Connected)return;
  }
  if(saved.Length>0)store.Set(address,Array.Empty<Guid>());
 }
}
