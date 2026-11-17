// Preserve the existing on-disk field names and weather parser's object model.
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace System.Web.Script.Serialization;
public sealed class JavaScriptSerializer {
 static readonly JsonSerializerOptions options=new(){IncludeFields=true,PropertyNameCaseInsensitive=true,Converters={new ObjectConverter()}};
 public T Deserialize<T>(string text)=>JsonSerializer.Deserialize<T>(text,options);
 public string Serialize(object value)=>JsonSerializer.Serialize(value,value.GetType(),options);
 sealed class ObjectConverter:JsonConverter<object> {
  public override object Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions opts){using var doc=JsonDocument.ParseValue(ref reader);return Convert(doc.RootElement);}
  static object Convert(JsonElement e)=>e.ValueKind switch {
   JsonValueKind.Object=>ReadObject(e),JsonValueKind.Array=>ReadArray(e),JsonValueKind.String=>e.GetString(),JsonValueKind.Number=>e.TryGetInt64(out long n)?(object)n:e.GetDouble(),JsonValueKind.True=>true,JsonValueKind.False=>false,_=>null
  };
  static Dictionary<string,object> ReadObject(JsonElement e){var d=new Dictionary<string,object>();foreach(var p in e.EnumerateObject())d[p.Name]=Convert(p.Value);return d;}
  static List<object> ReadArray(JsonElement e){var a=new List<object>();foreach(var v in e.EnumerateArray())a.Add(Convert(v));return a;}
  public override void Write(Utf8JsonWriter writer,object value,JsonSerializerOptions opts)=>JsonSerializer.Serialize(writer,value,value.GetType(),opts);
 }
}
