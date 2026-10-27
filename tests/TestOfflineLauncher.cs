// Optional integration test against a prepared private fixture; never opens the game.
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
class OfflineLauncherTest {
 [STAThread] static int Main(string[] args) {
  var type=Assembly.LoadFrom(Path.GetFullPath("Lanzador.exe")).GetType("AzurikLauncher.Launcher");
  var flags=BindingFlags.NonPublic|BindingFlags.Instance;
  using(var form=(Form)Activator.CreateInstance(type)) {
   string root=Path.GetFullPath(args[0]);
   type.GetField("root",flags).SetValue(form,root);
   ((TextBox)type.GetField("gameSource",flags).GetValue(form)).Text=File.ReadAllText(Path.Combine(root,"prepare-source.txt"));
   var task=(Task<bool>)type.GetMethod("PrepareGame",flags).Invoke(form,null);
   while(!task.IsCompleted){Application.DoEvents();Thread.Sleep(10);}
   if(!task.GetAwaiter().GetResult())throw new Exception("Offline GUI preparation failed");
   ((ComboBox)type.GetField("language",flags).GetValue(form)).SelectedIndex=1;
   type.GetMethod("Save",flags).Invoke(form,new object[]{false});
   if(!File.Exists(Path.Combine(root,".prepared.json")))throw new Exception("Missing prepared marker");
  }
  Console.WriteLine("PASS: GUI prepares offline and saves Spanish without opening a window or game");return 0;
 }
}
