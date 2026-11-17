using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Ada;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Veilix;

public sealed partial class MainWindow {
 async Task CheckLocalization(List<string> report,string folder){
  string language=prefs.Language,tab=settingsTab,theme=prefs.Theme;
  bool reduced=prefs.ReducedMotion;
  prefs.ReducedMotion=true;
  try{
   var json=new JavaScriptSerializer();
   var stored=json.Deserialize<Settings>(json.Serialize(new Settings{Language="en",Theme="İnci",Material="Şeffaf"}));
   Check(stored.Language=="en"&&stored.Theme=="İnci"&&stored.Material=="Şeffaf","Language persistence changed existing settings keys");
   Navigate("settings");
   var picker=page.Children.OfType<ComboBox>().Single(c=>Equals(c.Header,"Dil"));
   picker.SelectedIndex=1;
   Check(prefs.Language=="en"&&root.Language=="en-US"&&pageTitle.Text=="Settings","English selection did not refresh the active page");
   var englishTabs=page.Children.OfType<ComboBox>().Single(c=>Equals(c.Header,"Category"));
   Check(englishTabs.SelectedItem is ComboBoxItem{Tag:"Görünüm",Content:"Appearance"},"English settings options changed their saved key");
   string[] titles={"Now playing","Hydration","Quick controls","Bluetooth","Timers","Tasks","File shelf","Clipboard history"};
   for(int i=0;i<Routes.Length;i++){
    Navigate(Routes[i]);
    Check(pageTitle.Text==titles[i]&&sectionMenuItems[i].Text==titles[i]&&page.Children.Count>0,"English section is missing: "+Routes[i]);
   }
   Check(ThemePalette.Presets.All(p=>UiLanguage.L(p.Name)!=p.Name||p.Name is "Espresso" or "Mocha" or "Matcha"),"An English palette name is missing");
   Navigate("themes");
   var themeOptions=page.Children.OfType<ComboBox>().Single(c=>Equals(c.Header,"Show"));
   Check(themeOptions.Items.OfType<ComboBoxItem>().Any(i=>i.Tag.Equals("Açık")&&i.Content.Equals("Light")),"Light theme filter lost its context in English");
   Check(ParseDue("2026-11-27 18:30",out _)&&ParseDue("27.11.2026 18:30",out _),"English reminders rejected valid dates");
   Check(UiLanguage.WeatherCondition("Açık")=="Clear"&&UiLanguage.Status("Ses · %42")=="Volume · 42%"&&
    NoticeDetail("task-qa","Su zamanı")=="Su zamanı","Locale leaked into weather, system notifications or user-entered text");
   prefs.Theme="İnci";ApplyTheme();Navigate("media");root.UpdateLayout();
   Check(sectionMenuButton.BorderThickness.Left==1&&playButton.BorderThickness.Left==1&&playButton.CornerRadius.TopLeft==23,"Media controls lost their light-theme outlines");
   var arrow=(Microsoft.UI.Xaml.Shapes.Path)((Grid)((Viewbox)previousSectionButton.Content).Child).Children[0];
   Check(((Microsoft.UI.Xaml.Media.SolidColorBrush)arrow.Fill).Color==C(SemanticIcons.Color("arrow-left",true)),"Navigation kept its dark-theme icon on a light surface");
   foreach(var p in ThemePalette.Presets.Where(x=>x.Light)){
    string surfaceColor=ThemePalette.Mix(p.Start,"#101820",.06);
    string actionColor=ThemePalette.Mix(p.Start,p.Accent,.26);
    Check(ThemePalette.Contrast("#394756",surfaceColor)>=4.5&&ThemePalette.Contrast("#242C38",actionColor)>=4.5,"Light control text contrast: "+p.Name);
   }
   await RenderGalleryPreview(System.IO.Path.Combine(folder,"media-english-light.png"));
   Navigate("");SetHidden(true);root.UpdateLayout();
   Check(edgeGrip.Visibility==Visibility.Visible&&edgeGrip.Child is Border inner&&inner.ActualWidth>0&&inner.Background!=null,"Hidden edge grip is missing its inner highlight");
   Check(edgeGrip.Width==32&&edgeGrip.Height<3&&shoulderLayer.Opacity==0,"Hidden edge grip or shoulder transition regressed");
   SetHidden(false);
   Check(edgeGrip.Visibility==Visibility.Collapsed,"Hidden grip remained after reveal");
   Check(shoulderLayer.Opacity==1&&(!SharedGlass||unifiedGlass.CurvesMatchOpacity(1)),"Tint and glass shoulders did not reveal together");
   positionOffset=-7;
   UpdateEdgePresentation();
   Check(shoulderLayer.Opacity==1&&(!SharedGlass||unifiedGlass.CurvesMatchOpacity(1)),"The moving edges disappeared before leaving the display");
   positionOffset=0;
   UpdateEdgePresentation();
   Navigate("settings");
   page.Children.OfType<ComboBox>().Single(c=>Equals(c.Header,"Language")).SelectedIndex=0;
   Check(prefs.Language=="tr"&&root.Language=="tr-TR"&&pageTitle.Text=="Ayarlar","Switching back to Turkish failed");
  }finally{
   prefs.Language=language;settingsTab=tab;prefs.Theme=theme;prefs.ReducedMotion=reduced;ApplyLanguage();ApplyTheme();Navigate("");
  }
  report.Add("Localization: Turkish/English switch, stable settings keys, eight section titles, theme names, notices, dates, light control contrast and two-tone hidden grip.");
 }
}
