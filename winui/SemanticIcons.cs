namespace Veilix;
internal static class SemanticIcons {
 internal static string Color(string name,bool light)=>name switch {
  "drop" or "rain" or "bluetooth"=>light?"#0066CC":"#0A84FF",
  "battery" or "check" or "connected"=>light?"#18743B":"#30D158",
  "sun" or "sunrise" or "folder"=>light?"#8A5900":"#FFD60A",
  "timer"=>light?"#A34D00":"#FF9F0A",
  "delete" or "error"=>light?"#C42B25":"#FF453A",
  _=>light?"#34343A":"#F2F2F7"
 };
}
