// GPL-3.0-or-later. Interactive regressions exercised only with --self-test.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Ada {
 public sealed partial class AdaWindow {
  static IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject {for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T)yield return (T)child;foreach(var nested in Descendants<T>(child))yield return nested;}}
  async Task CaptureWindow(Window window,string output,string name){window.UpdateLayout();await Task.Delay(100);double dpi=PresentationSource.FromVisual(window).CompositionTarget.TransformToDevice.M11;var bitmap=new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth*dpi),(int)Math.Ceiling(window.ActualHeight*dpi),96*dpi,96*dpi,PixelFormats.Pbgra32);bitmap.Render(window);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(Path.Combine(output,name+".png")))png.Save(file);}
  async Task CheckNewFeatures(string output,List<string> report){
   Preferences.ReducedMotion=true;Preferences.ShowClock=true;SetHeaderHovered(true);BuildHeader();Navigate("");await Capture(output,"header-new");
   if(clock.FontSize<32||date.FontSize<14||header.ActualHeight<HeaderHeight)throw new Exception("Header typography is too small.");
   double weatherY=weatherButton.TranslatePoint(new Point(0,weatherButton.ActualHeight/2),header).Y,dateY=date.TranslatePoint(new Point(0,date.ActualHeight/2),header).Y;
   if(Math.Abs(weatherY-dateY)>1)throw new Exception("Weather/date are not on one line.");
   if(Descendants<Button>(header).Any(b=>AutomationProperties.GetName(b)=="Ayarlar"))throw new Exception("Header settings button remained.");
   Preferences.ShowClock=false;BuildHeader();Navigate("");await Capture(output,"header-clock-hidden");if(clock.Visibility!=Visibility.Collapsed||header.ColumnDefinitions[1].ActualWidth!=104||audioBars.Visibility!=Visibility.Visible)throw new Exception("Clock hiding did not reveal the audio meter.");
   Preferences.ShowClock=true;BuildHeader();Navigate("");report.Add("Header: 32px clock; readable date; single-line weather/date; hidden clock slot contains audio meter; no settings icon");
   weatherButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await WaitForReveal();await Capture(output,"weather-details");
   if(route!="weather")throw new Exception("Weather button did not open details.");
   if(Descendants<Button>(scroll).Any(b=>new[]{"Kontroller","Medya","Ayarlar"}.Contains(AutomationProperties.GetName(b))))throw new Exception("Weather detail added navigation tabs.");
   if(weather.Details==null||weather.Details.Hours.Count!=6)throw new Exception("Detailed live weather data unavailable.");
   report.Add("Weather details: feels-like="+weather.Details.FeelsLike+"; humidity="+weather.Details.Humidity+"; wind="+weather.Details.Wind+"; high="+weather.Details.High+"; low="+weather.Details.Low+"; hourly entries="+weather.Details.Hours.Count);
   foreach(var palette in ThemePalette.Presets){if(!palette.Readable)throw new Exception("Unreadable palette: "+palette.Name);Preferences.Theme=palette.Name;ApplyTheme();var text=((SolidColorBrush)Resources["Text"]).Color.ToString();var muted=((SolidColorBrush)Resources["Muted"]).Color.ToString();var tile=((SolidColorBrush)Resources["Tile"]).Color.ToString();if(ThemePalette.Contrast(text,tile)<4.5||ThemePalette.Contrast(muted,tile)<4.5)throw new Exception("Palette text contrast failed: "+palette.Name);}
   Preferences.Theme="Bordo";ApplyTheme();settingsPage="Görünüm";Navigate("settings");await Capture(output,"settings-palette-bordo");
   Preferences.Theme="Gece Mavisi";ApplyTheme();RenderRoute(false);await Capture(output,"settings-palette-dark");
   var editorOperation=Dispatcher.BeginInvoke(new Action(OpenThemeEditor));var deadline=DateTime.UtcNow.AddSeconds(3);while((themeEditor==null||!themeEditor.IsVisible)&&DateTime.UtcNow<deadline)await Task.Delay(30);
   if(themeEditor==null||!themeEditor.IsVisible)throw new Exception("Custom theme editor did not open.");
   var editor=themeEditor;try{
    var inputs=Descendants<TextBox>(editor).ToArray();var apply=Descendants<Button>(editor).First(b=>AutomationProperties.GetName(b)=="Özel temayı uygula");
    inputs[0].Text="#GGGGGG";if(apply.IsEnabled)throw new Exception("Invalid custom color was accepted.");
    inputs[0].Text="#202630";inputs[1].Text="#384552";inputs[2].Text="#A5C7C5";if(!apply.IsEnabled)throw new Exception("Valid custom palette was rejected.");
    await CaptureWindow(editor,output,"custom-theme-editor");apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
   }finally{if(editor.IsVisible)editor.Close();}
   await editorOperation.Task;await Task.Delay(100);if(Preferences.Theme!="Özel"||themeEditorOpen||route!="settings")throw new Exception("Custom palette apply lost settings state.");
   var serialized=new JavaScriptSerializer().Serialize(Preferences);var restored=new JavaScriptSerializer().Deserialize<Settings>(serialized);restored.Validate();if(restored.Theme!="Özel"||restored.CustomStart!="#202630"||!restored.ShowClock)throw new Exception("New preferences did not round-trip.");
   report.Add("Themes: "+ThemePalette.Presets.Length+" readable presets; custom editor invalid/valid hex + apply; settings serialization passed");
   foreach(var display in displays){selected=display;Preferences.DisplayId=display.Id;foreach(double scale in new[]{.85,1.0,1.2}){
    Preferences.Scale=scale;Preferences.ReducedMotion=true;Navigate("");await Task.Delay(150);Preferences.ReducedMotion=false;Navigate("media");await Task.Delay(60);double logicalWidth=main.Width,renderScale=((ScaleTransform)main.LayoutTransform).ScaleX;await WaitForReveal();
    if(logicalWidth!=ExpandedWidth||main.Width!=logicalWidth||Math.Abs(((ScaleTransform)main.LayoutTransform).ScaleX-renderScale)>.01||main.LayoutTransform.HasAnimatedProperties)throw new Exception("Layout scaled or reflowed during open.");
    report.Add("Open layout "+display.Bounds.Width+" @ "+scale+": logical width=500; scale stable="+renderScale.ToString("F3"));
   }}
   Preferences.Scale=1;Preferences.Theme="Grafit";Preferences.ReducedMotion=true;ApplyTheme();Navigate("");await CheckHoverFeatures(output,report);await CheckDailyUse(output,report);await CheckMediaPolish(output,report);
  }
 }
}
