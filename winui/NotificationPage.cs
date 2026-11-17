using System;
using System.Linq;
using Ada;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using static Veilix.UiLanguage;
namespace Veilix;
public sealed partial class MainWindow {
 static string NoticeDetail(string kind,string detail)=>kind?.StartsWith("task-",StringComparison.Ordinal)==true?detail:Status(detail);
 internal static string NoticeBalloonText(Ada.UtilityNotice notice)=>!String.IsNullOrWhiteSpace(notice.Detail)?NoticeDetail(notice.Kind,notice.Detail):!String.IsNullOrWhiteSpace(notice.Title)?Status(notice.Title):L("Veilix bildirimi");
 DispatcherQueueTimer noticeSave;Button notificationBell;
 void SaveNotices(){if(testing)return;noticeSave??=Timer(1000,()=>{noticeSave.Stop();tools.Save();if(route=="notifications")Render();});noticeSave.Stop();noticeSave.Start();}
 void UpdateNotificationBell(){if(notificationBell==null)return;int unread=tools.Data.Notices.Count(n=>!n.Read);ToolTipService.SetToolTip(notificationBell,unread>0?Format("Bildirimler · {0} yeni",unread):L("Bildirimler"));notificationBell.Content=Icons.Get("bell",19,IconBrush("bell"));}
 void RenderNotifications(){
  tools.NormalizeNotices();Heading(L("Bildirimler"),L("Bugünkü hatırlatmalar ve sistem değişiklikleri."));page.Children.Add(Button(L("Bildirim ayarları"),()=>{settingsTab=L("Bildirimler");Navigate("settings");},"controls"));
  if(tools.Data.Notices.Count==0){page.Children.Add(Card(Text(L("Henüz bir bildirim yok."),15,false,true)));return;}
  page.Children.Add(Button(L("Geçmişi temizle"),ClearNoticeHistory,"delete"));
  // Only the visible page of history creates controls; the stored list is bounded.
  int count=Math.Min(noticePageSize,tools.Data.Notices.Count);bool marked=tools.Data.Notices.Take(count).Any(n=>!n.Read);foreach(var entry in tools.Data.Notices.Take(count)){
   var text=new StackPanel{Spacing=5};text.Children.Add(Text(Status(entry.Title),14,true));if(!String.IsNullOrWhiteSpace(entry.Detail))text.Children.Add(Text(NoticeDetail(entry.Kind,entry.Detail),12,false,true));text.Children.Add(Text(new DateTimeOffset(Math.Clamp(entry.AtUtc,0,DateTimeOffset.MaxValue.UtcTicks),TimeSpan.Zero).ToLocalTime().ToString("dd MMM · HH:mm",tr),11,false,true));
   var row=new Grid{ColumnSpacing=12};row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});row.ColumnDefinitions.Add(new ColumnDefinition());row.Children.Add(Icons.Get(entry.Icon??"bell",23,IconBrush(entry.Icon??"bell")));Grid.SetColumn(text,1);row.Children.Add(text);
   var card=Button("",()=>{if(!String.IsNullOrEmpty(entry.Route))Navigate(entry.Route);});card.Content=row;card.HorizontalContentAlignment=HorizontalAlignment.Stretch;page.Children.Add(card);entry.Read=true;
  }
  if(count<tools.Data.Notices.Count)page.Children.Add(Button(L("Öncekileri göster"),()=>{noticePageSize+=15;Render();},"clock"));if(marked)SaveNotices();UpdateNotificationBell();
 }
 int noticePageSize=15;
 async void ClearNoticeHistory(){var dialog=ConfirmDialog(L("Bildirim geçmişi temizlensin mi?"),L("Kaydedilen bildirimler kaldırılacak."),L("Temizle"));if(await Confirm(dialog)){tools.Data.Notices.Clear();tools.Save();UpdateNotificationBell();if(route=="notifications")Render();}}
 void RenderNoticeSettings(){
  Heading(L("Bildirim tercihleri"));Toggle(L("Adada bildirim göster"),tools.Data.NoticeBanners,v=>{tools.Data.NoticeBanners=v;tools.Save();if(!v){notice=null;noticeTimer.Stop();UpdateHeader();}});Toggle(L("Sistem bildirimleri"),tools.Data.SystemNotices,v=>{tools.Data.SystemNotices=v;tools.Save();});
  Toggle(L("Ses değişiklikleri"),tools.Data.VolumeNotices,v=>{tools.Data.VolumeNotices=v;tools.Save();});Toggle(L("Klavye dili"),tools.Data.KeyboardNotices,v=>{tools.Data.KeyboardNotices=v;tools.Save();});Toggle(L("Bağlantı değişiklikleri"),tools.Data.NetworkNotices,v=>{tools.Data.NetworkNotices=v;tools.Save();});Toggle(L("Güç durumu"),tools.Data.PowerNotices,v=>{tools.Data.PowerNotices=v;tools.Save();});Toggle(L("Görev hatırlatmaları"),tools.Data.TaskNotices,v=>{tools.Data.TaskNotices=v;tools.Save();});Toggle(L("Zamanlayıcı bildirimleri"),tools.Data.TimerNotices,v=>{tools.Data.TimerNotices=v;tools.Save();});Toggle(L("Zamanlayıcı sesi"),tools.Data.TimerSound,v=>{tools.Data.TimerSound=v;tools.Save();});Toggle(L("Su hatırlatıcısı"),tools.Data.WaterEnabled,v=>{tools.Data.WaterEnabled=v;tools.ResetWaterSchedule();});Toggle(L("Su hatırlatıcı sesi"),tools.Data.WaterSound,v=>{tools.Data.WaterSound=v;tools.Save();});page.Children.Add(Button(L("Geçmişi aç"),()=>Navigate("notifications"),"bell"));
 }
 ContentDialog ConfirmDialog(string title,string message,string primary)=>new(){Title=title,Content=Text(message,14),PrimaryButtonText=primary,CloseButtonText=L("Vazgeç"),DefaultButton=ContentDialogButton.Close,XamlRoot=root.XamlRoot,RequestedTheme=root.RequestedTheme};
 async System.Threading.Tasks.Task<bool> Confirm(ContentDialog dialog){popupOpen=true;try{return await dialog.ShowAsync()==ContentDialogResult.Primary;}finally{popupOpen=false;}}
 async void ClearShelfConfirmed(){if(tools.Copying)return;var dialog=ConfirmDialog(L("Raf klasörü temizlensin mi?"),L("Belgeler / Veilix / Raf içindeki tüm kopyalar Geri Dönüşüm Kutusu'na taşınacak.\n\n")+UtilitySession.ShelfRoot,L("Temizle"));if(!await Confirm(dialog))return;try{var errors=await tools.ClearShelf();if(errors.Count>0)ShowNotice(new UtilityNotice{Kind="shelf",Title=L("Bazı dosyalar temizlenemedi"),Detail=errors[0],Icon="folder",Route="shelf"});if(route=="shelf")Render();}catch(Exception ex){ShowNotice(new UtilityNotice{Kind="shelf",Title=L("Raf temizlenemedi"),Detail=ex.Message,Icon="folder",Route="shelf"});}}
}
