using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Microsoft.UI.Dispatching;
using Ada;
namespace Veilix;
internal sealed class BluetoothMonitor:IDisposable {
 readonly DispatcherQueue queue;readonly DispatcherQueueTimer debounce,fallback;readonly DeviceWatcher watcher;readonly ConnectionTracker connection=new();readonly Dictionary<string,string> containers=new(StringComparer.OrdinalIgnoreCase);bool busy,pending,disposed;
 internal List<BluetoothBattery> Devices=new();internal event Action Changed;
 readonly PairedBluetoothDevices paired=new();
 List<BluetoothEntry> cachedPaired=new();
 internal List<BluetoothEntry> Paired {get{var endpoints=paired.Read();return PairedBluetoothDevices.Merge(endpoints.Concat(cachedPaired.Where(n=>!endpoints.Any(d=>d.Address==n.Address&&!d.LowEnergy)))).Select(d=>d with{Battery=Devices.FirstOrDefault(b=>!String.IsNullOrEmpty(d.Container)&&String.Equals(b.Container,d.Container,StringComparison.OrdinalIgnoreCase)||d.Address>0&&b.Id.Contains(d.Address.ToString("X12"),StringComparison.OrdinalIgnoreCase))?.Percent}).ToList();}}
 internal BluetoothMonitor(DispatcherQueue queue){this.queue=queue;debounce=queue.CreateTimer();debounce.Interval=TimeSpan.FromMilliseconds(400);debounce.IsRepeating=false;debounce.Tick+=(_,e)=>UiFaultBoundary.Run(Refresh,"bluetooth refresh");fallback=queue.CreateTimer();fallback.Interval=TimeSpan.FromSeconds(30);fallback.Tick+=(_,e)=>UiFaultBoundary.Run(Refresh,"bluetooth fallback");
  try{watcher=DeviceInformation.CreateWatcher(PairedBluetoothDevices.Selector,PairedBluetoothDevices.Properties,DeviceInformationKind.AssociationEndpoint);
  watcher.Added+=(_,d)=>Post(()=>{paired.Added(d);Record(d.Id,d.Properties);Changed?.Invoke();});watcher.Updated+=(_,d)=>Post(()=>{paired.Updated(d);Record(d.Id,d.Properties);Changed?.Invoke();});watcher.Removed+=(_,d)=>Post(()=>{paired.Removed(d.Id);connection.Remove(d.Id);containers.Remove(d.Id);Prune();Schedule();});}catch(Exception ex){Program.Trace("Bluetooth watcher unavailable: "+ex.Message);fallback.Interval=TimeSpan.FromSeconds(5);}
 }
 void Post(Action action){if(!disposed)UiFaultBoundary.Enqueue(queue,()=>{if(!disposed)action();},"bluetooth events");}
 void Record(string id,IReadOnlyDictionary<string,object> props){if(props.TryGetValue("System.Devices.ContainerId",out var c)&&c!=null)containers[id]=c.ToString();if(props.TryGetValue("System.Devices.Aep.IsConnected",out var value)&&value is bool connected&&containers.TryGetValue(id,out var container)){connection.Set(id,container,connected);if(!connected)Prune();}Schedule();}
 void Prune(){Devices.RemoveAll(d=>connection.Connected(d.Container)==false);Changed?.Invoke();}
 internal void Start(){try{watcher?.Start();}catch{}fallback.Start();Refresh();}
 internal void Schedule(){if(!disposed){debounce.Stop();debounce.Start();}}
 internal async void Refresh(){
  if(disposed)return;
  if(busy){pending=true;return;}
  busy=true;
  try{
   var devices=await Task.Run(BluetoothClassic.ReadPaired);
   if(disposed)return;
   cachedPaired=devices;
   Changed?.Invoke();
   var batteries=await Task.Run(BluetoothProperties.Read);
   if(disposed)return;
   Devices=batteries.Where(d=>connection.Connected(d.Container)!=false).ToList();
   Changed?.Invoke();
  }catch{}finally{busy=false;if(pending){pending=false;Schedule();}}
 }
 public void Dispose(){disposed=true;debounce.Stop();fallback.Stop();try{if(watcher!=null&&(watcher.Status==DeviceWatcherStatus.Started||watcher.Status==DeviceWatcherStatus.EnumerationCompleted))watcher.Stop();}catch{}}
}

