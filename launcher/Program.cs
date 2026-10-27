using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace AzurikLauncher {
internal static class Program {
    [STAThread] static void Main() {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Launcher());
    }
}
internal sealed class Launcher : Form {
    readonly string root = AppDomain.CurrentDomain.BaseDirectory;
    string SettingsPath { get { return Path.Combine(root, "out", "build", "release", "settings.toml"); } }
    string ControlsPath { get { return Path.Combine(root, "controles.ini"); } }
    string GamePath { get { return Path.Combine(root, "out", "build", "release", "Azurik.exe"); } }
    readonly ComboBox mode = new ComboBox(), aniso = new ComboBox(), language = new ComboBox(), upscale = new ComboBox();
    readonly NumericUpDown width = new NumericUpDown(), height = new NumericUpDown(), scale = new NumericUpDown(), fps = new NumericUpDown(), sharpen = new NumericUpDown(), brightness = new NumericUpDown(), contrast = new NumericUpDown(), saturation = new NumericUpDown(), deadzone = new NumericUpDown(), sensitivityX = new NumericUpDown(), sensitivityY = new NumericUpDown(), vibration = new NumericUpDown();
    readonly CheckBox diagnostic = new CheckBox(), largeText = new CheckBox();
    readonly CheckBox vsync = new CheckBox(), aa = new CheckBox(), nearest = new CheckBox(), invert = new CheckBox(), invertY = new CheckBox();
    readonly string[] padIds={"dpad_up","dpad_down","dpad_left","dpad_right","start","back","left_click","right_click","a","b","x","y","black","white","left_trigger","right_trigger"};
    readonly string[] padNames={"Cruceta arriba","Cruceta abajo","Cruceta izquierda","Cruceta derecha","Start","Back","Clic stick izquierdo","Clic stick derecho","A","B","X","Y","Negro / RB","Blanco / LB","Gatillo izquierdo","Gatillo derecho"};
    readonly ComboBox[] padButtons=new ComboBox[16];
    readonly Dictionary<string, TextBox> keys = new Dictionary<string, TextBox>();
    readonly string[] ids = {"move_forward","move_back","move_left","move_right","camera_up","camera_down","camera_left","camera_right","a","b","x","y","black","white","left_trigger","right_trigger","start","back","left_click","right_click","dpad_up","dpad_down","dpad_left","dpad_right"};
    readonly string[] names = {"Avanzar","Retroceder","Mover a la izquierda","Mover a la derecha","Cámara arriba","Cámara abajo","Cámara izquierda","Cámara derecha","Botón A","Botón B","Botón X","Botón Y","Botón negro","Botón blanco","Gatillo izquierdo","Gatillo derecho","Inicio","Atrás","Clic izquierdo","Clic derecho","Cruceta arriba","Cruceta abajo","Cruceta izquierda","Cruceta derecha"};
    readonly string[] defaults = {"W","S","A","D","I","K","J","L","SPACE","E","Q","R","X","Z","SHIFT","CTRL","ENTER","BACKSPACE","C","V","UP","DOWN","LEFT","RIGHT"};
    readonly TextBox gameSource = new TextBox {ReadOnly=true};
    readonly Label status = new Label();
    public Launcher() {
        Text = "Azurik — Lanzador"; StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(730, 690); MinimumSize = new Size(600, 580);
        Font = new Font("Segoe UI", 9.5f); BackColor = Color.FromArgb(25, 29, 39); ForeColor = Color.White;
        var outer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(18) };
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 73)); outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        var title = new Label { Text = "AZURIK  ·  RISE OF PERATHIA\nConfiguración y acceso al juego", Font = new Font("Segoe UI", 17, FontStyle.Bold), ForeColor = Color.FromArgb(134, 207, 232), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        outer.Controls.Add(title, 0, 0);
        var tabs = new TabControl { Dock = DockStyle.Fill }; tabs.TabPages.Add(GameTab()); tabs.TabPages.Add(LanguageTab()); tabs.TabPages.Add(GraphicsTab()); tabs.TabPages.Add(ControlsTab()); tabs.TabPages.Add(GamepadTab()); outer.Controls.Add(tabs, 0, 1);
        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105)); bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
        status.Text = "Los cambios se aplican al iniciar el juego."; status.Dock = DockStyle.Fill; status.TextAlign = ContentAlignment.MiddleLeft; status.AutoEllipsis = true;
        bottom.Controls.Add(status, 0, 0);
        var save = Button("Guardar"); save.Click += (s,e) => Save(false); bottom.Controls.Add(save, 1, 0);
        var play = Button("Guardar y jugar"); play.BackColor = Color.FromArgb(41, 110, 133); play.Click += async (s,e) => {if(await PrepareGame())Save(true);}; bottom.Controls.Add(play, 2, 0);
        outer.Controls.Add(bottom, 0, 2); Controls.Add(outer);
        try { LoadValues(); } catch(Exception ex) { MessageBox.Show(this, "No se pudieron leer los ajustes: " + ex.Message, "Azurik", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    Button Button(string text) { return new Button { Text = text, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(52, 61, 76), ForeColor = Color.White, Margin = new Padding(4, 12, 4, 12) }; }
    TabPage GameTab() {
        var tab=new TabPage("Juego") {BackColor=Color.FromArgb(32,38,50),ForeColor=Color.White};
        var grid=Grid(7);tab.Controls.Add(grid);Add(grid,0,"Tu copia del juego",gameSource);
        var iso=new Button {Text="Elegir ISO / XISO"};iso.Click+=async (s,e)=>{using(var d=new OpenFileDialog {Filter="Imagen Xbox|*.iso;*.xiso|Todos los archivos|*.*"})if(d.ShowDialog(this)==DialogResult.OK){gameSource.Text=d.FileName;await PrepareGame();}};Add(grid,1,"Imagen de disco",iso);
        var folder=new Button {Text="Elegir carpeta extraída"};folder.Click+=async (s,e)=>{using(var d=new FolderBrowserDialog {Description="Carpeta que contiene default.xbe",ShowNewFolderButton=false})if(d.ShowDialog(this)==DialogResult.OK){gameSource.Text=d.SelectedPath;await PrepareGame();}};Add(grid,2,"XISO extraído",folder);
        var prepare=new Button {Text="Comprobar instalación"};prepare.Click+=async (s,e)=>{await PrepareGame();};Add(grid,3,"Preparación automática",prepare);
        AddText(grid,4,"Aporta tu propia copia. El proyecto no incluye archivos del juego.");
        AddText(grid,5,"Al elegir tu copia se preparan automáticamente ambos idiomas y todas las mejoras.");
        AddText(grid,6,"No necesitas instalar herramientas ni compilar. El proceso funciona sin Internet.");return tab;
    }
    bool SaveSource() {
        if(!File.Exists(gameSource.Text) && !Directory.Exists(gameSource.Text)) {MessageBox.Show(this,"Selecciona tu ISO/XISO o carpeta extraída en Juego.");return false;}
        WriteFile(Path.Combine(root,"game-source.txt"),gameSource.Text);return true;
    }
    async Task<bool> PrepareGame() {
        if(!File.Exists(gameSource.Text) && !Directory.Exists(gameSource.Text)) {MessageBox.Show(this,"Selecciona tu ISO/XISO o carpeta extraída en Juego.");return false;}
        string helper=Path.Combine(root,"installer","PrepararJuego.exe");
        if(!File.Exists(helper)||!File.Exists(GamePath)){MessageBox.Show(this,"Faltan componentes. Descarga y extrae el paquete completo de la versión publicada.");return false;}
        Enabled=false;status.Text="Comprobando tu copia…";
        try {
            WriteFile(Path.Combine(root,"prepare-source.txt"),gameSource.Text);
            string last="";
            int result=await Task.Run(()=>{
                var start=new ProcessStartInfo {FileName=helper,WorkingDirectory=root,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};
                using(var log=new StreamWriter(Path.Combine(root,"setup.log"),false,new UTF8Encoding(false)))
                using(var p=new Process {StartInfo=start}) {
                    object gate=new object();
                    DataReceivedEventHandler receive=(s,e)=>{if(e.Data!=null){lock(gate){last=e.Data;log.WriteLine(e.Data);log.Flush();}if(IsHandleCreated&&!IsDisposed)BeginInvoke((Action)(()=>status.Text=e.Data));}};
                    p.OutputDataReceived+=receive;p.ErrorDataReceived+=receive;
                    p.Start();p.BeginOutputReadLine();p.BeginErrorReadLine();p.WaitForExit();return p.ExitCode;
                }
            });
            if(result!=0){status.Text="No se pudo preparar la copia.";MessageBox.Show(this,last,"Preparación",MessageBoxButtons.OK,MessageBoxIcon.Error);return false;}
            status.Text="Listo. Ambos idiomas y todas las mejoras están disponibles.";return true;
        }catch(Exception ex){MessageBox.Show(this,ex.Message,"Preparación",MessageBoxButtons.OK,MessageBoxIcon.Error);return false;}finally{Enabled=true;}
    }
    TabPage LanguageTab() {
        var tab = new TabPage("Idioma") { BackColor = Color.FromArgb(32, 38, 50), ForeColor = Color.White };
        var grid = Grid(7); tab.Controls.Add(grid);
        language.DropDownStyle = ComboBoxStyle.DropDownList;
        language.Items.AddRange(new object[] { "English", "Castellano" });
        Add(grid, 0, "Idioma del juego", language);
        AddText(grid, 1, "Castellano: menús, avisos y tutoriales traducidos.");
        AddText(grid, 2, "Las voces y las cinemáticas mantienen el audio original en inglés.");
        AddText(grid, 3, "Guarda la elección y abre el juego para aplicar el idioma.");
        AddText(grid, 4, "Puedes volver a English en cualquier momento.");
        largeText.Text="Ampliar avisos y tutoriales";Add(grid,5,"Texto en castellano",largeText);
        AddText(grid,6,"La letra crece donde hay espacio; los mensajes largos se ajustan al panel.");
        return tab;
    }
    TabPage GraphicsTab() {
        var tab = new TabPage("Imagen y rendimiento") { BackColor = Color.FromArgb(32, 38, 50), ForeColor = Color.White, AutoScroll = true };
        var grid = Grid(19); tab.Controls.Add(grid);
        mode.DropDownStyle = aniso.DropDownStyle = ComboBoxStyle.DropDownList;
        mode.Items.AddRange(new object[] { "Ventana sin bordes", "Pantalla completa", "Ventana" });
        aniso.Items.AddRange(new object[] { "Del juego", "2×", "4×", "8×", "16×" });
        InitNumber(width, 0, 7680, 0); InitNumber(height, 0, 4320, 0); InitNumber(scale, 50, 200, 100); InitNumber(fps, 0, 240, 60); InitNumber(sharpen, 0, 100, 20);
        InitNumber(brightness, 50, 150, 100); InitNumber(contrast, 50, 150, 100); InitNumber(saturation, 0, 150, 100);
        Add(grid, 0, "Modo de pantalla", mode);
        Add(grid, 1, "Anchura (0 = escritorio)", width); Add(grid, 2, "Altura (0 = escritorio)", height);
        Add(grid, 3, "Resolución interna (%)", scale); Add(grid, 4, "Límite de FPS (0 = sin límite)", fps);
        Add(grid, 5, "Filtro anisotrópico", aniso);
        vsync.Text = "Activada"; aa.Text = "Activado"; nearest.Text = "Píxel nítido (sin filtro bilineal)";
        Add(grid, 6, "Sincronización vertical", vsync); Add(grid, 7, "Suavizado de bordes", aa);
        Add(grid, 8, "Nitidez (0–100)", sharpen); Add(grid, 9, "Reescalado", upscale);
        upscale.DropDownStyle=ComboBoxStyle.DropDownList; upscale.Items.AddRange(new object[]{"Bilineal (rápido)","Píxel nítido","Bicúbico (suave)"});
        Add(grid, 10, "Brillo (100 = original)", brightness); Add(grid, 11, "Contraste (100 = original)", contrast); Add(grid, 12, "Saturación (100 = original)", saturation);
        AddText(grid, 13, "Resolución interna: 100% es el valor normal; 75% puede mejorar el rendimiento.");
        AddText(grid, 14, "Los filtros se aplican a la imagen final. Se leen al arrancar el juego.");
        AddText(grid, 15, "Para un tamaño personalizado, indica anchura y altura; 0 en ambos usa el escritorio.");
        var profiles = new FlowLayoutPanel { Dock=DockStyle.Fill, WrapContents=false };
        foreach(string name in new[]{"Original","Equilibrado","Calidad"}) {
            string selected=name; var b=new Button {Text=name,AutoSize=true};
            b.Click+=(s,e)=>ApplyProfile(selected); profiles.Controls.Add(b);
        }
        grid.Controls.Add(profiles,0,16);grid.SetColumnSpan(profiles,2);
        var reset=new Button {Text="Restaurar gráficos",Dock=DockStyle.Fill};reset.Click+=(s,e)=>ApplyProfile("Restaurar");
        Add(grid,17,"Valores recomendados",reset);
        diagnostic.Text="Registrar tiempos por fotograma";Add(grid,18,"Diagnóstico opcional",diagnostic);
        return tab;
    }
    void ApplyProfile(string name) {
        scale.Value=name=="Calidad"?150:100; aniso.SelectedIndex=name=="Original"?0:name=="Calidad"?4:3;
        aa.Checked=name!="Original";sharpen.Value=name=="Original"?0:20;upscale.SelectedIndex=0;
        brightness.Value=contrast.Value=saturation.Value=100;vsync.Checked=true;fps.Value=60;
        if(name=="Restaurar") {mode.SelectedIndex=0;width.Value=height.Value=0;}
        status.Text=name=="Original"?"Original: filtros del juego; conserva la resolución de salida.":name=="Calidad"?"Calidad: 150% de resolución interna; mayor coste de GPU.":"100% interno, filtro 8× y suavizado. Guarda para aplicar.";
    }
    TabPage ControlsTab() {
        var tab = new TabPage("Controles y accesibilidad") { BackColor = Color.FromArgb(32, 38, 50), ForeColor = Color.White, AutoScroll = true };
        var panel = Grid(8 + ids.Length); tab.Controls.Add(panel);
        InitNumber(deadzone, 0, 30000, 6000); deadzone.Increment = 500;
        Add(panel, 0, "Zona muerta del mando", deadzone);
        invert.Text = "Invertir giro lateral de cámara"; Add(panel, 1, "Cámara", invert);
        invertY.Text = "Invertir giro vertical"; Add(panel, 2, "Cámara vertical", invertY);
        InitNumber(sensitivityX,25,200,100); InitNumber(sensitivityY,25,200,100); InitNumber(vibration,0,100,100);
        Add(panel, 3, "Sensibilidad horizontal (%)", sensitivityX);
        Add(panel, 4, "Sensibilidad vertical (%)", sensitivityY);
        Add(panel, 5, "Intensidad de vibración (%)", vibration);
        AddText(panel, 6, "Haz clic en una casilla y pulsa la tecla que quieras asignar. Supr la desactiva.");
        AddText(panel, 7, "El mando y el teclado pueden usarse a la vez.");
        for(int i=0;i<ids.Length;i++) {
            var box = new TextBox { ReadOnly = true, Dock = DockStyle.Fill, TextAlign = HorizontalAlignment.Center, BackColor = Color.FromArgb(51, 60, 76), ForeColor = Color.White, Tag = ids[i], ShortcutsEnabled = false };
            box.KeyDown += KeyCaptured; box.KeyPress += (s,e) => e.Handled = true;
            keys[ids[i]] = box; Add(panel, i+8, names[i], box);
        }
        return tab;
    }
    TabPage GamepadTab() {
        var tab=new TabPage("Mando") {BackColor=Color.FromArgb(32,38,50),ForeColor=Color.White,AutoScroll=true};
        var grid=Grid(18);tab.Controls.Add(grid);
        AddText(grid,0,"Para cada acción del juego, elige el botón físico que la activa.");
        for(int i=0;i<16;++i){var c=new ComboBox {DropDownStyle=ComboBoxStyle.DropDownList};c.Items.Add("Desactivado");c.Items.AddRange(padNames);padButtons[i]=c;Add(grid,i+1,padNames[i],c);}
        var reset=new Button {Text="Restaurar mando",Dock=DockStyle.Fill};reset.Click+=(s,e)=>{for(int i=0;i<16;++i)padButtons[i].SelectedIndex=i+1;};
        grid.Controls.Add(reset,0,17);grid.SetColumnSpan(reset,2);return tab;
    }
    TableLayoutPanel Grid(int rows) {
        var g = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = rows, Padding = new Padding(15) };
        g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56)); g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        for(int i=0;i<rows;i++) g.RowStyles.Add(new RowStyle(SizeType.Absolute, 39)); return g;
    }
    void Add(TableLayoutPanel g,int row,string label,Control c) {
        g.Controls.Add(new Label { Text=label, Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleLeft, AutoEllipsis=true },0,row);
        c.Margin = new Padding(2,5,2,5); c.Dock = DockStyle.Fill; g.Controls.Add(c,1,row);
    }
    void AddText(TableLayoutPanel g,int row,string text) { var l=new Label { Text=text, Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleLeft, ForeColor=Color.FromArgb(185,197,212) }; g.Controls.Add(l,0,row);g.SetColumnSpan(l,2); }
    void InitNumber(NumericUpDown n,int min,int max,int val) { n.Minimum=min;n.Maximum=max;n.Value=val;n.ThousandsSeparator=true; }
    void KeyCaptured(object sender, KeyEventArgs e) { var box=(TextBox)sender; var k=e.KeyCode; string value;
        if(k==Keys.Delete) value="NONE";
        else if(k>=Keys.A && k<=Keys.Z || k>=Keys.D0 && k<=Keys.D9) value=k>=Keys.D0 && k<=Keys.D9 ? ((int)k-(int)Keys.D0).ToString() : k.ToString();
        else if(k>=Keys.NumPad0 && k<=Keys.NumPad9) value=((int)k-(int)Keys.NumPad0).ToString();
        else { var known=new Dictionary<Keys,string> {{Keys.Space,"SPACE"},{Keys.Enter,"ENTER"},{Keys.Back,"BACKSPACE"},{Keys.Tab,"TAB"},{Keys.ShiftKey,"SHIFT"},{Keys.ControlKey,"CTRL"},{Keys.Menu,"ALT"},{Keys.Up,"UP"},{Keys.Down,"DOWN"},{Keys.Left,"LEFT"},{Keys.Right,"RIGHT"}};
            if(!known.TryGetValue(k,out value)) { int v=(int)k; if(v>0 && v<256) value="0x"+v.ToString("X2"); else { e.SuppressKeyPress=true;return; } }
        }
        box.Text=value;e.SuppressKeyPress=true;e.Handled=true;
    }
    static string Toml(string text,string key,string fallback) { var m=Regex.Match(text,@"(?m)^\s*"+Regex.Escape(key)+@"\s*=\s*([^#\r\n]+)");return m.Success?m.Groups[1].Value.Trim().Trim('\'','"'):fallback; }
    static string Ini(string text,string section,string key,string fallback) { var m=Regex.Match(text,@"(?ms)^\["+Regex.Escape(section)+@"\]\s*\r?\n(?<body>.*?)(?=^\[|\z)"); if(!m.Success)return fallback;var x=Regex.Match(m.Groups["body"].Value,@"(?m)^\s*"+Regex.Escape(key)+@"\s*=\s*([^;\r\n]*)");return x.Success?x.Groups[1].Value.Trim():fallback; }
    static decimal Num(string s,decimal fallback,NumericUpDown box){ decimal n;return decimal.TryParse(s,NumberStyles.Integer,CultureInfo.InvariantCulture,out n)?Math.Max(box.Minimum,Math.Min(box.Maximum,n)):fallback; }
    void LoadValues() {
        var source=Path.Combine(root,"game-source.txt");gameSource.Text=File.Exists(source)?File.ReadAllText(source,Encoding.UTF8).Trim():"";
        var t=File.Exists(SettingsPath)?File.ReadAllText(SettingsPath):"";var i=File.Exists(ControlsPath)?File.ReadAllText(ControlsPath):"";
        largeText.Checked=Toml(t,"large_text","false")=="true";
        language.SelectedIndex=Toml(t,"game_language","en")=="es-ES"?1:0;
        string m=Toml(t,"display_mode","borderless");mode.SelectedIndex=m=="fullscreen"?1:m=="windowed"?2:0;
        width.Value=Num(Toml(t,"width","0"),0,width);height.Value=Num(Toml(t,"height","0"),0,height);
        scale.Value=Num(Toml(t,"render_percent","100"),100,scale);fps.Value=Num(Toml(t,"fps_cap","60"),60,fps);
        string a=Toml(t,"anisotropic_filtering","8");aniso.SelectedIndex=a=="2"?1:a=="4"?2:a=="8"?3:a=="16"?4:0;
        vsync.Checked=Toml(t,"vsync","true")=="true";aa.Checked=Toml(t,"post_antialiasing","true")=="true";
        brightness.Value=Num(Toml(t,"post_brightness","100"),100,brightness);
        contrast.Value=Num(Toml(t,"post_contrast","100"),100,contrast);
        saturation.Value=Num(Toml(t,"post_saturation","100"),100,saturation);
        sharpen.Value=Num(Toml(t,"post_sharpen","20"),20,sharpen);nearest.Checked=Toml(t,"nearest_upscale","false")=="true";
        upscale.SelectedIndex=(int)Num(Toml(t,"upscale_filter",nearest.Checked?"1":"0"),0,new NumericUpDown {Minimum=0,Maximum=2});
        diagnostic.Checked=Toml(t,"diagnostic_frames","false")=="true";
        deadzone.Value=Num(Ini(i,"controller","deadzone","6000"),6000,deadzone);invert.Checked=Ini(i,"camera","invert_horizontal","1")!="0";
        invertY.Checked=Ini(i,"camera","invert_vertical","0")!="0";
        sensitivityX.Value=Num(Ini(i,"camera","sensitivity_horizontal","100"),100,sensitivityX);
        sensitivityY.Value=Num(Ini(i,"camera","sensitivity_vertical","100"),100,sensitivityY);
        vibration.Value=Num(Ini(i,"controller","vibration","100"),100,vibration);
        for(int j=0;j<16;j++){int mapped; if(!Int32.TryParse(Ini(i,"gamepad",padIds[j],j.ToString()),out mapped))mapped=j;padButtons[j].SelectedIndex=Math.Max(-1,Math.Min(15,mapped))+1;}
        for(int j=0;j<ids.Length;j++)keys[ids[j]].Text=Ini(i,"keyboard",ids[j],defaults[j]);
    }
    static string SetToml(string text,string key,string value) { var re=new Regex(@"(?m)^(\s*"+Regex.Escape(key)+@"\s*=\s*)[^\r\n]*");return re.IsMatch(text)?re.Replace(text,m=>m.Groups[1].Value+value,1):text.TrimEnd()+Environment.NewLine+key+" = "+value+Environment.NewLine; }
    static string SetIni(string text,string section,string key,string value) {
        var sec=new Regex(@"(?ms)^(\["+Regex.Escape(section)+@"\]\s*\r?\n)(.*?)(?=^\[|\z)");var match=sec.Match(text);
        if(!match.Success)return text.TrimEnd()+Environment.NewLine+"["+section+"]"+Environment.NewLine+key+"="+value+Environment.NewLine;
        string body=match.Groups[2].Value;var prop=new Regex(@"(?m)^\s*"+Regex.Escape(key)+@"\s*=[^\r\n]*");
        body=prop.IsMatch(body)?prop.Replace(body,key+"="+value,1):body.TrimEnd()+Environment.NewLine+key+"="+value+Environment.NewLine;
        return text.Substring(0,match.Index)+match.Groups[1].Value+body+text.Substring(match.Index+match.Length);
    }
    static void WriteFile(string path,string text) { Directory.CreateDirectory(Path.GetDirectoryName(path));string tmp=path+".launcher.tmp";File.WriteAllText(tmp,text,new UTF8Encoding(false));if(File.Exists(path))File.Replace(tmp,path,null);else File.Move(tmp,path); }
    void Save(bool play) { try {
        if(!SaveSource())return;
        if((width.Value==0)!=(height.Value==0)) { MessageBox.Show(this,"Pon 0 en anchura y altura para usar el escritorio, o indica ambas dimensiones.","Resolución",MessageBoxButtons.OK,MessageBoxIcon.Warning);return; }
        if(language.SelectedIndex==1) {
            string pack=Path.Combine(Path.GetDirectoryName(GamePath),"localization","es-ES");
            if(!File.Exists(Path.Combine(pack,"gamedata","english.xbr")) || !File.Exists(Path.Combine(pack,"english.txt"))) {
                MessageBox.Show(this,"No se encuentra el paquete de castellano. Vuelve a generar o instalar la traducción.","Idioma",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;
            }
        }
        string t=File.Exists(SettingsPath)?File.ReadAllText(SettingsPath):"";
        t=SetToml(t,"large_text",largeText.Checked?"true":"false");
        t=SetToml(t,"game_language",language.SelectedIndex==1?"'es-ES'":"'en'");
        var values=new Dictionary<string,string> {{"display_mode","'"+(mode.SelectedIndex==1?"fullscreen":mode.SelectedIndex==2?"windowed":"borderless")+"'"},{"width",width.Value.ToString(CultureInfo.InvariantCulture)},{"height",height.Value.ToString(CultureInfo.InvariantCulture)},{"render_percent",scale.Value.ToString(CultureInfo.InvariantCulture)},{"fps_cap",fps.Value.ToString(CultureInfo.InvariantCulture)},{"anisotropic_filtering",new[]{"0","2","4","8","16"}[aniso.SelectedIndex<0?0:aniso.SelectedIndex]},{"vsync",vsync.Checked?"true":"false"},{"post_antialiasing",aa.Checked?"true":"false"},{"post_sharpen",sharpen.Value.ToString(CultureInfo.InvariantCulture)},{"post_brightness",brightness.Value.ToString(CultureInfo.InvariantCulture)},{"post_contrast",contrast.Value.ToString(CultureInfo.InvariantCulture)},{"post_saturation",saturation.Value.ToString(CultureInfo.InvariantCulture)},{"nearest_upscale",upscale.SelectedIndex==1?"true":"false"},{"upscale_filter",Math.Max(0,upscale.SelectedIndex).ToString(CultureInfo.InvariantCulture)},{"diagnostic_frames",diagnostic.Checked?"true":"false"}};
        foreach(var x in values)t=SetToml(t,x.Key,x.Value);
        string ini=File.Exists(ControlsPath)?File.ReadAllText(ControlsPath):"";
        foreach(var x in keys)ini=SetIni(ini,"keyboard",x.Key,x.Value.Text);
        ini=SetIni(ini,"controller","deadzone",deadzone.Value.ToString(CultureInfo.InvariantCulture));ini=SetIni(ini,"camera","invert_horizontal",invert.Checked?"1":"0");
        ini=SetIni(ini,"camera","invert_vertical",invertY.Checked?"1":"0");
        ini=SetIni(ini,"camera","sensitivity_horizontal",sensitivityX.Value.ToString(CultureInfo.InvariantCulture));
        ini=SetIni(ini,"camera","sensitivity_vertical",sensitivityY.Value.ToString(CultureInfo.InvariantCulture));
        ini=SetIni(ini,"controller","vibration",vibration.Value.ToString(CultureInfo.InvariantCulture));
        for(int j=0;j<16;j++)ini=SetIni(ini,"gamepad",padIds[j],(padButtons[j].SelectedIndex-1).ToString(CultureInfo.InvariantCulture));
        WriteFile(SettingsPath,t);WriteFile(ControlsPath,ini);status.Text="Opciones guardadas.";
        if(play){if(!File.Exists(GamePath)){MessageBox.Show(this,"Falta Azurik.exe. Vuelve a extraer el paquete completo de la versión publicada.","Azurik",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
            var start=new ProcessStartInfo { FileName=GamePath,WorkingDirectory=Path.GetDirectoryName(GamePath),UseShellExecute=false };
            // F3 captures one GPU frame for diagnosing camera dependent particles.
            // No continuous tracing is enabled, so normal play has no extra cost.
            start.EnvironmentVariables["XBE_CAPTURE_DIR"]="captures";
            if(diagnostic.Checked) start.EnvironmentVariables["XBE_PERF"]="1";
            else start.EnvironmentVariables.Remove("XBE_PERF");
            Process.Start(start); Close(); }
    } catch(Exception ex){MessageBox.Show(this,"No se pudieron guardar las opciones: "+ex.Message,"Azurik",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }
}
}

