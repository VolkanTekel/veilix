using System;
using System.Runtime.InteropServices;
using Ada;

namespace Veilix;

internal static class BluetoothAudio {
 const uint Active=1, Unplugged=8, AllContexts=23;
 const uint Get=1, BasicSupport=0x200;
 static readonly Guid BtAudio=new("7FA06C40-B8F6-4C7E-8556-E8C33A12E54D");
 static readonly Guid ContainerPropertySet=new("8C7ED206-3F8A-4827-B3AB-AE9E1FAEFC6C");

 internal static bool Matches(BluetoothEntry entry,Guid container,string adapterId){
  if(entry.LowEnergy||String.IsNullOrEmpty(adapterId)||
   !(adapterId.Contains("BTHENUM",StringComparison.OrdinalIgnoreCase)||adapterId.Contains("BTHHFENUM",StringComparison.OrdinalIgnoreCase)))return false;
  if(container!=Guid.Empty&&Guid.TryParse(entry.Container,out var expected)&&expected!=Guid.Empty)
   return container==expected;
  string address=entry.Address.ToString("X12");
  return entry.Address!=0&&(adapterId.Contains("DEV_"+address,StringComparison.OrdinalIgnoreCase)||
   adapterId.Contains("&0&"+address+"_",StringComparison.OrdinalIgnoreCase));
 }

 internal static bool TryRequest(BluetoothEntry entry,bool connect,bool probeOnly=false){
  if(entry.LowEnergy||entry.Address==0)return false;
  IMMDeviceEnumerator enumerator=null;
  EndpointCollection endpoints=null;
  try{
   enumerator=(IMMDeviceEnumerator)new DeviceEnumerator();
   if(enumerator.EnumAudioEndpoints(0,Active|Unplugged,out var pointer)<0||pointer==IntPtr.Zero)return false;
   endpoints=FromPointer<EndpointCollection>(pointer);
   if(endpoints.GetCount(out uint count)<0)return false;
   for(uint i=0;i<count;i++){
    if(endpoints.Item(i,out var endpoint)<0||endpoint==null)continue;
    try{
     Guid container=ReadContainer(endpoint);
     if(container!=Guid.Empty&&Guid.TryParse(entry.Container,out var requested)&&requested!=Guid.Empty&&container!=requested)continue;
     if(TryEndpoint(enumerator,endpoint,entry,container,connect,probeOnly))return true;
    }finally{Release(endpoint);}
   }
   return false;
  }catch(COMException){return false;}
  finally{Release(endpoints);Release(enumerator);}
 }

 static bool TryEndpoint(IMMDeviceEnumerator enumerator,IMMDevice endpoint,BluetoothEntry entry,Guid container,bool connect,bool probeOnly){
  var topologyId=typeof(DeviceTopology).GUID;
  if(endpoint.Activate(ref topologyId,AllContexts,IntPtr.Zero,out var result)<0||result==null)return false;
  try{
   var topology=(DeviceTopology)result;
   if(topology.GetConnectorCount(out uint count)<0)return false;
   for(uint i=0;i<count;i++){
    if(topology.GetConnector(i,out var connector)<0||connector==null)continue;
    try{
     if(connector.GetDeviceIdConnectedTo(out var id)<0||!Matches(entry,container,id))continue;
     if(enumerator.GetDevice(id,out var adapter)<0||adapter==null)continue;
     try{
      var controlId=typeof(KsControl).GUID;
      if(adapter.Activate(ref controlId,AllContexts,IntPtr.Zero,out var control)<0||control==null)continue;
      try{
       var property=new KsProperty{Set=BtAudio,Id=connect?0u:1u,Flags=probeOnly?BasicSupport:Get};
       if(!probeOnly)return ((KsControl)control).Property(ref property,24,IntPtr.Zero,0,out _)>=0;
       IntPtr buffer=Marshal.AllocHGlobal(64);
       try{return ((KsControl)control).Property(ref property,24,buffer,64,out _)>=0;}
       finally{Marshal.FreeHGlobal(buffer);}
      }finally{Release(control);}
     }finally{Release(adapter);}
    }finally{Release(connector);}
   }
   return false;
  }finally{Release(result);}
 }

 static Guid ReadContainer(IMMDevice endpoint){
  if(endpoint.OpenPropertyStore(0,out var pointer)<0||pointer==IntPtr.Zero)return Guid.Empty;
  var store=FromPointer<PropertyStore>(pointer);
  try{
   var key=new PropertyKey{Set=ContainerPropertySet,Id=2};
   if(store.GetValue(ref key,out var value)<0)return Guid.Empty;
   try{return value.Type==72&&value.Pointer!=IntPtr.Zero?Marshal.PtrToStructure<Guid>(value.Pointer):Guid.Empty;}
   finally{PropVariantClear(ref value);}
  }finally{Release(store);}
 }

 static T FromPointer<T>(IntPtr pointer) where T:class {
  try{return (T)Marshal.GetObjectForIUnknown(pointer);}
  finally{Marshal.Release(pointer);}
 }
 static void Release(object value){if(value!=null)Marshal.ReleaseComObject(value);}
 [DllImport("ole32.dll")]static extern int PropVariantClear(ref PropertyValue value);
 [StructLayout(LayoutKind.Sequential)]struct KsProperty {internal Guid Set;internal uint Id,Flags;}
 [StructLayout(LayoutKind.Sequential)]struct PropertyKey {internal Guid Set;internal uint Id;}
 [StructLayout(LayoutKind.Explicit,Size=24)]struct PropertyValue {
  [FieldOffset(0)]internal ushort Type;
  [FieldOffset(8)]internal IntPtr Pointer;
 }

 [ComImport,Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface EndpointCollection {
  [PreserveSig]int GetCount(out uint count);
  [PreserveSig]int Item(uint index,out IMMDevice endpoint);
 }
 [ComImport,Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface PropertyStore {
  [PreserveSig]int GetCount(out uint count);
  [PreserveSig]int GetAt(uint index,out PropertyKey key);
  [PreserveSig]int GetValue(ref PropertyKey key,out PropertyValue value);
 }
 [ComImport,Guid("2A07407E-6497-4A18-9787-32F79BD0D98F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface DeviceTopology {
  [PreserveSig]int GetConnectorCount(out uint count);
  [PreserveSig]int GetConnector(uint index,out Connector connector);
 }
 [ComImport,Guid("9C2C4058-23F5-41DE-877A-DF3AF236A09E"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface Connector {
  [PreserveSig]int GetType(out int type);
  [PreserveSig]int GetDataFlow(out int flow);
  [PreserveSig]int ConnectTo(Connector other);
  [PreserveSig]int Disconnect();
  [PreserveSig]int IsConnected(out int connected);
  [PreserveSig]int GetConnectedTo(out Connector other);
  [PreserveSig]int GetConnectorIdConnectedTo([MarshalAs(UnmanagedType.LPWStr)]out string id);
  [PreserveSig]int GetDeviceIdConnectedTo([MarshalAs(UnmanagedType.LPWStr)]out string id);
 }
 [ComImport,Guid("28F54685-06FD-11D2-B27A-00A0C9223196"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface KsControl {
  [PreserveSig]int Property(ref KsProperty property,uint propertySize,IntPtr data,uint dataSize,out uint written);
 }
}
