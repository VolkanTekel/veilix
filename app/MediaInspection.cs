// GPL-3.0-or-later. Explicit --inspect diagnostics, never song/artwork content.
using System;
using System.IO;
using System.Windows.Threading;
namespace Ada {
 public sealed partial class AdaWindow {
  void EnableMediaInspection(){
   var timer=new DispatcherTimer(DispatcherPriority.Background,Dispatcher){Interval=TimeSpan.FromSeconds(2)};
   timer.Tick+=delegate{try{Directory.CreateDirectory(Settings.DirectoryPath);File.WriteAllText(Path.Combine(Settings.DirectoryPath,"media-inspection.txt"),DateTimeOffset.Now.ToString("O")+" source="+Media.SourceAppId+" hasSong="+(!String.IsNullOrEmpty(Media.Title))+" playing="+Media.Playing+" cover="+(Media.Cover!=null)+" error="+Media.Error+Environment.NewLine+Media.Diagnostics);}catch(IOException){}catch(UnauthorizedAccessException){}};
   Loaded+=delegate{timer.Start();};Closed+=delegate{timer.Stop();};
  }
 }
}
