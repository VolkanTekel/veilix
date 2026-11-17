using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using static Veilix.UiLanguage;
namespace Veilix;

// Remembers only profiles that Windows already installed on the selected paired
// device. Disconnect does not unpair; the same profiles can be restored after restart.
internal sealed class BluetoothProfiles {
 readonly string path;
 internal BluetoothProfiles(string path=null){this.path=path??Path.Combine(AppContext.BaseDirectory,"data","bluetooth-profiles.json");}
 Dictionary<string,Guid[]> Read(){
  if(!File.Exists(path))return new(StringComparer.OrdinalIgnoreCase);
  var data=JsonSerializer.Deserialize<Dictionary<string,Guid[]>>(File.ReadAllText(path));
  if(data==null||data.Count>128||data.Values.Any(v=>v==null||v.Length>256))throw new InvalidDataException(L("Bluetooth profil kaydı okunamadı."));return data;
 }
 internal Guid[] Get(ulong address)=>Read().GetValueOrDefault(address.ToString("X12"))??Array.Empty<Guid>();
 internal void Set(ulong address,IEnumerable<Guid> profiles){var data=Read();var list=profiles.Distinct().ToArray();if(list.Length>256)throw new InvalidDataException(L("Çok fazla Bluetooth profili."));string key=address.ToString("X12");if(list.Length==0)data.Remove(key);else data[key]=list;Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));string temporary=path+".tmp";File.WriteAllText(temporary,JsonSerializer.Serialize(data));File.Move(temporary,path,true);}
}
