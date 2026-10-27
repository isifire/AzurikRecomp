using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
class LauncherSourceTest {
 [STAThread] static int Main() {
  string temp=Path.Combine(Path.GetTempPath(),"Azurik-launcher-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
  var asm=Assembly.LoadFrom(Path.Combine(Directory.GetCurrentDirectory(),"Lanzador.exe"));var type=asm.GetType("AzurikLauncher.Launcher");var flags=BindingFlags.NonPublic|BindingFlags.Instance;
  using(var form=(Form)Activator.CreateInstance(type)) {
   type.GetField("root",flags).SetValue(form,temp);
   string source=Path.Combine(temp,"Copia con espacios y acento á");Directory.CreateDirectory(source);
   ((TextBox)type.GetField("gameSource",flags).GetValue(form)).Text=source;
   ((ComboBox)type.GetField("language",flags).GetValue(form)).SelectedIndex=0;
   type.GetMethod("Save",flags).Invoke(form,new object[]{false});
   if(File.ReadAllText(Path.Combine(temp,"game-source.txt"))!=source)throw new Exception("Source path not preserved");
   ((TextBox)type.GetField("gameSource",flags).GetValue(form)).Text="";
   type.GetMethod("LoadValues",flags).Invoke(form,null);
   if(((TextBox)type.GetField("gameSource",flags).GetValue(form)).Text!=source)throw new Exception("Source path not restored");
  }
  Console.WriteLine("PASS: launcher saves and reloads an external Unicode source path without opening the game");return 0;
 }
}
