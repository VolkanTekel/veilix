using System;
using System.Collections.Generic;
using System.Linq;
namespace Veilix;
internal sealed class ConnectionTracker {
 readonly Dictionary<string,(string Container,bool Connected)> entries=new(StringComparer.OrdinalIgnoreCase);readonly HashSet<string> removed=new(StringComparer.OrdinalIgnoreCase);
 internal void Set(string id,string container,bool connected){if(string.IsNullOrEmpty(container))return;entries[id]=(container,connected);removed.Remove(container);}
 internal void Remove(string id){if(!entries.Remove(id,out var old))return;if(!entries.Values.Any(x=>x.Container.Equals(old.Container,StringComparison.OrdinalIgnoreCase)))removed.Add(old.Container);}
 internal bool? Connected(string container){if(string.IsNullOrEmpty(container))return null;var matches=entries.Values.Where(x=>x.Container.Equals(container,StringComparison.OrdinalIgnoreCase)).ToArray();return matches.Length>0?matches.Any(x=>x.Connected):removed.Contains(container)?false:null;}
}
