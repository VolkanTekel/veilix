// GPL-3.0-or-later. Calm palettes and a personal palette editor.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.UI;


namespace Ada {
 public sealed class ThemePalette {
  public string Name,Start,End,Accent;public bool Light;
  public ThemePalette(string name,string start,string end,string accent){Name=name;Start=start;End=end;Accent=accent;Light=Luminance(Parse(start))>.45;}
  public static readonly ThemePalette[] Presets={
   new ThemePalette("Grafit","#16191E","#242A33","#C2CED9"),new ThemePalette("Kömür","#151617","#292C2E","#C8CFD5"),
   new ThemePalette("Sis","#1B242D","#2B3B47","#B7CFDF"),new ThemePalette("Adaçayı","#1C2521","#2C3B31","#B8D0BD"),
   new ThemePalette("Gece Mavisi","#161D2B","#283950","#AFC9E6"),new ThemePalette("Mürdüm","#281F2C","#403049","#D7BEDC"),
   new ThemePalette("İndigo","#201E33","#35324F","#C1BDE8"),new ThemePalette("Bordo","#2D1C22","#492B36","#DDB5C5"),
   new ThemePalette("Karamel","#2A221D","#40342A","#D9C3A8"),new ThemePalette("Okyanus","#17282C","#284047","#A4CFD0"),
   new ThemePalette("Kehribar","#282117","#40351F","#DCC596"),new ThemePalette("Petrol","#17272A","#294349","#A4CCD0"),
   new ThemePalette("Obsidyen","#101116","#252530","#BDBBD6"),new ThemePalette("Bazalt","#1B1E22","#32363B","#C4CBD2"),
   new ThemePalette("Antrasit","#171A1B","#2A3335","#BDCDCD"),new ThemePalette("Duman","#202228","#343944","#BEC8DC"),
   new ThemePalette("Çelik","#171E27","#303D4B","#ABC8E4"),new ThemePalette("Kurşun","#22232B","#393D49","#C5C7DF"),
   new ThemePalette("Gece Yarısı","#101827","#21304C","#ABC3EF"),new ThemePalette("Derin Uzay","#15142B","#2A2850","#BAB6EF"),
   new ThemePalette("Kutup Gecesi","#14232E","#253D4C","#ACD5E4"),new ThemePalette("Kobalt","#12213A","#243D5A","#9CC4EE"),
   new ThemePalette("Safir","#14203A","#2B3155","#B5C1F5"),new ThemePalette("Yağmur","#1B252C","#33424C","#B7CEDD"),
   new ThemePalette("Fırtına","#20232F","#35384E","#C6C5E8"),new ThemePalette("Denizaltı","#11252D","#20414A","#9CD3D7"),
   new ThemePalette("Lagün","#142A2A","#23473F","#A5D8CA"),new ThemePalette("Yosun","#20281B","#35442B","#C1D3A5"),
   new ThemePalette("Orman","#14261D","#254532","#A8D7B6"),new ThemePalette("Çam","#132724","#26453D","#A5D5C6"),
   new ThemePalette("Zeytin","#28291D","#41432E","#D0D1AC"),new ThemePalette("Okaliptüs","#1B2926","#304740","#B1D8C8"),
   new ThemePalette("Nane Gecesi","#162A28","#2A4848","#A8DADB"),new ThemePalette("Matcha","#242B20","#3D4733","#CAD7B1"),
   new ThemePalette("Erik","#291C2D","#452D48","#D8B7DD"),new ThemePalette("Ametist","#251D35","#3D3054","#D0B9E8"),
   new ThemePalette("Leylak Gecesi","#242132","#3D3751","#D0C2E7"),new ThemePalette("Orkide","#2C1E2E","#4A3047","#E1B7D8"),
   new ThemePalette("İncir","#2E2029","#47313E","#DDBFCC"),new ThemePalette("Dut","#301B26","#4C2B3C","#E2ADC7"),
   new ThemePalette("Vişne","#2B1921","#492731","#E4B4BF"),new ThemePalette("Gül Kurusu","#2D2328","#49353C","#DFC0C8"),
   new ThemePalette("Kiremit","#2C211D","#49332B","#DFBDAA"),new ThemePalette("Bakır","#2A211B","#49372A","#E3C09F"),
   new ThemePalette("Tarçın","#2B201B","#453028","#DEB79D"),new ThemePalette("Espresso","#211B18","#39302A","#D3C0AA"),
   new ThemePalette("Kakao","#29211F","#423631","#D7C2B9"),new ThemePalette("Mocha","#282324","#433B3A","#D4C6BE"),
   new ThemePalette("Kumtaşı","#29261F","#443E30","#DBCFB1"),new ThemePalette("Gece Altını","#28251A","#433C26","#DFD09A"),
   new ThemePalette("İnci","#F3F2EF","#DFE3E8","#425B7A"),new ThemePalette("Bulut Beyazı","#F3F5F8","#DCE4ED","#405B7C"),
   new ThemePalette("Buz Mavisi","#EAF3FA","#CFDFEF","#305F8C"),new ThemePalette("Nane","#E9F3ED","#CDE3D6","#35664E"),
   new ThemePalette("Lavanta Açığı","#EFEAF8","#DAD0ED","#65518C"),new ThemePalette("Gül Pembesi","#F9EBEF","#EDCFD9","#864F65"),
   new ThemePalette("Şeftali","#FAEEE4","#EED6C4","#845A3B"),new ThemePalette("Kum Beji","#F5EFE4","#E3D7BF","#75603B"),
   new ThemePalette("Gün Işığı","#F8F3DC","#EBE1B8","#726024"),new ThemePalette("Deniz Köpüğü","#E6F3F2","#CAE4E2","#35666B"),
   new ThemePalette("Lila","#F0EAF3","#E0CFE6","#755781"),new ThemePalette("Gümüş","#EEF0F2","#D4DCE1","#4C6070")
  };
  public static bool ValidHex(string value){uint n;return value!=null&&value.Length==7&&value[0]=='#'&&UInt32.TryParse(value.Substring(1),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out n);}
  public static Color Parse(string value){if(!ValidHex(value))throw new FormatException("Geçersiz renk kodu.");return Color.FromArgb(255,Convert.ToByte(value.Substring(1,2),16),Convert.ToByte(value.Substring(3,2),16),Convert.ToByte(value.Substring(5,2),16));}
  public static string Mix(string a,string b,double amount){var x=Parse(a);var y=Parse(b);return String.Format("#{0:X2}{1:X2}{2:X2}",(byte)Math.Round(x.R+(y.R-x.R)*amount),(byte)Math.Round(x.G+(y.G-x.G)*amount),(byte)Math.Round(x.B+(y.B-x.B)*amount));}
  static double Channel(byte n){double v=n/255.0;return v<=.04045?v/12.92:Math.Pow((v+.055)/1.055,2.4);}
  static double Luminance(Color c){return .2126*Channel(c.R)+.7152*Channel(c.G)+.0722*Channel(c.B);}
  public static double Contrast(string a,string b){double x=Luminance(Parse(a)),y=Luminance(Parse(b));return (Math.Max(x,y)+.05)/(Math.Min(x,y)+.05);}
  public bool Readable{get{string text=Light?"#282C31":"#EFF2F5",tile=Mix(Start,Light?"#101820":"#D4DFEB",Light?.06:.10);return Contrast(text,Start)>=4.5&&Contrast(text,End)>=4.5&&Contrast(text,tile)>=4.5;}}
  public static ThemePalette Get(Settings settings){return settings.Theme=="Özel"?new ThemePalette("Özel",settings.CustomStart,settings.CustomEnd,settings.CustomAccent):Presets.FirstOrDefault(x=>x.Name==settings.Theme)??Presets[0];}
 }

}
