// GPL-3.0-or-later. Short notices and a single low-frequency utility clock.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
namespace Ada {
 public sealed partial class AdaWindow {
  readonly DispatcherTimer utilityTick=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromSeconds(1)};
  readonly DispatcherTimer noticeExpiry=new DispatcherTimer{Interval=TimeSpan.FromSeconds(5)};readonly Queue<UtilityNotice> noticeQueue=new Queue<UtilityNotice>();UtilityNotice currentNotice;FrameworkElement compactNotice;TextBlock noticeTitle,noticeDetail;ProgressBar noticeProgress;Action shelfChanged;long lastUtilitySecond=-1;
  void InitUtilities(UtilitySession shared){
   tools=shared??new UtilitySession(testing);PreviewMouseWheel+=OnPageWheel;
   shelfChanged=delegate{OnUI(delegate{if(route=="shelf")RenderRoute(false);});};tools.ShelfChanged+=shelfChanged;
   AllowDrop=true;DragOver+=delegate(object sender,DragEventArgs e){if(!e.Data.GetDataPresent(DataFormats.FileDrop))return;e.Effects=tools.Copying?DragDropEffects.None:DragDropEffects.Copy;e.Handled=true;if(route!="shelf")Navigate("shelf");};Drop+=delegate(object sender,DragEventArgs e){if(!e.Data.GetDataPresent(DataFormats.FileDrop))return;e.Handled=true;ImportShelf((string[])e.Data.GetData(DataFormats.FileDrop));};
   utilityTick.Tick+=delegate{long second=tools.Now.UtcTicks/TimeSpan.TicksPerSecond;if(lastUtilitySecond!=second){lastUtilitySecond=second;foreach(var notice in tools.Tick())PublishNotice(notice);if(!testing)PollSystemIndicators();}UpdateToolDisplays();string caption=tools.Activity;if(caption!=activityCaption){activityCaption=caption;UpdateCompact();}utilityTick.Interval=TimeSpan.FromMilliseconds(route=="time"&&timePage=="Kronometre"&&tools.Data.StopwatchRunning?100:1000);};
   noticeExpiry.Tick+=delegate{noticeExpiry.Stop();currentNotice=null;UpdateCompact();if(noticeQueue.Count>0){ShowNotice(noticeQueue.Dequeue());return;}if(noticeBadge!=null)noticeBadge.Opacity=.35;if(route==""&&!edgeHidden)RenderRoute(true);};
   Loaded+=delegate{if(testing)return;tools.Save();utilityTick.Start();StartSystemIndicators();};
   Closed+=delegate{utilityTick.Stop();noticeExpiry.Stop();tools.ShelfChanged-=shelfChanged;StopSystemIndicators();if(!replacingWindow)try{tools.Save();}catch{}};
   PreviewKeyDown+=delegate(object sender,KeyEventArgs e){if((Keyboard.Modifiers&ModifierKeys.Alt)!=0&&(e.Key==Key.Left||e.Key==Key.Right)){MovePage(e.Key==Key.Left?-1:1);e.Handled=true;}};
  }
  void PublishNotice(string kind,string title,string detail,string icon,string destination,double value=-1){PublishNotice(new UtilityNotice{Kind=kind,Title=title,Detail=detail,Icon=icon,Route=destination,Value=value});}
  void PublishNotice(UtilityNotice notice){if(closing)return;if(currentNotice!=null&&currentNotice.Kind==notice.Kind){currentNotice=notice;UpdateNoticeText();noticeExpiry.Stop();noticeExpiry.Start();return;}if(currentNotice!=null){if(notice.Kind.StartsWith("system-"))return;if(noticeQueue.Count<20)noticeQueue.Enqueue(notice);return;}ShowNotice(notice);}
  void ShowNotice(UtilityNotice notice){currentNotice=notice;noticeExpiry.Interval=TimeSpan.FromSeconds(notice.Kind=="water"?12:5);UpdateCompact();noticeExpiry.Stop();noticeExpiry.Start();if(notice.Sound&&!testing)System.Media.SystemSounds.Asterisk.Play();if(noticeBadge!=null){noticeBadge.Opacity=1;noticeBadge.ToolTip=notice.Title+" · "+notice.Detail;}if(edgeHidden){if(!notice.Kind.StartsWith("system-")&&tray!=null)tray.ShowBalloonTip(5000,notice.Title,notice.Detail,System.Windows.Forms.ToolTipIcon.Info);return;}if(route=="")RenderRoute(true);}
  void UpdateNoticeText(){if(currentNotice==null)return;if(noticeTitle!=null)noticeTitle.Text=currentNotice.Title;if(noticeDetail!=null)noticeDetail.Text=currentNotice.Detail;if(noticeProgress!=null){noticeProgress.Visibility=currentNotice.Value<0?Visibility.Collapsed:Visibility.Visible;noticeProgress.Value=Math.Max(0,Math.Min(100,currentNotice.Value));}}
  FrameworkElement BuildCompactNotice(){var outer=new StackPanel{Margin=new Thickness(22,8,22,18)};var row=new Grid();row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(44)});row.ColumnDefinitions.Add(new ColumnDefinition());row.Children.Add(Glyph(currentNotice.Icon??"bell",28,"Accent"));var info=new StackPanel{VerticalAlignment=VerticalAlignment.Center};noticeTitle=Label(currentNotice.Title,16,true);noticeDetail=Label(currentNotice.Detail,11,false,"Muted");noticeDetail.Margin=new Thickness(0,5,0,0);noticeDetail.TextWrapping=TextWrapping.Wrap;info.Children.Add(noticeTitle);info.Children.Add(noticeDetail);Grid.SetColumn(info,1);row.Children.Add(info);outer.Children.Add(row);noticeProgress=new ProgressBar{Minimum=0,Maximum=100,Height=4,Margin=new Thickness(44,12,0,0)};noticeProgress.SetResourceReference(Control.ForegroundProperty,"Accent");noticeProgress.SetResourceReference(Control.BackgroundProperty,"Track");outer.Children.Add(noticeProgress);
   if(currentNotice.Route=="water"){outer.Children.Add(ActionRow(ActionButton("İçtim","drop",delegate{tools.Drink(tools.Data.CupMl);DismissNotice();}),ActionButton("10 dk ertele","clock",delegate{tools.SnoozeWater();DismissNotice();})));}else if(!String.IsNullOrEmpty(currentNotice.Route)){var open=MakeButton("Bildirimi aç","Aç",delegate{string target=currentNotice.Route;DismissNotice();Navigate(target);});open.HorizontalAlignment=HorizontalAlignment.Right;open.MinHeight=28;open.Padding=new Thickness(12,4,12,4);open.Margin=new Thickness(0,8,0,0);open.Background=Brushes.Transparent;outer.Children.Add(open);}UpdateNoticeText();return outer;}
  void DismissNotice(){noticeExpiry.Stop();currentNotice=null;UpdateCompact();if(route=="")RenderRoute(true);if(noticeQueue.Count>0)ShowNotice(noticeQueue.Dequeue());}
  void RenderCompactNotice(bool animate){compactNotice=BuildCompactNotice();Grid.SetRow(compactNotice,1);main.Children.Add(compactNotice);compactNotice.Measure(new Size(IslandWidth,Double.PositiveInfinity));ResizeIsland(IslandWidth,HeaderHeight+compactNotice.DesiredSize.Height,animate);UpdateHeaderLayout(false);}
 }
}
