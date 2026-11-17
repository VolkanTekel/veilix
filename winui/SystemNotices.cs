using System;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Ada;
namespace Veilix;
public sealed partial class MainWindow {
 bool systemNoticesStarted;string previousLanguage;System.Net.NetworkInformation.NetworkAvailabilityChangedEventHandler networkChanged;PowerModeChangedEventHandler powerChanged;
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
 [DllImport("user32.dll")]static extern IntPtr GetKeyboardLayout(uint thread);
 string KeyboardLanguage(){try{uint pid;uint thread=GetWindowThreadProcessId(Native.GetForegroundWindow(),out pid);return CultureInfo.GetCultureInfo(GetKeyboardLayout(thread).ToInt32()&65535).DisplayName;}catch{return "";}}
 void StartSystemNotices(){previousLanguage=KeyboardLanguage();systemNoticesStarted=true;networkChanged=(_,e)=>EnqueueUi(()=>{if(!disposed&&tools.Data.SystemNotices)ShowNotice(new UtilityNotice{Kind="system-network",Title=e.IsAvailable?"Bağlantı hazır":"Bağlantı kesildi",Icon="network"});});powerChanged=(_,e)=>EnqueueUi(()=>{if(!disposed&&tools.Data.SystemNotices&&e.Mode==PowerModes.StatusChange)ShowNotice(new UtilityNotice{Kind="system-power",Title=System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus==System.Windows.Forms.PowerLineStatus.Online?"Prize bağlı":"Pille çalışıyor",Icon="battery"});});NetworkChange.NetworkAvailabilityChanged+=networkChanged;SystemEvents.PowerModeChanged+=powerChanged;}
 void PollSystemNotices(){if(!systemNoticesStarted)return;string language=KeyboardLanguage();if(tools.Data.SystemNotices){if(language!=previousLanguage&&language.Length>0)ShowNotice(new UtilityNotice{Kind="system-language",Title="Klavye dili",Detail=language,Icon="keyboard"});}previousLanguage=language;}
 void StopSystemNotices(){if(!systemNoticesStarted)return;systemNoticesStarted=false;NetworkChange.NetworkAvailabilityChanged-=networkChanged;SystemEvents.PowerModeChanged-=powerChanged;}
}
