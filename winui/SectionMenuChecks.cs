using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Veilix;
public sealed partial class MainWindow {
 async Task CheckSectionMenu(List<string> report){
  bool reduced=prefs.ReducedMotion;
  prefs.ReducedMotion=true;
  Navigate("controls");
  root.UpdateLayout();
  var trigger=FindAncestor<Button>(pageTitle);
  Check(trigger?.Flyout is MenuFlyout,"Section title does not open a native menu");
  var menu=(MenuFlyout)trigger.Flyout;
  var entries=menu.Items.OfType<RadioMenuFlyoutItem>().ToArray();
  Check(entries.Select(i=>i.Text).SequenceEqual(Names),"Section menu order or labels differ from navigation");
  Check(entries.Single(i=>i.IsChecked).Text=="Hızlı ayarlar","Current section not selected in menu");
  var opened=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
  void Opened(object sender,object args)=>opened.TrySetResult();
  menu.Opened+=Opened;
  try{
   ((IInvokeProvider)new ButtonAutomationPeer(trigger).GetPattern(PatternInterface.Invoke)).Invoke();
   await opened.Task.WaitAsync(TimeSpan.FromSeconds(3));
   Check(sectionMenuOpen&&PopupActive,"Window did not suspend occlusion while the menu was open");
   MovePage(1);
   Check(route=="controls","Wheel navigation changed page while the section picker was open");
   var item=entries[Array.IndexOf(Routes,"water")];
   ((IInvokeProvider)new MenuFlyoutItemAutomationPeer(item)).Invoke();
   for(int i=0;i<60&&(route!="water"||sectionMenuOpen);i++)await Task.Delay(25);
   Check(route=="water"&&pageTitle.Text=="Su takibi"&&!PopupActive&&item.IsChecked,"Section selection did not navigate and dismiss the menu");
   lastNavigation=0;
   MovePage(1);
   Check(route=="controls","Wheel or arrow navigation did not resume after menu selection");
  }finally{menu.Opened-=Opened;menu.Hide();prefs.ReducedMotion=reduced;Navigate("");}
  report.Add("Section menu: native title invocation, all sections in wheel order, selection, navigation and dismissal verified.");
 }
}
