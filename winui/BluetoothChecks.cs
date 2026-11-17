using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Ada;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Devices.Enumeration;
namespace Veilix;
public sealed partial class MainWindow {
 async Task CheckBluetoothPanel(List<string> report,string folder){
  Check(Marshal.SizeOf<BluetoothClassic.DeviceInfo>()==560,"Bluetooth Win32 ABI layout");
  var audio=new BluetoothEntry("qa-audio","qa-container","AirPods Pro",0x112233445566,false,false);
  var container=Guid.NewGuid();
  var hardware=audio with{Container=container.ToString("B")};
  Check(BluetoothAudio.Matches(hardware,container,@"\\?\BTHENUM#dev_112233445566")&&
   BluetoothAudio.Matches(audio,Guid.Empty,@"\\?\BTHHFENUM#BthHFPAudio#8&0&112233445566_C00000000")&&
   !BluetoothAudio.Matches(hardware,Guid.NewGuid(),@"\\?\BTHENUM#dev_112233445566")&&
   !BluetoothAudio.Matches(hardware,container,@"\\?\USB#dev_112233445566")&&
   !BluetoothAudio.Matches(hardware with{LowEnergy=true},container,@"\\?\BTHENUM#dev_112233445566"),"Audio driver request must target only the selected classic device");
  var entries=PairedBluetoothDevices.Merge(new[]{audio,audio with{Id="qa-ble",LowEnergy=true,Connected=true,Battery=78},new BluetoothEntry("qa-keyboard","qa-keys","Keyboard",2,true,false),new BluetoothEntry("qa-speaker","qa-other","Salon hoparlörü",3,false,false)});
  Check(entries.Count==3&&entries[0].Connected&&entries[0].Id==audio.Id&&entries[0].Battery==78,"Bluetooth dual-mode deduplication/connected ordering");
  Check(audio.Key==(audio with{Container=""}).Key,"Native-to-AEP update changed Bluetooth connection identity");
  string profilesPath=Path.Combine(folder,"isolated-bluetooth-profiles.json");if(File.Exists(profilesPath))File.Delete(profilesPath);var profiles=new BluetoothProfiles(profilesPath);profiles.Set(audio.Address,new[]{BluetoothClassic.AudioSink,BluetoothClassic.HandsFree,BluetoothClassic.AudioSink});profiles.Set(9,new[]{BluetoothClassic.Hid});Check(new BluetoothProfiles(profilesPath).Get(audio.Address).Length==2&&profiles.Get(9).Length==1,"Disconnected profiles lost across restart or duplicated");profiles.Set(audio.Address,Array.Empty<Guid>());Check(profiles.Get(audio.Address).Length==0&&profiles.Get(9).Length==1,"Reconnect removed another device's profiles");
  Check(NoticeBalloonText(new UtilityNotice{Title="Ses · %50",Detail=""})=="Ses · %50"&&NoticeBalloonText(new UtilityNotice{Title=" ",Detail=null})=="Veilix bildirimi","Empty system notice can crash balloon notification");
  Check(BluetoothClassic.Supported(new[]{BluetoothClassic.AudioSink,Guid.NewGuid(),BluetoothClassic.AudioSink}).SequenceEqual(new[]{BluetoothClassic.AudioSink}),"Unknown or duplicate Bluetooth profile selected");
  var calls=new List<string>();BluetoothClassic.CycleProfile(()=>calls.Add("disable"),()=>calls.Add("enable"));Check(calls.SequenceEqual(new[]{"disable","enable"}),"Bluetooth profile order");calls.Clear();try{BluetoothClassic.CycleProfile(()=>{calls.Add("disable");throw new InvalidOperationException("QA only");},()=>calls.Add("enable"));}catch(InvalidOperationException){}Check(calls.SequenceEqual(new[]{"disable","enable"}),"Profile not restored on failure");
  bool rejected=false;try{BluetoothClassic.Reconnect(0);}catch(InvalidOperationException){rejected=true;}Check(rejected,"Invalid Bluetooth address reached native API");
  int polls=0;Check(!await WaitForBluetooth(()=>false,()=>{polls++;return Task.CompletedTask;},CancellationToken.None,3)&&polls==3,"Unconfirmed Bluetooth request reported success");polls=0;Check(await WaitForBluetooth(()=>polls==2,()=>{polls++;return Task.CompletedTask;},CancellationToken.None,3),"Bluetooth confirmation missed");using(var cancelled=new CancellationTokenSource()){cancelled.Cancel();bool stopped=false;try{await WaitForBluetooth(()=>false,()=>Task.CompletedTask,cancelled.Token);}catch(OperationCanceledException){stopped=true;}Check(stopped,"Bluetooth confirmation ignored cancellation");}
  await CheckBluetoothEvents();
  bool reduced=prefs.ReducedMotion;prefs.ReducedMotion=true;string theme=prefs.Theme;
  foreach(string palette in new[]{"Grafit","İnci"}){prefs.Theme=palette;ApplyTheme();Navigate("bluetooth");page.Children.Clear();RenderBluetooth(entries);root.UpdateLayout();Resize(PanelHeight(132+page.DesiredSize.Height,AvailableHeight));root.UpdateLayout();Check(page.Children.OfType<Border>().Count()==3,"Bluetooth device cards missing");var linked=(Grid)page.Children.OfType<Border>().First().Child;var disconnect=linked.Children.OfType<Button>().Single();Check(disconnect.IsEnabled&&disconnect.Content is TextBlock label&&label.Text=="Bağlantıyı kes","Connected device has no disconnect action");await RenderGalleryPreview(Path.Combine(folder,"bluetooth-"+(palette=="Grafit"?"dark":"light")+".png"));}
  connectingBluetooth=entries[1].Key;page.Children.Clear();RenderBluetooth(entries);Check(page.Children.OfType<Border>().Select(b=>b.Child).OfType<Grid>().SelectMany(g=>g.Children).OfType<ProgressRing>().Count()==1,"Bluetooth pending indicator missing");connectingBluetooth=null;bluetoothError=true;bluetoothMessage="Cihaz yanıt vermedi.";Navigate("bluetooth");Check(page.Children.OfType<Border>().Any(b=>b.Child is TextBlock t&&t.Text==bluetoothMessage),"Bluetooth error feedback missing");bluetoothError=false;bluetoothMessage="";prefs.Theme=theme;ApplyTheme();prefs.ReducedMotion=reduced;Navigate("");
  // Enumeration is read-only: never connect, pair, toggle the radio or modify audio in QA.
  var actual=await Task.Run(BluetoothClassic.ReadPaired).WaitAsync(TimeSpan.FromSeconds(15));Check(actual.All(d=>!String.IsNullOrWhiteSpace(d.Name)&&d.Address>0),"Bluetooth cached paired enumeration");
  int supported=await Task.Run(()=>actual.Count(e=>BluetoothAudio.TryRequest(e,true,probeOnly:true))).WaitAsync(TimeSpan.FromSeconds(10));
  report.Add($"Bluetooth audio driver: exact container/address matching verified; read-only KS capability probe found {supported} supported paired endpoint(s).");
  File.WriteAllLines(Path.Combine(folder,"bluetooth-enumeration.txt"),actual.Select(d=>$"{d.Name} | LE={d.LowEnergy} | connected={d.Connected} | validAddress={d.Address>0}"));
  report.Add($"Bluetooth panel: native ABI, installed-profile allowlist + failure restoration, dual-mode deduplication, poll and event confirmation, timeout/cancellation cleanup, connected disconnect action, pending/error UI, disconnect-profile persistence/restoration across restart, dark/light previews. {actual.Count} paired devices enumerated read-only; no real connection commands sent.");
 }
 static async Task CheckBluetoothEvents(){
  Action changed=null;
  void Subscribe(Action callback)=>changed+=callback;
  void Unsubscribe(Action callback)=>changed-=callback;
  bool matched=false;
  var confirmation=WaitForBluetoothUpdate(()=>matched,Subscribe,Unsubscribe,TimeSpan.FromSeconds(3),CancellationToken.None);
  changed();
  Check(!confirmation.IsCompleted,"Unrelated device update completed the Bluetooth operation");
  matched=true;
  changed();
  Check(await confirmation&&changed==null,"Bluetooth update failed to confirm or leaked its subscription");
  Check(await WaitForBluetoothUpdate(()=>true,Subscribe,Unsubscribe,TimeSpan.Zero,CancellationToken.None)&&changed==null,"Already-confirmed link waited or leaked a subscription");
  Check(!await WaitForBluetoothUpdate(()=>false,Subscribe,Unsubscribe,TimeSpan.FromMilliseconds(10),CancellationToken.None)&&changed==null,"Timed-out Bluetooth update leaked its subscription");
  using var cancelled=new CancellationTokenSource();
  var pending=WaitForBluetoothUpdate(()=>false,Subscribe,Unsubscribe,TimeSpan.FromSeconds(3),cancelled.Token);
  cancelled.Cancel();
  try{await pending;throw new InvalidOperationException("Bluetooth event wait ignored cancellation");}catch(OperationCanceledException){}
  Check(changed==null,"Cancelled Bluetooth update leaked its subscription");
 }
}
