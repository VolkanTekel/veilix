// GPL-3.0-or-later. Event-driven audio/network/power, no typed-key collection.
using System;
using System.Globalization;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Forms=System.Windows.Forms;
namespace Ada {
 public sealed partial class AdaWindow {
  [DllImport("user32.dll")]static extern short GetKeyState(int key);
  [DllImport("user32.dll")]static extern IntPtr GetKeyboardLayout(uint thread);
  [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
  Action<double,bool> audioNotice;Action deviceNotice;NetworkAvailabilityChangedEventHandler networkNotice;PowerModeChangedEventHandler powerNotice;ManagementEventWatcher brightnessNotice;readonly object brightnessGate=new object();bool indicatorsStopped;bool previousCaps;string previousLanguage;Forms.PowerLineStatus previousPower;double previousVolume;bool previousMute,updatingSystemLevel;System.Windows.Controls.Slider systemVolumeSlider;
  string CurrentLanguage(){try{uint pid;uint thread=GetWindowThreadProcessId(Native.GetForegroundWindow(),out pid);int id=(int)(GetKeyboardLayout(thread).ToInt64()&0xffff);return CultureInfo.GetCultureInfo(id).TwoLetterISOLanguageName.ToUpperInvariant();}catch{return "";}}
  void StartSystemIndicators(){
   lock(brightnessGate){indicatorsStopped=false;}
   previousCaps=(GetKeyState(0x14)&1)!=0;previousLanguage=CurrentLanguage();previousPower=Forms.SystemInformation.PowerStatus.PowerLineStatus;audio.Refresh();previousVolume=audio.Volume;previousMute=audio.Muted;
   audioNotice=delegate(double value,bool muted){OnUI(delegate{if(systemVolumeSlider!=null&&route=="controls"&&!systemVolumeSlider.IsMouseCaptureWithin){updatingSystemLevel=true;systemVolumeSlider.Value=value;updatingSystemLevel=false;}bool changed=Math.Abs(value-previousVolume)>=.5||muted!=previousMute;previousVolume=value;previousMute=muted;if(changed&&tools.Data.SystemNotices)PublishNotice("system-volume",muted?"Ses kapalı":"Ses düzeyi",muted?"Sessiz mod etkin":"%"+Math.Round(value),muted?"mute":"speaker","controls",muted?0:value);});};audio.Changed+=audioNotice;
   deviceNotice=delegate{OnUI(delegate{audio.Refresh();previousVolume=audio.Volume;previousMute=audio.Muted;if(tools.Data.SystemNotices)PublishNotice("system-device","Ses çıkışı değişti","Yeni ses aygıtı kullanıma hazır.","headphones","controls");});};audio.DeviceChanged+=deviceNotice;audio.StartNotifications();
   networkNotice=delegate(object sender,NetworkAvailabilityEventArgs e){OnUI(delegate{if(tools.Data.SystemNotices)PublishNotice("system-network",e.IsAvailable?"Bağlantı geri geldi":"Ağ bağlantısı kesildi",e.IsAvailable?"Yeniden çevrimiçisin.":"Bağlantı geldiğinde haber vereceğim.","cloud","");});};NetworkChange.NetworkAvailabilityChanged+=networkNotice;
   powerNotice=delegate(object sender,PowerModeChangedEventArgs e){OnUI(delegate{var power=Forms.SystemInformation.PowerStatus.PowerLineStatus;if(power!=previousPower&&tools.Data.SystemNotices)PublishNotice("system-power",power==Forms.PowerLineStatus.Online?"Şarja bağlandı":"Pil kullanımında","Güç kaynağı güncellendi.","battery","");previousPower=power;});};SystemEvents.PowerModeChanged+=powerNotice;
   Task.Run(delegate{ManagementEventWatcher watcher=null;try{watcher=new ManagementEventWatcher("root\\WMI","SELECT * FROM WmiMonitorBrightnessEvent");watcher.EventArrived+=delegate(object sender,EventArrivedEventArgs e){double value=Convert.ToDouble(e.NewEvent["Brightness"]);OnUI(delegate{if(tools.Data.SystemNotices)PublishNotice("system-brightness","Ekran parlaklığı","%"+Math.Round(value),"sun","controls",value);});};watcher.Start();bool rejected;lock(brightnessGate){rejected=indicatorsStopped||closing;if(!rejected)brightnessNotice=watcher;}if(rejected){try{watcher.Stop();}finally{watcher.Dispose();}}}catch{if(watcher!=null)watcher.Dispose();}});
  }
  void PollSystemIndicators(){bool caps=(GetKeyState(0x14)&1)!=0;string language=CurrentLanguage();if(tools.Data.SystemNotices){if(caps!=previousCaps)PublishNotice("system-caps",caps?"Caps Lock açık":"Caps Lock kapalı",caps?"Büyük harflerle yazıyorsun.":"Normal yazı düzenine döndün.","keyboard","");if(language!=previousLanguage&&!String.IsNullOrEmpty(language))PublishNotice("system-language","Klavye dili",language,"keyboard","");}previousCaps=caps;previousLanguage=language;}
  void StopSystemIndicators(){if(audioNotice!=null)audio.Changed-=audioNotice;if(deviceNotice!=null)audio.DeviceChanged-=deviceNotice;if(networkNotice!=null)NetworkChange.NetworkAvailabilityChanged-=networkNotice;if(powerNotice!=null)SystemEvents.PowerModeChanged-=powerNotice;ManagementEventWatcher watcher;lock(brightnessGate){indicatorsStopped=true;watcher=brightnessNotice;brightnessNotice=null;}if(watcher!=null){try{watcher.Stop();}catch{}watcher.Dispose();}}
 }
}
