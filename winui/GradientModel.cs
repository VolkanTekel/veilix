using System;
using System.Collections.Generic;
using System.Linq;
namespace Ada;
public sealed class ThemeStop {public string Color="#202630";public double Offset;public ThemeStop(){}public ThemeStop(string color,double offset){Color=color;Offset=offset;}}
public static class GradientModel {
 public static List<ThemeStop> Normalize(IEnumerable<ThemeStop> stops,string first,string last){
  var result=(stops??Array.Empty<ThemeStop>()).Where(x=>x!=null&&ThemePalette.ValidHex(x.Color)&&double.IsFinite(x.Offset)).Select(x=>new ThemeStop(x.Color.ToUpperInvariant(),Math.Clamp(x.Offset,0,1))).OrderBy(x=>x.Offset).ToList();
  if(result.Count<2)return new(){new(first,0),new(last,1)};
  return result;
 }
 public static string BatteryColor(int value)=>value<=15?"#FF453A":value<=30?"#FFD60A":"#30D158";
}
