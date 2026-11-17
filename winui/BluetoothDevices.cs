using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.Devices.Enumeration;
namespace Veilix;

internal sealed record BluetoothEntry(string Id,string Container,string Name,ulong Address,bool LowEnergy,bool Connected,int? Battery=null) {
 internal string Key=>!LowEnergy&&Address>0?"classic:"+Address.ToString("X12"):String.IsNullOrEmpty(Container)?Id:Container;
 internal string Icon=>Name.Contains("keyboard",StringComparison.OrdinalIgnoreCase)||Name.Contains("klavye",StringComparison.OrdinalIgnoreCase)?"keyboard":Name.Contains("buds",StringComparison.OrdinalIgnoreCase)||Name.Contains("head",StringComparison.OrdinalIgnoreCase)||Name.Contains("kulak",StringComparison.OrdinalIgnoreCase)||Name.Contains("airpods",StringComparison.OrdinalIgnoreCase)?"headphones":"bluetooth";
}

// Reuses the existing monitor's AEP watcher. No second watcher or continuous inquiry.
internal sealed class PairedBluetoothDevices {
 internal const string Selector="(System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\" OR System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\") AND System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True";
 internal static readonly string[] Properties={"System.Devices.Aep.IsConnected","System.Devices.Aep.IsPaired","System.Devices.ContainerId","System.Devices.Aep.DeviceAddress","System.Devices.Aep.ProtocolId"};
 readonly Dictionary<string,DeviceInformation> endpoints=new(StringComparer.OrdinalIgnoreCase);
 internal void Added(DeviceInformation device){endpoints[device.Id]=device;}
 internal void Updated(DeviceInformationUpdate update){if(endpoints.TryGetValue(update.Id,out var device))device.Update(update);}
 internal void Removed(string id)=>endpoints.Remove(id);
 internal List<BluetoothEntry> Read(){
  var rows=new List<BluetoothEntry>();
  foreach(var d in endpoints.Values){
   if(!d.Pairing.IsPaired||String.IsNullOrWhiteSpace(d.Name))continue;
   string Property(string key)=>d.Properties.TryGetValue(key,out var value)?Convert.ToString(value):"";
   bool connected=d.Properties.TryGetValue("System.Devices.Aep.IsConnected",out var state)&&state is true;
   string address=Property("System.Devices.Aep.DeviceAddress").Replace(":","").Replace("-","");ulong.TryParse(address,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out ulong number);
   rows.Add(new(d.Id,Property("System.Devices.ContainerId"),d.Name,number,Property("System.Devices.Aep.ProtocolId").Contains("bb7bb05e",StringComparison.OrdinalIgnoreCase),connected));
  }
  return Merge(rows);
 }
 internal static List<BluetoothEntry> Merge(IEnumerable<BluetoothEntry> rows)=>rows.GroupBy(d=>String.IsNullOrEmpty(d.Container)?d.Key:d.Container,StringComparer.OrdinalIgnoreCase).Select(g=>{
  var primary=g.OrderBy(d=>d.LowEnergy).ThenBy(d=>d.Id,StringComparer.Ordinal).First();
  return primary with{Connected=g.Any(d=>d.Connected),Battery=g.Select(d=>d.Battery).FirstOrDefault(b=>b.HasValue)};
 }).OrderByDescending(d=>d.Connected).ThenBy(d=>d.Name,StringComparer.CurrentCultureIgnoreCase).ToList();
}
