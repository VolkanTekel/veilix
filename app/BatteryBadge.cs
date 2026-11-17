// GPL-3.0-or-later. Compact battery percentage with a vector progress ring.
using System;using System.Globalization;using System.Linq;using System.Windows;using System.Windows.Controls;using System.Windows.Media;
namespace Ada {
 public sealed class BatteryBadge:FrameworkElement {
  public int Percent;
  public static readonly DependencyProperty AccentProperty=DependencyProperty.Register("Accent",typeof(Brush),typeof(BatteryBadge),new FrameworkPropertyMetadata(Brushes.White,FrameworkPropertyMetadataOptions.AffectsRender));
  public static readonly DependencyProperty TrackProperty=DependencyProperty.Register("Track",typeof(Brush),typeof(BatteryBadge),new FrameworkPropertyMetadata(Brushes.Gray,FrameworkPropertyMetadataOptions.AffectsRender));
  protected override void OnRender(DrawingContext dc){double r=Math.Min(ActualWidth,ActualHeight)/2-2;var center=new Point(ActualWidth/2,ActualHeight/2);dc.DrawEllipse(null,new Pen((Brush)GetValue(TrackProperty),3.5),center,r,r);double amount=Math.Max(0,Math.Min(.9999,Percent/100.0));if(amount>0){var path=new StreamGeometry();using(var g=path.Open()){g.BeginFigure(new Point(center.X,center.Y-r),false,false);double angle=amount*2*Math.PI;g.ArcTo(new Point(center.X+Math.Sin(angle)*r,center.Y-Math.Cos(angle)*r),new Size(r,r),0,amount>.5,SweepDirection.Clockwise,true,false);}dc.DrawGeometry(null,new Pen((Brush)GetValue(AccentProperty),3.5){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},path);}}

 }
 public sealed partial class AdaWindow {
  Button bluetoothBadgeButton;BatteryBadge bluetoothBadge;
  void BuildBluetoothBadge(StackPanel line){bluetoothBadge=new BatteryBadge{Width=34,Height=34};bluetoothBadge.SetResourceReference(BatteryBadge.AccentProperty,"Accent");bluetoothBadge.SetResourceReference(BatteryBadge.TrackProperty,"Track");bluetoothBadgeButton=MakeButton("Bluetooth pil durumu",bluetoothBadge,delegate{Navigate("controls");});bluetoothBadgeButton.Width=34;bluetoothBadgeButton.Height=34;bluetoothBadgeButton.MinHeight=34;bluetoothBadgeButton.Padding=new Thickness(0);bluetoothBadgeButton.Margin=new Thickness(0,0,6,0);bluetoothBadgeButton.Background=Brushes.Transparent;line.Children.Add(bluetoothBadgeButton);UpdateBluetoothBadge();}
  void UpdateBluetoothBadge(){if(bluetoothBadge==null)return;var devices=bluetoothDevices.Where(d=>d.Connected&&d.Percent.HasValue).OrderBy(d=>d.Percent.Value).ThenBy(d=>d.Name).ToArray();bluetoothBadgeButton.Visibility=devices.Length==0?Visibility.Collapsed:Visibility.Visible;if(devices.Length==0)return;bluetoothBadge.Percent=devices[0].Percent.Value;bluetoothBadge.InvalidateVisual();bluetoothBadgeButton.ToolTip=String.Join("\n",devices.Select(d=>d.Name+" · %"+d.Percent));}
 }
}
