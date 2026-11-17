using System;
using Ada;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using static Veilix.UiLanguage;

namespace Veilix;
public sealed partial class MainWindow {
 Button sectionMenuButton;
 Grid sectionNavigation;
 Button previousSectionButton,nextSectionButton,settingsSectionButton;
 readonly SolidColorBrush sectionChevronBrush=new();
 readonly RadioMenuFlyoutItem[] sectionMenuItems=new RadioMenuFlyoutItem[Routes.Length];
 bool sectionMenuOpen;

 Button BuildSectionMenu(){
  var menu=new MenuFlyout{Placement=FlyoutPlacementMode.Bottom,ShouldConstrainToRootBounds=false};
  for(int i=0;i<Routes.Length;i++){
   int index=i;
   var item=new RadioMenuFlyoutItem{Text=L(Names[i]),GroupName="VeilixSections"};
   item.Click+=(_,_)=>{
    int from=Array.IndexOf(Routes,route);
    if(from!=index)Navigate(Routes[index],index<from?-1:1);
   };
   sectionMenuItems[i]=item;
   menu.Items.Add(item);
  }
  menu.Opened+=(_,_)=>sectionMenuOpen=true;
  menu.Closed+=(_,_)=>sectionMenuOpen=false;
  TextStyle(pageTitle,13,true);
  pageTitle.VerticalAlignment=VerticalAlignment.Center;
  var glyph=Icons.Get("chevron",12,sectionChevronBrush);
  var label=new Grid{ColumnSpacing=6};
  label.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
  label.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
  label.Children.Add(pageTitle);
  Grid.SetColumn(glyph,1);
  label.Children.Add(glyph);
  sectionMenuButton=new Button{
   Content=label,Flyout=menu,Height=32,MinWidth=88,MaxWidth=194,Padding=new Thickness(10,0,8,0),
   CornerRadius=new CornerRadius(16),HorizontalAlignment=HorizontalAlignment.Center,
   HorizontalContentAlignment=HorizontalAlignment.Center
  };
  ToolTipService.SetToolTip(sectionMenuButton,L("Bölüm seç"));
  return sectionMenuButton;
 }

 void UpdateSectionMenu(int selected){
  for(int i=0;i<sectionMenuItems.Length;i++)sectionMenuItems[i].IsChecked=i==selected;
  AutomationProperties.SetName(sectionMenuButton,Format("Bölüm seç · {0}",pageTitle.Text));
 }

 void ApplySectionMenuTheme(){
  ApplyControlChrome(sectionMenuButton);
  sectionChevronBrush.Color=((SolidColorBrush)TextBrush(true)).Color;
  previousSectionButton.Content=Icons.Get("arrow-left",19,IconBrush("arrow-left"));
  nextSectionButton.Content=Icons.Get("arrow-right",19,IconBrush("arrow-right"));
  settingsSectionButton.Content=Icons.Get("settings",19,IconBrush("settings"));
  UpdateNotificationBell();
 }
 void ApplyNavigationSurface(ThemePalette palette){
  bool protectContrast=palette.Light&&prefs.Material=="Şeffaf";
  sectionNavigation.Margin=new Thickness(protectContrast?7:14,3,protectContrast?7:14,7);
  sectionNavigation.Background=protectContrast
   ?B(ThemePalette.Mix(palette.End,"#242C38",.12))
   :B("#32101822");
 }
 void RefreshNavigationLanguage(){
  for(int i=0;i<sectionMenuItems.Length;i++)sectionMenuItems[i].Text=L(Names[i]);
  ToolTipService.SetToolTip(sectionMenuButton,L("Bölüm seç"));
  SetIconLabel(previousSectionButton,L("Önceki sayfa"));
  SetIconLabel(nextSectionButton,L("Sonraki sayfa"));
  SetIconLabel(settingsSectionButton,L("Ayarlar"));
  SetIconLabel(notificationBell,L("Bildirimler"));
  UpdateNotificationBell();
 }
}
