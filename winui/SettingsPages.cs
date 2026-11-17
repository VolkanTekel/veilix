using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ada;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using static Veilix.UiLanguage;
namespace Veilix;
public sealed partial class MainWindow {
 void RenderSettings(){
  AddLanguageSetting();
  Choice("Kategori",new[]{"Ekran","Görünüm","Medya","Hava","Bildirimler","Sistem"},settingsTab,v=>{settingsTab=v;Render();});
  switch(settingsTab){case "Ekran":
   var displaysBox=new ComboBox{Header=L("Gösterilecek ekran"),HorizontalAlignment=HorizontalAlignment.Stretch};foreach(var d in displays)displaysBox.Items.Add(new ComboBoxItem{Content=d.ToString(),Tag=d});displaysBox.SelectedItem=displaysBox.Items.OfType<ComboBoxItem>().FirstOrDefault(i=>i.Tag==monitor);displaysBox.SelectionChanged+=(_,e)=>{if(displaysBox.SelectedItem is ComboBoxItem{Tag:Display d}){monitor=d;prefs.DisplayId=d.Id;Save();DisplayChanged();}};page.Children.Add(displaysBox);
   page.Children.Add(Number(L("Panel boyutu"),prefs.Scale,.85,1.2,v=>{prefs.Scale=v;Save();DisplayChanged();}));page.Children.Add(Text(L("Panel ekranın üst kenarında ve yatay merkezinde durur. Sayfalar ekranın yüksekliğine göre açılır."),13,false,true));break;
  case "Görünüm":
   page.Children.Add(Button(L("Küçük paneli düzenle"),()=>Navigate("compact-layout"),"controls"));
   var themeButton=Button(L("Renk temaları · ")+L(prefs.Theme),()=>Navigate("themes"),"palette");page.Children.Add(themeButton);page.Children.Add(Text(L("62 hazır palet veya kendi renk geçişin."),13,false,true));
   Choice(L("Yüzey"),new[]{"Cam","Şeffaf","Mika","Düz"},prefs.Material,v=>{prefs.Material=v;ApplyTheme();Save();});Level(L("Şeffaflık"),(1-prefs.Tint)*100,v=>{prefs.Tint=Math.Clamp(1-v/100,.25,1);ApplyTheme();Save();});Toggle(L("Renk geçişi"),prefs.Gradient,v=>{prefs.Gradient=v;ApplyTheme();Save();});Toggle(L("Üzerine gelince saati göster"),prefs.ShowClock,v=>{prefs.ShowClock=v;UpdateClock();Save();});Toggle(L("Hareketi azalt"),prefs.ReducedMotion,SetReducedMotion);Choice(L("Animasyon sınırı"),new[]{"60 FPS","90 FPS","120 FPS"},prefs.FrameLimit+" FPS",v=>{prefs.FrameLimit=int.Parse(v.Split(' ')[0]);peakTimer.Interval=TimeSpan.FromMilliseconds(1000.0/Math.Min(prefs.FrameLimit,monitor.Hz));Save();});break;
  case "Medya":Choice(L("Oynatıcı görünümü"),new[]{"Dengeli","Kapak","Minimal"},prefs.MediaLayout,v=>{prefs.MediaLayout=v;Save();});Toggle(L("Uzun şarkı adlarını kaydır"),prefs.Marquee,v=>{prefs.Marquee=v;Save();});Toggle(L("Diğer uygulamalardaki medyayı göster"),prefs.OtherMediaSources,v=>{prefs.OtherMediaSources=v;media.SetOtherSources(v);Save();});page.Children.Add(Text(L("Bu seçenek kapalıyken yalnızca Spotify kullanılır."),13,false,true));break;
  case "Hava":RenderWeatherSettings();break;
  case "Bildirimler":RenderNoticeSettings();break;case "Sistem":Toggle(L("Üstüne pencere gelince gizlen"),prefs.AutoHideOverlap,v=>{prefs.AutoHideOverlap=v;Save();CheckOcclusion();});Toggle(L("Tam ekranda gizlen"),prefs.HideFullscreen,v=>{prefs.HideFullscreen=v;Save();CheckOcclusion();});Toggle(L("Örtüşme bitince gizli kal"),prefs.KeepHiddenAfterOverlap,v=>{prefs.KeepHiddenAfterOverlap=v;Save();CheckOcclusion();});Toggle(L("Sistem değişikliklerini göster"),tools.Data.SystemNotices,v=>{tools.Data.SystemNotices=v;tools.Save();});Toggle(L("Windows ile başlat"),StartupEnabled,v=>SetStartup(v));page.Children.Add(Button(L("Veilix'i kapat"),Close,"close"));break;
  }
 }
 bool StartupEnabled=>StartupRegistration.Enabled;
 void SetStartup(bool value){try{StartupRegistration.SetEnabled(value);}catch(Exception ex){ShowNotice(new UtilityNotice{Kind="startup",Title=L("Başlangıç ayarı değiştirilemedi"),Detail=ex.Message,Icon="system"});Render();}}
 string themeFilter="Tümü";readonly List<Button> themeCards=new();
 void RenderThemes(){
  Heading(L("Renkler"),L("50 koyu, 12 açık palet. Renk örnekleri şeffaflıktan etkilenmez."));
  page.Children.Add(Button(L("Kendi temamı oluştur"),()=>{customDraft=null;Navigate("custom");},"palette"));
  Choice("Göster",new[]{"Tümü","Koyu","Açık"},themeFilter,v=>{themeFilter=v;Render();});
  themeCards.Clear();double cardWidth=Math.Floor((width-36-5/host.Dpi-16)*host.Dpi/3)/host.Dpi;var grid=new Grid{ColumnSpacing=8,RowSpacing=8,RequestedTheme=ElementTheme.Dark,HorizontalAlignment=HorizontalAlignment.Center};for(int i=0;i<3;i++)grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(cardWidth)});
  int index=0;foreach(var p in ThemePalette.Presets.Where(p=>themeFilter=="Tümü"||(themeFilter=="Açık")==p.Light)){
   int row=index/3,col=index%3;if(col==0)grid.RowDefinitions.Add(new RowDefinition{Height=new GridLength(104)});
   var swatch=new Border{Height=46,HorizontalAlignment=HorizontalAlignment.Stretch,CornerRadius=new CornerRadius(9),Background=PaletteBrush(p),BorderBrush=B("#7B8899"),BorderThickness=new Thickness(1)};
   var accent=new Border{Height=3,CornerRadius=new CornerRadius(1.5),Margin=new Thickness(8,0,8,5),VerticalAlignment=VerticalAlignment.Bottom,Background=B(p.Accent)};swatch.Child=accent;
   var name=Text(L(p.Name),14,true);name.Foreground=B("#F5F7FB");name.TextWrapping=TextWrapping.NoWrap;name.HorizontalAlignment=HorizontalAlignment.Stretch;name.TextAlignment=TextAlignment.Center;
   var stack=new Grid{RowSpacing=8,HorizontalAlignment=HorizontalAlignment.Stretch};stack.RowDefinitions.Add(new RowDefinition{Height=new GridLength(46)});stack.RowDefinitions.Add(new RowDefinition{Height=new GridLength(22)});stack.Children.Add(swatch);Grid.SetRow(name,1);stack.Children.Add(name);
   var selected=prefs.Theme==p.Name;var b=new Button{Content=stack,Width=cardWidth,Height=104,Padding=new Thickness(8),HorizontalAlignment=HorizontalAlignment.Stretch,HorizontalContentAlignment=HorizontalAlignment.Stretch,VerticalContentAlignment=VerticalAlignment.Center,Background=B("#252C35"),Foreground=B("#F5F7FB"),BorderThickness=new Thickness(2),BorderBrush=B(selected?"#99D4FF":"#3D4857"),CornerRadius=new CornerRadius(13)};
   Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(b,L(p.Name)+(p.Light?L(" · Açık tema"):L(" · Koyu tema"))+(selected?L(" · Seçili"):""));ToolTipService.SetToolTip(b,L(p.Name)+(selected?L(" · Seçili"):""));
   b.Click+=(_,e)=>{double offset=scroller.VerticalOffset;prefs.Theme=p.Name;ApplyTheme();Save();Render();scroller.ChangeView(null,offset,null,true);};Grid.SetRow(b,row);Grid.SetColumn(b,col);grid.Children.Add(b);themeCards.Add(b);index++;
  }page.Children.Add(grid);
 }
 static LinearGradientBrush PaletteBrush(ThemePalette p){var b=new LinearGradientBrush{StartPoint=new Point(0,0),EndPoint=new Point(1,1)};b.GradientStops.Add(new GradientStop{Color=C(p.Start),Offset=0});b.GradientStops.Add(new GradientStop{Color=C(p.End),Offset=1});return b;}
 List<ThemeStop> customDraft;string customAccent;int selectedStop;Border gradientPreview;TextBlock customError;Button applyCustom;
 void RenderCustomTheme(){
  customDraft??=GradientModel.Normalize(prefs.CustomStops,prefs.CustomStart,prefs.CustomEnd);customAccent??=prefs.CustomAccent;selectedStop=Math.Clamp(selectedStop,0,customDraft.Count-1);Heading(L("Kendi renklerin"),L("Geçişe istediğin kadar renk ekle."));gradientPreview=new Border{Height=90,CornerRadius=new CornerRadius(16),Child=Text("Veilix   ·   19:00",25,true),Padding=new Thickness(16)};page.Children.Add(gradientPreview);
  var stopsPanel=new StackPanel{Spacing=7};page.Children.Add(stopsPanel);for(int i=0;i<customDraft.Count;i++){int n=i;var s=customDraft[i];var swatch=new Border{Width=26,Height=26,Background=B(s.Color),CornerRadius=new CornerRadius(7),BorderBrush=B("#FF98A2AF"),BorderThickness=new Thickness(1)};var label=Text(s.Color+"   ·   %"+Math.Round(s.Offset*100),13,i==selectedStop);var row=Row(swatch,label);var choose=Button("",()=>{selectedStop=n;Render();});choose.Content=row;stopsPanel.Children.Add(choose);}
  page.Children.Add(Row(Button(L("Renk ekle"),()=>{double offset=customDraft.Count==0?.5:Math.Min(.99,customDraft[selectedStop].Offset+.1);customDraft.Add(new ThemeStop(customDraft[selectedStop].Color,offset));selectedStop=customDraft.Count-1;Render();},"add"),Button(L("Seçili rengi kaldır"),()=>{if(customDraft.Count<=2)return;customDraft.RemoveAt(selectedStop);selectedStop=Math.Min(selectedStop,customDraft.Count-1);Render();},"delete")));
  var colorButton=Button(L("Seçili rengin tonunu değiştir"),()=>{},"palette");var colorFlyout=new Flyout();colorFlyout.Opening+=(_,e)=>{if(colorFlyout.Content!=null)return;var picker=new ColorPicker{Color=C(customDraft[selectedStop].Color),IsAlphaEnabled=false,IsColorChannelTextInputVisible=true,IsHexInputVisible=true,IsColorSliderVisible=true,IsColorSpectrumVisible=true,IsMoreButtonVisible=false,MaxWidth=width-50,HorizontalAlignment=HorizontalAlignment.Stretch};picker.ColorChanged+=(_,change)=>{customDraft[selectedStop].Color=$"#{change.NewColor.R:X2}{change.NewColor.G:X2}{change.NewColor.B:X2}";UpdateCustomPreview();};colorFlyout.Content=picker;};colorFlyout.Closed+=(_,e)=>colorFlyout.Content=null;colorButton.Flyout=colorFlyout;page.Children.Add(colorButton);var hex=new TextBox{Header=L("Seçili renk"),Text=customDraft[selectedStop].Color,MaxLength=7};hex.TextChanged+=(_,e)=>{if(ThemePalette.ValidHex(hex.Text)){customDraft[selectedStop].Color=hex.Text;UpdateCustomPreview();}};page.Children.Add(hex);var offset=new Slider{Header=L("Geçişteki konumu · %"),Minimum=0,Maximum=100,Value=customDraft[selectedStop].Offset*100};offset.ValueChanged+=(_,e)=>{customDraft[selectedStop].Offset=e.NewValue/100;UpdateCustomPreview();};page.Children.Add(offset);
  var accent=new TextBox{Header=L("Vurgu rengi"),Text=customAccent,MaxLength=7};accent.TextChanged+=(_,e)=>{customAccent=accent.Text;UpdateCustomPreview();};page.Children.Add(accent);customError=Text("",12,false,true);page.Children.Add(customError);applyCustom=Button(L("Temamı uygula"),()=>{var sorted=GradientModel.Normalize(customDraft,prefs.CustomStart,prefs.CustomEnd);prefs.CustomStops=sorted;prefs.CustomStart=sorted[0].Color;prefs.CustomEnd=sorted[^1].Color;prefs.CustomAccent=customAccent.ToUpperInvariant();prefs.Theme="Özel";ApplyTheme();Save();Navigate("themes");},"check");page.Children.Add(applyCustom);UpdateCustomPreview();
 }
 void UpdateCustomPreview(){if(gradientPreview==null)return;var brush=new LinearGradientBrush{StartPoint=new Point(0,0),EndPoint=new Point(1,1)};foreach(var s in customDraft.OrderBy(x=>x.Offset))brush.GradientStops.Add(new GradientStop{Color=C(s.Color),Offset=s.Offset});gradientPreview.Background=brush;bool darkText=customDraft.All(x=>ThemePalette.Contrast("#242C38",x.Color)>=4.5);bool lightText=customDraft.All(x=>ThemePalette.Contrast("#F1F4F8",x.Color)>=4.5);if(gradientPreview.Child is TextBlock sample)sample.Foreground=B(darkText?"#242C38":"#F1F4F8");bool valid=ThemePalette.ValidHex(customAccent)&&(darkText||lightText);if(applyCustom!=null)applyCustom.IsEnabled=valid;if(customError!=null)customError.Text=valid?customDraft.Count+L(" renk · geçiş hazır"):L("Yazıların okunması için zemin renklerini benzer açıklıkta seç; vurgu kodu #A1B2C3 biçiminde olmalı.");}
}




