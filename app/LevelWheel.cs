// GPL-3.0-or-later. Shared wheel behavior for audio and per-monitor brightness.
using System;
using System.Windows.Controls;
namespace Ada {
 public sealed partial class AdaWindow {
  static void AdjustLevelWheel(Slider slider,int delta,bool fine){if(!slider.IsEnabled)return;slider.Value=Math.Max(slider.Minimum,Math.Min(slider.Maximum,slider.Value+delta/120.0*(fine?1:2)));}
 }
}
