// GPL-3.0-or-later. Arrow/wheel navigation without stealing control scrolling.
using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
namespace Ada {
 public sealed partial class AdaWindow {
  static readonly string[] ToolRoutes={"media","water","controls","time","tasks","shelf"};
  static readonly string[] ToolNames={"Medya","Su","Kontroller","Zaman","Görevler","Raf"};
  static readonly string[] ToolIcons={"music","drop","controls","timer","check","folder"};
  static readonly DependencyProperty WheelControlProperty=DependencyProperty.RegisterAttached("WheelControl",typeof(bool),typeof(AdaWindow),new FrameworkPropertyMetadata(false,FrameworkPropertyMetadataOptions.Inherits));
  FrameworkElement navigationArea,pageHost;Button noticeBadge;string lastToolRoute="media",taskDraftTitle="",taskDraftDue="";int pageMotionDirection,requestedDirection,wheelCarry;DateTime lastWheelPage=DateTime.MinValue;
  void PrepareNavigation(string destination){int from=Array.IndexOf(ToolRoutes,route),to=Array.IndexOf(ToolRoutes,destination);pageMotionDirection=requestedDirection!=0?requestedDirection:from>=0&&to>=0&&from!=to?Math.Sign(to-from):0;requestedDirection=0;if(route=="tasks"&&taskTitleInput!=null){taskDraftTitle=taskTitleInput.Text;taskDraftDue=taskDueInput.Text;}if(to>=0)lastToolRoute=destination;}
  void MovePage(int direction){int index=Array.IndexOf(ToolRoutes,route);if(index<0)return;requestedDirection=direction;string next=ToolRoutes[(index+direction+ToolRoutes.Length)%ToolRoutes.Length];Navigate(next);}
  FrameworkElement BuildPageNavigation(){
   bool mainPage=Array.IndexOf(ToolRoutes,route)>=0;int index=Array.IndexOf(ToolRoutes,route);var grid=new Grid{Height=42};foreach(double width in new[]{36.0,40,1,40,36})grid.ColumnDefinitions.Add(new ColumnDefinition{Width=width==1?new GridLength(1,GridUnitType.Star):new GridLength(width)});
   if(mainPage){
    noticeBadge=IconButton("Son bildirim","bell",delegate{if(currentNotice!=null&&!String.IsNullOrEmpty(currentNotice.Route))Navigate(currentNotice.Route);});noticeBadge.Opacity=currentNotice==null?.35:1;noticeBadge.MinHeight=32;noticeBadge.Width=30;noticeBadge.Padding=new Thickness(4);noticeBadge.Background=Brushes.Transparent;grid.Children.Add(noticeBadge);
    var previous=IconButton("Önceki sayfa · Alt+Sol","arrow-left",delegate{MovePage(-1);});previous.MinHeight=34;previous.Width=34;previous.Padding=new Thickness(6);previous.Background=Brushes.Transparent;Grid.SetColumn(previous,1);grid.Children.Add(previous);
    var next=IconButton("Sonraki sayfa · Alt+Sağ","arrow-right",delegate{MovePage(1);});next.MinHeight=34;next.Width=34;next.Padding=new Thickness(6);next.Background=Brushes.Transparent;Grid.SetColumn(next,3);grid.Children.Add(next);
    var settings=IconButton("Ayarlar","settings",delegate{Navigate("settings");});settings.MinHeight=34;settings.Width=32;settings.Padding=new Thickness(6);settings.Background=Brushes.Transparent;Grid.SetColumn(settings,4);grid.Children.Add(settings);
   }else{var back=IconButton("Geri dön","arrow-left",delegate{Navigate(lastToolRoute);});back.MinHeight=34;back.Width=32;back.Padding=new Thickness(5);back.Background=Brushes.Transparent;grid.Children.Add(back);}
   var center=new StackPanel{VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center};var caption=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};caption.Children.Add(Glyph(mainPage?ToolIcons[index]:route=="settings"?"settings":"cloud",15,"Accent"));var name=Label(mainPage?ToolNames[index]:route=="settings"?"Ayarlar":"Günlük hava",13,true);name.Margin=new Thickness(7,0,0,0);caption.Children.Add(name);center.Children.Add(caption);
   if(mainPage){var dots=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,6,0,0)};for(int i=0;i<ToolRoutes.Length;i++){var dot=new Ellipse{Width=i==index?13:4,Height=4,Margin=new Thickness(3,0,3,0)};if(i==index){var active=new Border{Width=13,Height=4,CornerRadius=new CornerRadius(2),Margin=new Thickness(3,0,3,0)};active.SetResourceReference(Border.BackgroundProperty,"Accent");dots.Children.Add(active);}else{dot.SetResourceReference(Shape.FillProperty,"Track");dots.Children.Add(dot);}}center.Children.Add(dots);}Grid.SetColumn(center,2);grid.Children.Add(center);
   var border=new Border{CornerRadius=new CornerRadius(16),Child=grid,Margin=new Thickness(0,0,0,18)};border.SetResourceReference(Border.BackgroundProperty,"Nav");navigationArea=border;return border;
  }
  static DependencyObject ParentOf(DependencyObject item){if(item is Visual||item is System.Windows.Media.Media3D.Visual3D)return VisualTreeHelper.GetParent(item);return LogicalTreeHelper.GetParent(item);}
  static bool Under(DependencyObject item,DependencyObject parent){for(var node=item;node!=null;node=ParentOf(node))if(node==parent)return true;return false;}
  bool CanWheelNavigate(DependencyObject target){if(Array.IndexOf(ToolRoutes,route)<0||target==null||themeEditorOpen)return false;if(navigationArea!=null&&Under(target,navigationArea))return true;if(scroll==null||!Under(target,scroll))return false;for(var node=target;node!=null;node=ParentOf(node)){if((bool)node.GetValue(WheelControlProperty)||node is RangeBase||node is TextBoxBase||node is ComboBox||node is ButtonBase||node is Selector)return false;var viewer=node as ScrollViewer;if(viewer!=null&&viewer.ScrollableHeight>1)return false;}return true;}
  void OnPageWheel(object sender,MouseWheelEventArgs e){if(e.Handled||!CanWheelNavigate(e.OriginalSource as DependencyObject))return;e.Handled=true;if((DateTime.UtcNow-lastWheelPage).TotalMilliseconds<280){wheelCarry=0;return;}wheelCarry+=e.Delta;if(Math.Abs(wheelCarry)<120)return;int direction=wheelCarry<0?1:-1;wheelCarry=0;lastWheelPage=DateTime.UtcNow;MovePage(direction);}
 }
}
