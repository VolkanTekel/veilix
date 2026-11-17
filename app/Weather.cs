// GPL-3.0-or-later. Open-Meteo current, daily and short hourly forecast.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
#if !WINUI
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
#endif

namespace Ada {
 public sealed class WeatherHour {public DateTime Time;public double Temperature,RainChance;public int Code;public bool IsDay;}
 public sealed class WeatherDetails {
  public double Temperature,FeelsLike,Humidity,Wind,High,Low,RainChance;
  public DateTime Sunrise,Sunset,Time;public List<WeatherHour> Hours=new List<WeatherHour>();
 }
 public sealed class WeatherService : IDisposable {
  readonly HttpClient client=new HttpClient{Timeout=TimeSpan.FromSeconds(12)};readonly JavaScriptSerializer json=new JavaScriptSerializer();CancellationTokenSource refreshCancel;bool disposed;
  public string Summary="Konum seç",Description="Hava durumu için ayarlardan şehir seç.",City="",ConditionText="";
  double currentLatitude,currentLongitude;
  public DateTime Updated=DateTime.MinValue;public bool Stale,IsDay=true;public int Code=-1;public WeatherDetails Details;public event Action Changed;
  public WeatherService(){client.DefaultRequestHeaders.UserAgent.ParseAdd("AdaDesktop/0.2");}
  public async Task<List<Place>> Search(string query,string language="tr"){var list=new List<Place>();query=(query??"").Trim().Normalize();if(query.Length<2)return list;
   var raw=await client.GetStringAsync("https://geocoding-api.open-meteo.com/v1/search?count=6&language="+Uri.EscapeDataString(language)+"&name="+Uri.EscapeDataString(query.Trim()));
   var root=json.Deserialize<Dictionary<string,object>>(raw);object matches;if(root==null||!root.TryGetValue("results",out matches)||!(matches is IEnumerable))return list;
   foreach(Dictionary<string,object> p in (IEnumerable)matches){string name=Convert.ToString(p["name"]);list.Add(new Place{Name=name,Label=name+", "+(p.ContainsKey("admin1")?p["admin1"]+", ":"")+(p.ContainsKey("country")?p["country"]:""),Latitude=Convert.ToDouble(p["latitude"],CultureInfo.InvariantCulture),Longitude=Convert.ToDouble(p["longitude"],CultureInfo.InvariantCulture)});}return list;
  }
  public async Task Refresh(Settings settings){
   if(disposed)return;if(refreshCancel!=null){refreshCancel.Cancel();refreshCancel.Dispose();}var cancellation=new CancellationTokenSource();refreshCancel=cancellation;
   if(!settings.ShowWeather||String.IsNullOrWhiteSpace(settings.City)){Summary="Konum seç";City="";Code=-1;Details=null;Notify();return;}
   string city=settings.City;double lat=settings.Latitude,lon=settings.Longitude;
   if(City!=city||currentLatitude!=lat||currentLongitude!=lon){Details=null;Code=-1;Summary="—°";ConditionText="Bilgi bekleniyor";Stale=false;City=city;currentLatitude=lat;currentLongitude=lon;}
   try{
    string url="https://api.open-meteo.com/v1/forecast?latitude="+lat.ToString(CultureInfo.InvariantCulture)+"&longitude="+lon.ToString(CultureInfo.InvariantCulture)+"&current=temperature_2m,apparent_temperature,relative_humidity_2m,weather_code,wind_speed_10m,is_day&daily=temperature_2m_max,temperature_2m_min,precipitation_probability_max,sunrise,sunset&hourly=temperature_2m,weather_code,precipitation_probability,is_day&forecast_days=2&temperature_unit=celsius&wind_speed_unit=kmh&timezone=auto";
    using(var response=await client.GetAsync(url,cancellation.Token)){
     response.EnsureSuccessStatusCode();var raw=await response.Content.ReadAsStringAsync();if(cancellation.IsCancellationRequested||disposed)return;
     var root=json.Deserialize<Dictionary<string,object>>(raw);var current=(Dictionary<string,object>)root["current"];var daily=(Dictionary<string,object>)root["daily"];var hourly=(Dictionary<string,object>)root["hourly"];
     var details=new WeatherDetails{Temperature=Number(current["temperature_2m"]),FeelsLike=Number(current["apparent_temperature"]),Humidity=Number(current["relative_humidity_2m"]),Wind=Number(current["wind_speed_10m"]),Time=Time(current["time"]),High=Number(At(daily,"temperature_2m_max",0)),Low=Number(At(daily,"temperature_2m_min",0)),RainChance=Number(At(daily,"precipitation_probability_max",0)),Sunrise=Time(At(daily,"sunrise",0)),Sunset=Time(At(daily,"sunset",0))};
     var times=(IList)hourly["time"];for(int i=0;i<times.Count&&details.Hours.Count<6;i++){DateTime time=Time(times[i]);if(time<details.Time)continue;details.Hours.Add(new WeatherHour{Time=time,Temperature=Number(At(hourly,"temperature_2m",i)),Code=(int)Number(At(hourly,"weather_code",i)),RainChance=Number(At(hourly,"precipitation_probability",i)),IsDay=Number(At(hourly,"is_day",i))==1});}
     Code=(int)Number(current["weather_code"]);IsDay=Number(current["is_day"])==1;ConditionText=Condition(Code);Details=details;Summary=Math.Round(details.Temperature)+"°";City=city;Description=city+" · "+ConditionText;Updated=DateTime.Now;Stale=false;
    }
   }catch(OperationCanceledException){if(cancellation.IsCancellationRequested)return;Stale=true;Description=city+" · İstek zaman aşımına uğradı";}
   catch(Exception){if(cancellation.IsCancellationRequested||disposed)return;Stale=true;Description=city+" · Bağlantı yok";}
   Notify();
  }
  static object At(Dictionary<string,object> values,string key,int index){return ((IList)values[key])[index];}
  static double Number(object value){return value==null?Double.NaN:Convert.ToDouble(value,CultureInfo.InvariantCulture);}
  static DateTime Time(object value){DateTime parsed;return DateTime.TryParse(Convert.ToString(value,CultureInfo.InvariantCulture),CultureInfo.InvariantCulture,DateTimeStyles.None,out parsed)?parsed:DateTime.MinValue;}
  public static string Condition(int c){return c<0?"Bilgi bekleniyor":c==0?"Açık":c==1?"Çoğunlukla açık":c==2?"Parçalı bulutlu":c==3?"Kapalı":c==45||c==48?"Sisli":c>=95?"Gök gürültülü":c>=71&&c<=77||c==85||c==86?"Kar yağışlı":c>=51&&c<=57?"Çiseleme":"Yağmurlu";}
  void Notify(){if(disposed)return;var h=Changed;if(h!=null)h();}
  public void Dispose(){disposed=true;if(refreshCancel!=null){refreshCancel.Cancel();refreshCancel.Dispose();}client.Dispose();}
 }
#if !WINUI
 public sealed partial class AdaWindow {
  static string WeatherIcon(int code,bool day=true){return code==0||code==1?day?"sun":"moon":code<0||code<=3?"cloud":code==45||code==48?"fog":code>=95?"storm":code>=71&&code<=77||code==85||code==86?"snow":"rain";}
  static string WeatherValue(double value,string suffix=""){return Double.IsNaN(value)||Double.IsInfinity(value)?"—":Math.Round(value).ToString(CultureInfo.CurrentCulture)+suffix;}
  void RenderWeather(){
   var heading=new DockPanel{Margin=new Thickness(0,0,0,14)};var close=IconButton("Hava durumunu kapat","close",delegate{Navigate("");});close.Width=36;close.Height=36;close.Background=Brushes.Transparent;DockPanel.SetDock(close,Dock.Right);heading.Children.Add(close);
   var place=Label(Preferences.City.Length>0?Preferences.City:"Hava durumu",21,true);place.VerticalAlignment=VerticalAlignment.Center;heading.Children.Add(place);content.Children.Add(heading);
   var details=weather.Details;
   if(details==null){Section(Preferences.City.Length==0?"Günün havası burada.":"Şu an hava bilgisine ulaşılamıyor.",Preferences.City.Length==0?"Bir şehir seç; günün ayrıntılarını buradan takip et.":"Bağlantı geldiğinde bilgiler yenilenecek.");if(Preferences.City.Length==0)content.Children.Add(MakeButton("Konum seç",ButtonContent("cloud","Konum seç"),delegate{GoSettings("Hava");}));else content.Children.Add(MakeButton("Hava durumunu yenile",ButtonContent("refresh","Yeniden dene"),async delegate{await weather.Refresh(Preferences);}));return;}
   var hero=new Grid{Margin=new Thickness(4,0,4,18)};hero.ColumnDefinitions.Add(new ColumnDefinition());hero.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(92)});
   var info=new StackPanel();var day=Label(details.Time.ToString("d MMMM dddd",tr),13,false,"Muted");info.Children.Add(day);var temperature=Label(WeatherValue(details.Temperature,"°"),54);temperature.FontFamily=new FontFamily("Bahnschrift, Segoe UI");temperature.Margin=new Thickness(0,8,0,0);info.Children.Add(temperature);info.Children.Add(Label(weather.ConditionText,16,true));var range=Label("En yüksek "+WeatherValue(details.High,"°")+"   ·   En düşük "+WeatherValue(details.Low,"°"),12,false,"Muted");range.Margin=new Thickness(0,7,0,0);info.Children.Add(range);hero.Children.Add(info);
   var symbol=Glyph(WeatherIcon(weather.Code,weather.IsDay),72,"Accent");Grid.SetColumn(symbol,1);hero.Children.Add(symbol);content.Children.Add(hero);
   var metrics=new UniformGrid{Columns=2,Margin=new Thickness(-4,0,-4,6)};
   WeatherMetric(metrics,"temperature","Hissedilen",WeatherValue(details.FeelsLike,"°"));WeatherMetric(metrics,"drop","Yağış ihtimali",WeatherValue(details.RainChance,"%"));WeatherMetric(metrics,"wind","Rüzgâr",WeatherValue(details.Wind," km/sa"));WeatherMetric(metrics,"drop","Nem",WeatherValue(details.Humidity,"%"));content.Children.Add(metrics);
   if(details.Hours.Count>0){Section("Önümüzdeki saatler");var hours=new UniformGrid{Columns=details.Hours.Count,Margin=new Thickness(0,3,0,18)};foreach(var hour in details.Hours){var cell=new StackPanel{HorizontalAlignment=HorizontalAlignment.Center};var time=Label(hour.Time.ToString("HH:mm"),12,false,"Muted");time.HorizontalAlignment=HorizontalAlignment.Center;cell.Children.Add(time);var icon=Glyph(WeatherIcon(hour.Code,hour.IsDay),25);icon.Margin=new Thickness(0,10,0,9);cell.Children.Add(icon);var temp=Label(WeatherValue(hour.Temperature,"°"),16,true);temp.HorizontalAlignment=HorizontalAlignment.Center;cell.Children.Add(temp);var rain=Label(WeatherValue(hour.RainChance,"%"),11,false,"Muted");rain.HorizontalAlignment=HorizontalAlignment.Center;rain.Margin=new Thickness(0,5,0,0);cell.Children.Add(rain);hours.Children.Add(cell);}content.Children.Add(hours);}
   var sun=new UniformGrid{Columns=2};SunTime(sun,"Gün doğumu",details.Sunrise);SunTime(sun,"Gün batımı",details.Sunset);content.Children.Add(Card(sun));
   var footer=new DockPanel();var refresh=IconButton("Hava durumunu yenile","refresh",async delegate{await weather.Refresh(Preferences);});refresh.Width=36;refresh.Height=36;refresh.Background=Brushes.Transparent;DockPanel.SetDock(refresh,Dock.Right);footer.Children.Add(refresh);var updated=Label(weather.Stale?"Son bilgi · Bağlantı bekleniyor":"Güncellendi "+weather.Updated.ToString("HH:mm"),11,false,"Muted");updated.VerticalAlignment=VerticalAlignment.Center;updated.ToolTip="Open-Meteo · Yerel saatler seçilen şehre aittir.";footer.Children.Add(updated);content.Children.Add(footer);
  }
  void WeatherMetric(Panel grid,string icon,string label,string value){var row=new Grid();row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(32)});row.ColumnDefinitions.Add(new ColumnDefinition());row.Children.Add(Glyph(icon,23,"Accent"));var text=new StackPanel{Margin=new Thickness(8,0,0,0)};text.Children.Add(Label(label,11,false,"Muted"));var number=Label(value,19,true);number.Margin=new Thickness(0,5,0,0);text.Children.Add(number);Grid.SetColumn(text,1);row.Children.Add(text);var card=Card(row);card.Margin=new Thickness(4,0,4,8);card.Padding=new Thickness(13);grid.Children.Add(card);}
  void SunTime(Panel panel,string label,DateTime time){var row=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};row.Children.Add(Glyph("sunrise",23,"Accent"));var text=new StackPanel{Margin=new Thickness(10,0,0,0)};var caption=Label(label,11,false,"Muted");caption.TextTrimming=TextTrimming.None;text.Children.Add(caption);text.Children.Add(Label(time==DateTime.MinValue?"—":time.ToString("HH:mm"),16,true));row.Children.Add(text);panel.Children.Add(row);}
 }
#endif
}
