using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Xml.Linq;
using Microsoft.Win32;
namespace Veilix;
internal static class StartupRegistration {
 internal const string TaskName="Veilix Desktop Widget";
 const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
 static readonly XNamespace Ns="http://schemas.microsoft.com/windows/2004/02/mit/task";
 internal static string BuildXml(string executable,string userSid)=>new XDocument(new XElement(Ns+"Task",new XAttribute("version","1.2"),
  new XElement(Ns+"RegistrationInfo",new XElement(Ns+"Description","Veilix masaüstü widget — kullanıcı oturumu açıldığında.")),
  new XElement(Ns+"Triggers",new XElement(Ns+"LogonTrigger",new XElement(Ns+"Enabled","true"),new XElement(Ns+"UserId",userSid),new XElement(Ns+"Delay","PT12S"))),
  new XElement(Ns+"Principals",new XElement(Ns+"Principal",new XAttribute("id","Author"),new XElement(Ns+"UserId",userSid),new XElement(Ns+"LogonType","InteractiveToken"),new XElement(Ns+"RunLevel","LeastPrivilege"))),
  new XElement(Ns+"Settings",new XElement(Ns+"MultipleInstancesPolicy","IgnoreNew"),new XElement(Ns+"DisallowStartIfOnBatteries","false"),new XElement(Ns+"StopIfGoingOnBatteries","false"),new XElement(Ns+"StartWhenAvailable","true"),new XElement(Ns+"Enabled","true"),new XElement(Ns+"ExecutionTimeLimit","PT0S"),new XElement(Ns+"RestartOnFailure",new XElement(Ns+"Interval","PT1M"),new XElement(Ns+"Count","3"))),
  new XElement(Ns+"Actions",new XAttribute("Context","Author"),new XElement(Ns+"Exec",new XElement(Ns+"Command",executable),new XElement(Ns+"Arguments","--startup"),new XElement(Ns+"WorkingDirectory",Path.GetDirectoryName(executable)))))).ToString();
 static (int Code,string Text) Run(params string[] args){var info=new ProcessStartInfo(Path.Combine(Environment.SystemDirectory,"schtasks.exe")){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};foreach(var arg in args)info.ArgumentList.Add(arg);using var process=Process.Start(info);var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();if(!process.WaitForExit(8000)){try{process.Kill();}catch{}throw new IOException("Windows başlangıç ayarı yanıt vermedi.");}return (process.ExitCode,output.GetAwaiter().GetResult()+error.GetAwaiter().GetResult());}
 static XDocument Query(){var result=Run("/Query","/TN",TaskName,"/XML");if(result.Code!=0)return null;return XDocument.Parse(result.Text.TrimStart('\uFEFF'));}
 static bool OwnTask(XDocument task,string executable)=>String.Equals(task?.Descendants(Ns+"Command").FirstOrDefault()?.Value,executable,StringComparison.OrdinalIgnoreCase);
 internal static bool Enabled {get{try{var xml=Query();return OwnTask(xml,Environment.ProcessPath)&&xml.Descendants(Ns+"Settings").Elements(Ns+"Enabled").FirstOrDefault()?.Value!="false";}catch{return false;}}}
 internal static void SetEnabled(bool enabled){
  string executable=Environment.ProcessPath;var existing=Query();if(existing!=null&&!OwnTask(existing,executable))throw new IOException("Aynı adlı farklı bir başlangıç görevi var.");
  if(enabled){string file=Path.Combine(Path.GetTempPath(),"veilix-startup-"+Guid.NewGuid().ToString("N")+".xml");try{File.WriteAllText(file,BuildXml(executable,WindowsIdentity.GetCurrent().User.Value),System.Text.Encoding.Unicode);var result=Run("/Create","/TN",TaskName,"/XML",file,"/F");if(result.Code!=0)throw new IOException("Windows başlangıç kaydı oluşturulamadı: "+result.Text.Trim());}finally{File.Delete(file);}if(!Enabled)throw new IOException("Başlangıç kaydı doğrulanamadı.");}
  else if(existing!=null){var result=Run("/Delete","/TN",TaskName,"/F");if(result.Code!=0)throw new IOException(result.Text.Trim());}
  // Remove only this checkout's old registrations, never another application's.
  using var key=Registry.CurrentUser.OpenSubKey(RunKey,true);if(key!=null){string workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../.."));var own=new[]{executable,Path.Combine(workspace,"app/bin/Veilix.exe"),Path.Combine(workspace,"app/bin/Halo.exe"),Path.Combine(workspace,"app/bin/Ada.exe")};foreach(string name in new[]{"Veilix","AdaDesktop","Halo"})if(key.GetValue(name) is string value&&own.Any(path=>String.Equals(value.Trim().Trim('"'),path,StringComparison.OrdinalIgnoreCase)))key.DeleteValue(name,false);}
 }
}
