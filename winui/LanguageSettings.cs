using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Veilix.UiLanguage;

namespace Veilix;

public sealed partial class MainWindow {
 void AddLanguageSetting(){
  var language=new ComboBox{Header=L("Dil"),HorizontalAlignment=HorizontalAlignment.Stretch};
  language.Items.Add("Türkçe");
  language.Items.Add("English");
  language.SelectedIndex=English?1:0;
  language.SelectionChanged+=(_,_)=>SetLanguage(language.SelectedIndex==1?"en":"tr");
  page.Children.Add(language);
 }

 void SetLanguage(string language){
  prefs.Language=language;
  ApplyLanguage();
  Save();
  Render();
  UpdateHeader();
 }

 void ApplyLanguage(){
  Select(prefs.Language);
  root.Language=Culture.Name;
  if(sectionMenuButton==null)return;
  RefreshNavigationLanguage();
  if(tray!=null)UpdateTrayMenu();
  batterySignature=null;
  UpdateBattery();
 }
}
