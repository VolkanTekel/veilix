using System;
using System.IO;
using Microsoft.UI.Xaml;
namespace Veilix;
internal static class Program {
 internal static void Trace(string text)=>File.AppendAllText(Path.Combine(AppContext.BaseDirectory,"startup.log"),DateTime.Now.ToString("O")+" "+text+Environment.NewLine);
 [STAThread] static void Main(string[] args) {
  if(Array.IndexOf(args,"--enable-startup")>=0){try{StartupRegistration.SetEnabled(true);Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory,"qa"));File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"qa","startup-registration.txt"),"Enabled="+StartupRegistration.Enabled+Environment.NewLine+"Task="+StartupRegistration.TaskName+Environment.NewLine+"Executable="+Environment.ProcessPath);}catch(Exception ex){Trace(ex.ToString());Environment.ExitCode=1;}return;}
  if(Array.IndexOf(args,"--api")>=0){File.WriteAllLines(Path.Combine(AppContext.BaseDirectory,"api.txt"),Array.ConvertAll(typeof(Microsoft.UI.Xaml.Controls.SystemBackdropElement).GetProperties(),p=>p.Name+" "+p.PropertyType.FullName));return;}
  using var singleton=new System.Threading.Mutex(false,@"Local\AdaDesktop-Window-v1");bool isolated=Array.IndexOf(args,"--self-test")>=0||Array.IndexOf(args,"--benchmark")>=0||Array.IndexOf(args,"--stability-test")>=0;bool acquired=false;if(!isolated){try{acquired=singleton.WaitOne(0);}catch(System.Threading.AbandonedMutexException){acquired=true;}if(!acquired)return;}
  Trace("Main");WinRT.ComWrappersSupport.InitializeComWrappers();
  try {
  Application.Start(initialization => {
   Trace("Application.Start");
   var queue=Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
   System.Threading.SynchronizationContext.SetSynchronizationContext(new GuardedUiContext(new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(queue)));
   _=new VeilixApp();
  });
  }catch(Exception ex){Trace(ex.ToString());Environment.ExitCode=1;}finally{if(acquired)singleton.ReleaseMutex();}
 }
}
public sealed partial class VeilixApp:Application {
 internal static MainWindow Main;
 public VeilixApp(){Program.Trace("App ctor");InitializeComponent();Program.Trace("Resources ready");UnhandledException+=(_,e)=>UiFaultBoundary.Report(e.Exception,"XAML unhandled",false);AppDomain.CurrentDomain.UnhandledException+=(_,e)=>{if(e.ExceptionObject is Exception error)UiFaultBoundary.Report(error,"AppDomain unhandled",false);};System.Threading.Tasks.TaskScheduler.UnobservedTaskException+=(_,e)=>UiFaultBoundary.Report(e.Exception,"unobserved task",false);}
 protected override async void OnLaunched(LaunchActivatedEventArgs args){try{Program.Trace("Launch");if(Array.IndexOf(Environment.GetCommandLineArgs(),"--startup")>=0){for(int attempt=0;attempt<5&&Ada.Native.Displays().Count==0;attempt++)await System.Threading.Tasks.Task.Delay(2000);}if(Ada.Native.Displays().Count==0)throw new InvalidOperationException("Windows henüz ekranları hazırlamadı.");Main=new MainWindow();Program.Trace("Window constructed");Main.Activate();Program.Trace("Window activated");}catch(Exception ex){Program.Trace(ex.ToString());Environment.ExitCode=1;Exit();}}
}


