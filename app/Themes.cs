// GPL-3.0-or-later. Calm palettes and a personal palette editor.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using Forms=System.Windows.Forms;

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
   new ThemePalette("Kehribar","#282117","#40351F","#DCC596"),new ThemePalette("Petrol","#17272A","#294349","#A4CCD0")
  };
  public static bool ValidHex(string value){uint n;return value!=null&&value.Length==7&&value[0]=='#'&&UInt32.TryParse(value.Substring(1),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out n);}
  public static Color Parse(string value){return (Color)ColorConverter.ConvertFromString(value);}
  public static string Mix(string a,string b,double amount){var x=Parse(a);var y=Parse(b);return String.Format("#{0:X2}{1:X2}{2:X2}",(byte)Math.Round(x.R+(y.R-x.R)*amount),(byte)Math.Round(x.G+(y.G-x.G)*amount),(byte)Math.Round(x.B+(y.B-x.B)*amount));}
  static double Channel(byte n){double v=n/255.0;return v<=.04045?v/12.92:Math.Pow((v+.055)/1.055,2.4);}
  static double Luminance(Color c){return .2126*Channel(c.R)+.7152*Channel(c.G)+.0722*Channel(c.B);}
  public static double Contrast(string a,string b){double x=Luminance(Parse(a)),y=Luminance(Parse(b));return (Math.Max(x,y)+.05)/(Math.Min(x,y)+.05);}
  public bool Readable{get{string text=Light?"#282C31":"#EFF2F5",tile=Mix(Start,Light?"#101820":"#D4DFEB",Light?.06:.10);return Contrast(text,Start)>=4.5&&Contrast(text,End)>=4.5&&Contrast(text,tile)>=4.5;}}
  public static ThemePalette Get(Settings settings){return settings.Theme=="Özel"?new ThemePalette("Özel",settings.CustomStart,settings.CustomEnd,settings.CustomAccent):Presets.FirstOrDefault(x=>x.Name==settings.Theme)??Presets[0];}
 }
 public sealed partial class AdaWindow {
  bool themeEditorOpen;Window themeEditor;
  void RenderThemeChoices(){
   Section("Renk paleti");var grid=new UniformGrid{Columns=3,Margin=new Thickness(0,0,0,14)};
   foreach(var palette in ThemePalette.Presets.Concat(new[]{new ThemePalette("Özel",Preferences.CustomStart,Preferences.CustomEnd,Preferences.CustomAccent)})){
    var p=palette;var row=new StackPanel{Orientation=Orientation.Horizontal};
    row.Children.Add(ThemeSwatch(p));
    var text=Label(p.Name,12,Preferences.Theme==p.Name);text.TextTrimming=TextTrimming.None;text.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(text);
    var button=MakeButton(p.Name,row,delegate{if(p.Name=="Özel")OpenThemeEditor();else{Preferences.Theme=p.Name;ApplyTheme();ApplyMaterial();Save();RenderRoute(false);}});
    button.Background=(Brush)Resources[Preferences.Theme==p.Name?"Selected":"Tile"];button.Margin=new Thickness(3);button.Padding=new Thickness(8,9,8,9);grid.Children.Add(button);
   }content.Children.Add(grid);
   if(Preferences.Theme=="Özel"){var edit=MakeButton("Özel temayı düzenle",ButtonContent("palette","Özel temayı düzenle"),OpenThemeEditor);edit.HorizontalAlignment=HorizontalAlignment.Left;edit.Margin=new Thickness(0,0,0,12);content.Children.Add(edit);}
  }
  Border ThemeSwatch(ThemePalette palette){
   var fill=new LinearGradientBrush(ThemePalette.Parse(palette.Start),ThemePalette.Parse(palette.End),45);fill.Freeze();
   var sample=new Border{CornerRadius=new CornerRadius(7),Background=fill,Padding=new Thickness(3),Child=new Border{Height=3,CornerRadius=new CornerRadius(1.5),VerticalAlignment=VerticalAlignment.Bottom,Background=Brush(palette.Accent)}};
   return new Border{Width=29,Height=29,CornerRadius=new CornerRadius(9),Background=Brush("#8995A3"),Padding=new Thickness(1.5),Child=sample,Margin=new Thickness(0,0,9,0),ToolTip=palette.Start+" → "+palette.End};
  }
  sealed class DialogOwner : Forms.IWin32Window {public IntPtr Handle{get;private set;}public DialogOwner(Window w){Handle=new WindowInteropHelper(w).Handle;}}
  void OpenThemeEditor(){
   if(themeEditorOpen)return;themeEditorOpen=true;
   var dialog=new Window{Title="Özel tema",Owner=this,Width=440,SizeToContent=SizeToContent.Height,MaxHeight=selected.Bounds.Height/WindowDpi-30,WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,WindowStartupLocation=WindowStartupLocation.CenterOwner,Topmost=true,FontFamily=FontFamily,FontSize=13};
   themeEditor=dialog;
   dialog.Resources.MergedDictionaries.Add(Resources);
   var panel=new StackPanel{Margin=new Thickness(22)};var frame=new Border{CornerRadius=new CornerRadius(22),Child=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}};frame.SetResourceReference(Border.BackgroundProperty,"Popup");dialog.Content=frame;
   var heading=new DockPanel();var close=IconButton("Düzenleyiciyi kapat","close",delegate{dialog.Close();});close.Width=36;close.Height=36;close.Background=Brushes.Transparent;DockPanel.SetDock(close,Dock.Right);heading.Children.Add(close);var title=Label("Kendi renklerin",22,true);title.VerticalAlignment=VerticalAlignment.Center;heading.Children.Add(title);panel.Children.Add(heading);
   var hint=Label("İki zemin rengi ve bir vurgu seç.",12,false,"Muted");hint.Margin=new Thickness(0,5,0,18);panel.Children.Add(hint);
   var sampleText=new TextBlock{Text="12:48",FontSize=34,FontFamily=new FontFamily("Bahnschrift"),HorizontalAlignment=HorizontalAlignment.Center};
   var sampleDate=new TextBlock{Text="22 Eki Per  ·  18°",FontSize=14,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,6,0,0)};
   var samplePanel=new StackPanel{VerticalAlignment=VerticalAlignment.Center};samplePanel.Children.Add(sampleText);samplePanel.Children.Add(sampleDate);
   var sample=new Border{Height=112,CornerRadius=new CornerRadius(17),Child=samplePanel,Margin=new Thickness(0,0,0,16)};panel.Children.Add(sample);
   var inputs=new List<TextBox>();var swatches=new List<Button>();var error=Label("",11,false,"Muted");error.TextWrapping=TextWrapping.Wrap;error.Margin=new Thickness(0,4,0,14);
   Button apply=null;Action update=null;
   foreach(var entry in new[]{new[]{"Zemin",Preferences.CustomStart},new[]{"Geçiş rengi",Preferences.CustomEnd},new[]{"Vurgu",Preferences.CustomAccent}}){
    var row=new Grid{Margin=new Thickness(0,0,0,10)};row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(112)});row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(48)});
    var label=Label(entry[0],13);label.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(label);
    var input=new TextBox{Text=entry[1],MaxLength=7,Margin=new Thickness(0,0,8,0),FontFamily=new FontFamily("Consolas")};AutomationProperties.SetName(input,entry[0]+" renk kodu");Grid.SetColumn(input,1);row.Children.Add(input);inputs.Add(input);
    var swatch=MakeButton(entry[0]+" rengini seç","",delegate{using(var picker=new Forms.ColorDialog{FullOpen=true}){if(ThemePalette.ValidHex(input.Text)){var c=ThemePalette.Parse(input.Text);picker.Color=System.Drawing.Color.FromArgb(c.R,c.G,c.B);}if(picker.ShowDialog(new DialogOwner(dialog))==Forms.DialogResult.OK)input.Text=String.Format("#{0:X2}{1:X2}{2:X2}",picker.Color.R,picker.Color.G,picker.Color.B);}});swatch.Padding=new Thickness(0);Grid.SetColumn(swatch,2);row.Children.Add(swatch);swatches.Add(swatch);input.TextChanged+=delegate{if(update!=null)update();};panel.Children.Add(row);
   }
   panel.Children.Add(error);var actions=new UniformGrid{Columns=2};var cancel=MakeButton("Vazgeç","Vazgeç",delegate{dialog.Close();});cancel.Margin=new Thickness(0,0,5,0);actions.Children.Add(cancel);
   apply=MakeButton("Özel temayı uygula","Uygula",delegate{Preferences.CustomStart=inputs[0].Text.ToUpperInvariant();Preferences.CustomEnd=inputs[1].Text.ToUpperInvariant();Preferences.CustomAccent=inputs[2].Text.ToUpperInvariant();Preferences.Theme="Özel";ApplyTheme();ApplyMaterial();Save();dialog.Close();RenderRoute(false);});apply.Margin=new Thickness(5,0,0,0);actions.Children.Add(apply);panel.Children.Add(actions);
   update=delegate{bool valid=inputs.All(x=>ThemePalette.ValidHex(x.Text));for(int i=0;i<inputs.Count;i++)if(ThemePalette.ValidHex(inputs[i].Text))swatches[i].Background=Brush(inputs[i].Text);apply.IsEnabled=false;if(!valid){error.Text="Renk kodunu #A1B2C3 biçiminde yaz.";return;}var p=new ThemePalette("Özel",inputs[0].Text,inputs[1].Text,inputs[2].Text);sample.Background=new LinearGradientBrush(ThemePalette.Parse(p.Start),ThemePalette.Parse(p.End),45);sampleText.Foreground=sampleDate.Foreground=Brush(p.Light?"#282C31":"#EFF2F5");apply.IsEnabled=p.Readable;error.Text=p.Readable?"Önizleme hazır. Şeffaflık ayarın ayrıca korunur.":"Yazıların okunması için iki zemin rengini benzer açıklıkta seç.";};
   update();dialog.PreviewKeyDown+=delegate(object sender,System.Windows.Input.KeyEventArgs e){if(e.Key==System.Windows.Input.Key.Escape){dialog.Close();e.Handled=true;}};
   try{dialog.ShowDialog();}finally{themeEditorOpen=false;themeEditor=null;if(!testing&&!IsMouseOver)SetHeaderHovered(false);}
  }
 }
}
