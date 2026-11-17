using System;
using System.Collections.Generic;
using Ada;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;
using static Veilix.UiLanguage;
namespace Veilix;
public sealed partial class MainWindow {
 static Color C(string v){if(v.Length==9)return Color.FromArgb(Convert.ToByte(v.Substring(1,2),16),Convert.ToByte(v.Substring(3,2),16),Convert.ToByte(v.Substring(5,2),16),Convert.ToByte(v.Substring(7,2),16));return Color.FromArgb(255,Convert.ToByte(v.Substring(1,2),16),Convert.ToByte(v.Substring(3,2),16),Convert.ToByte(v.Substring(5,2),16));}
 static SolidColorBrush B(string value)=>new(C(value));
 void TextStyle(TextBlock t,double size,bool bold=false,bool muted=false){t.FontFamily=new FontFamily("Segoe UI Variable, Segoe UI");t.FontSize=size;t.FontWeight=bold?Microsoft.UI.Text.FontWeights.SemiBold:Microsoft.UI.Text.FontWeights.Normal;t.Foreground=TextBrush(muted);t.TextTrimming=TextTrimming.CharacterEllipsis;}
 TextBlock Text(string value,double size=14,bool bold=false,bool muted=false){var t=new TextBlock{Text=value,TextWrapping=TextWrapping.Wrap};TextStyle(t,size,bold,muted);return t;}
 Button Button(string label,Action action,string icon=null){var b=new Button{CornerRadius=new CornerRadius(11),Padding=new Thickness(13,9,13,9),HorizontalAlignment=HorizontalAlignment.Stretch,Background=Tile(),BorderThickness=new Thickness(0)};if(icon==null)b.Content=Text(label,13,true);else{var row=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};row.Children.Add(Icons.Get(icon,18,IconBrush(icon)));row.Children.Add(Text(label,13,true));b.Content=row;}if(ThemePalette.Get(prefs).Light)ApplyControlChrome(b);b.Click+=(_,e)=>action();return b;}
 Button IconButton(string label,string icon,Action action){var b=Button("",action);b.Content=Icons.Get(icon,19,IconBrush(icon));b.Width=b.Height=32;b.Padding=new Thickness(6);b.Background=B("#00000000");b.BorderThickness=new Thickness(0);SetIconLabel(b,label);return b;}
 static void SetIconLabel(Button button,string label){Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(button,label);ToolTipService.SetToolTip(button,label);}
 Brush TextBrush(bool muted=false){bool light=ThemePalette.Get(prefs).Light;return B(light?(muted?"#394756":"#242C38"):(muted?"#C4CDD8":"#F1F4F8"));}
 Brush Tile(){var p=ThemePalette.Get(prefs);return B(ThemePalette.Mix(p.Start,p.Light?"#101820":"#D4DFEB",p.Light?.06:.10));}
 Brush IconBrush(string icon)=>B(SemanticIcons.Color(icon,ThemePalette.Get(prefs).Light));
 Brush ControlOutline(ThemePalette palette)=>B(palette.Light?"#687687":ThemePalette.Mix(palette.Start,"#D4DFEB",.26));
 void ApplyControlChrome(Button button,bool primary=false){
  var palette=ThemePalette.Get(prefs);
  button.Background=primary?B(ThemePalette.Mix(palette.Start,palette.Accent,palette.Light?.26:.22)):Tile();
  button.BorderBrush=ControlOutline(palette);
  button.BorderThickness=new Thickness(1);
 }
 Border Card(UIElement content){
  var palette=ThemePalette.Get(prefs);
  return new Border{Child=content,CornerRadius=new CornerRadius(16),Background=Tile(),Padding=new Thickness(14),BorderBrush=palette.Light?ControlOutline(palette):null,BorderThickness=new Thickness(palette.Light?1:0)};
 }
 StackPanel Row(params UIElement[] items){var p=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};foreach(var x in items)p.Children.Add(x);return p;}
 void Heading(string title,string subtitle=null){page.Children.Add(Text(title,21,true));if(subtitle!=null)page.Children.Add(Text(subtitle,13,false,true));}
 void Toggle(string label,bool value,Action<bool> update){var toggle=new ToggleSwitch{Header=label,IsOn=value,OnContent=L("Açık"),OffContent=L("Kapalı")};toggle.Toggled+=(_,e)=>update(toggle.IsOn);page.Children.Add(toggle);}
 ComboBox Choice(string label,IEnumerable<string> values,string selected,Action<string> change){
  var c=new ComboBox{Header=L(label),HorizontalAlignment=HorizontalAlignment.Stretch};
  foreach(string value in values){
   var option=new ComboBoxItem{Tag=value,Content=Option(label,value)};
   c.Items.Add(option);
   if(value==selected)c.SelectedItem=option;
  }
  c.SelectionChanged+=(_,e)=>{if(c.SelectedItem is ComboBoxItem{Tag:string value})change(value);};
  page.Children.Add(c);
  return c;
 }
 Slider Level(string label,double value,Action<double> update,bool enabled=true){var s=new Slider{Header=label,Minimum=0,Maximum=100,Value=value,IsEnabled=enabled,StepFrequency=1,MinHeight=44};s.ValueChanged+=(_,e)=>update(e.NewValue);s.PointerWheelChanged+=(_,e)=>{s.Value=Math.Clamp(s.Value+(e.GetCurrentPoint(s).Properties.MouseWheelDelta>0?2:-2),0,100);e.Handled=true;};page.Children.Add(s);return s;}
 string appliedMaterial="",appliedPalette="";
 void ApplyTheme(){UpdateSharedGlass();var p=ThemePalette.Get(prefs);root.RequestedTheme=p.Light?ElementTheme.Light:ElementTheme.Dark;var brush=new LinearGradientBrush{MappingMode=BrushMappingMode.Absolute,StartPoint=new Point(0,0),EndPoint=new Point(width,surface.Height)};var stops=prefs.Theme=="Özel"?GradientModel.Normalize(prefs.CustomStops,p.Start,p.End):new List<ThemeStop>{new(p.Start,0),new(p.End,1)};foreach(var stop in stops){var c=C(prefs.Gradient?stop.Color:p.Start);c.A=(byte)Math.Round(255*(prefs.Material=="Düz"||(prefs.Material=="Cam"&&!SharedGlass)?1:p.Light?Math.Max(.82,prefs.Tint):prefs.Tint));brush.GradientStops.Add(new GradientStop{Color=c,Offset=stop.Offset});}tint.Background=brush;if(appliedMaterial!=prefs.Material){appliedMaterial=prefs.Material;glass.SystemBackdrop=prefs.Material=="Mika"?new MicaBackdrop():null;}glass.Visibility=prefs.Material=="Mika"?Visibility.Visible:Visibility.Collapsed;string signature=p.Name+p.Start+p.End+p.Accent;if(appliedPalette!=signature){appliedPalette=signature;root.Resources["SystemAccentColor"]=C(p.Accent);root.Resources["AccentFillColorDefaultBrush"]=B(p.Accent);root.Resources["AccentTextFillColorPrimaryBrush"]=B(p.Accent);root.Resources["SliderTrackValueFill"]=B(p.Accent);foreach(var b in bars)b.Fill=B(p.Accent);miniTitle.Foreground=date.Foreground=weatherText.Foreground=clock.Foreground=pageTitle.Foreground=TextBrush();ApplySectionMenuTheme();ApplyGripTheme(p);miniArtist.Foreground=TextBrush(true);if(weatherIconPlate!=null)weatherIconPlate.Background=B(p.Light?"#435466":"#00000000");}ApplyNavigationSurface(p);UpdateShoulders();ClipBodyBackdrop();UpdateHeader();}
}








