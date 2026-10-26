using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
class LocalizeTextures {
 static bool Large;
 static string Output="translation/es/rendered";
 static Color Ink=Color.FromArgb(245,232,183), Gold=Color.FromArgb(232,190,75), Panel=Color.FromArgb(30,23,12);
 static void Clear(Graphics g,RectangleF r,Color color){g.CompositingMode=CompositingMode.SourceCopy;using(var b=new SolidBrush(color))g.FillRectangle(b,r);g.CompositingMode=CompositingMode.SourceOver;}
 static string Symbols(string text) {
  text=Regex.Replace(text,@"\[([ABXY])\]","$1");
  text=Regex.Replace(text,@"\b(A|B|X|Y|START|BACK)\b","{$1}");
  return text.Replace("gatillo izquierdo","gatillo {LT}").Replace("gatillo derecho","gatillo {RT}")
    .Replace("↑↓","{DPAD}").Replace("←→","{DPAD}");
 }
 static void Icon(Graphics g,string key,RectangleF r) {
  Color fill=key=="A"?Color.FromArgb(38,150,52):key=="B"?Color.FromArgb(205,35,35):key=="X"?Color.FromArgb(30,100,210):key=="Y"?Color.FromArgb(240,195,30):Color.FromArgb(75,80,87);
  using(var b=new SolidBrush(fill))using(var pen=new Pen(Color.FromArgb(245,235,210),1)) {
   if(key.Length==1){g.FillEllipse(b,r);g.DrawEllipse(pen,r);}
   else {g.FillRectangle(b,r);g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height);}
  }
  if(key=="DPAD") {
   using(var b=new SolidBrush(Color.WhiteSmoke)) {g.FillRectangle(b,r.X+r.Width*.4f,r.Y+r.Height*.16f,r.Width*.2f,r.Height*.68f);g.FillRectangle(b,r.X+r.Width*.16f,r.Y+r.Height*.4f,r.Width*.68f,r.Height*.2f);}return;
  }
  using(var font=new Font("Arial",r.Height*(key.Length>2?.42f:.66f),FontStyle.Bold,GraphicsUnit.Pixel))
  using(var brush=new SolidBrush(key=="Y"?Color.Black:Color.White))
  using(var sf=new StringFormat {Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})g.DrawString(key,font,brush,r,sf);
 }
 static float TokenWidth(Graphics g,string token,Font f,float size) {
  if(token.StartsWith("{"))return size*(token=="{START}"||token=="{BACK}"?2.6f:1.35f);
  return g.MeasureString(token,f,10000,StringFormat.GenericTypographic).Width;
 }
 static void Text(Graphics g,string text,RectangleF box,float max,Color color){
  if(String.IsNullOrWhiteSpace(text))return;
  var matches=Regex.Matches(Symbols(text),@"\{[^}]+\}|\n|[^\s{}]+");
  for(float size=max;size>=7;size-=.5f)using(var f=new Font("Georgia",size,FontStyle.Regular,GraphicsUnit.Pixel)) {
   float gap=size*.28f,lineHeight=Math.Max(f.GetHeight(g)*1.18f,size*1.5f),used=0;
   var lines=new List<List<string>>();var line=new List<string>();bool tooWide=false;
   foreach(Match m in matches){string token=m.Value;
    if(token=="\n"){lines.Add(line);line=new List<string>();used=0;continue;}
    float w=TokenWidth(g,token,f,size);if(w>box.Width)tooWide=true;
    if(line.Count>0 && used+gap+w>box.Width){lines.Add(line);line=new List<string>();used=0;}
    used+=(line.Count>0?gap:0)+w;line.Add(token);
   }if(line.Count>0)lines.Add(line);
   if(tooWide||lines.Count*lineHeight>box.Height){if(size<=7)throw new Exception("Text/icon overflow: "+text);continue;}
   float y=box.Y+(box.Height-lines.Count*lineHeight)/2;
   using(var brush=new SolidBrush(color))foreach(var row in lines){
    float w=0;foreach(string token in row)w+=TokenWidth(g,token,f,size)+gap;w=Math.Max(0,w-gap);
    float x=box.X+(box.Width-w)/2;
    foreach(string token in row){float tw=TokenWidth(g,token,f,size);
     if(token.StartsWith("{"))Icon(g,token.Substring(1,token.Length-2),new RectangleF(x,y+(lineHeight-size*1.25f)/2,tw,size*1.25f));
     else g.DrawString(token,f,brush,new PointF(x,y+(lineHeight-f.GetHeight(g))/2),StringFormat.GenericTypographic);
     x+=tw+gap;
    }y+=lineHeight;
   }return;
  }
 }
 static void FramedPanel(Graphics g,Rectangle r){Clear(g,r,Panel);using(var pen=new Pen(Color.FromArgb(120,95,40),1))g.DrawRectangle(pen,r);}
 static void Render(int id,Bitmap b,string text,string footer){using(var g=Graphics.FromImage(b)){
  g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;g.SmoothingMode=SmoothingMode.AntiAlias;
  int w=b.Width,h=b.Height;
  if(id>=43){FramedPanel(g,new Rectangle(25,32,w-50,h-52));Text(g,text,new RectangleF(29,40,w-58,h-87),Large?21:16,Ink);Text(g,footer,new RectangleF(30,h-40,w-60,20),11,Ink);}
  else if(id==0||id==2||id==8||id==9||id==10){FramedPanel(g,new Rectangle(40,32,w-80,h-46));Text(g,text,new RectangleF(53,50,w-106,h-86),22,Ink);}
  else if(id==1||id==39||id==40||id==41||id==42){Clear(g,new RectangleF(0,0,w,h),Color.Transparent);Text(g,text,new RectangleF(6,0,w-12,h),id==39?14:20,Ink);}
  else if(id==3||id==4||id==5||id==6||id==21||id==22){Clear(g,new RectangleF(12,10,w-24,h-20),Color.Black);var lines=text.Split('\n');Text(g,lines[0],new RectangleF(16,12,w-32,35),17,Gold);bool yes=id==4||id==6||id==22;Text(g,yes?"No":"› No ‹",new RectangleF(15,49,w-30,29),20,Gold);Text(g,yes?"› Sí ‹":"Sí",new RectangleF(15,82,w-30,29),20,Gold);}
  else if(id==7){Clear(g,new RectangleF(260,418,520,59),Color.FromArgb(95,66,26));Text(g,text,new RectangleF(268,434,494,28),18,Ink);}
  else if(id==11||id==12||id==13){FramedPanel(g,new Rectangle(112,321,294,155));string[] labels={"Continuar","Opciones","Salir"};int selected=id==13?0:id==11?1:2;for(int j=0;j<3;j++)Text(g,(selected==j?"› ":"")+labels[j]+(selected==j?" ‹":""),new RectangleF(120,325+j*40,278,37),26,Gold);Text(g,"↑↓ Elegir   [A] Aceptar   [B] Volver",new RectangleF(116,448,286,23),13,Ink);}
  else if(id==17||id==19||id==20){
   FramedPanel(g,new Rectangle(72,168,378,183));
   Text(g,"Sonido",new RectangleF(140,172,125,34),28,Gold);
   Text(g,"Efectos",new RectangleF(137,211,120,24),21,Gold);Text(g,"Música",new RectangleF(137,236,120,24),21,Gold);
   for(int j=0;j<10;j++)using(var brush=new SolidBrush(Color.Black)){g.FillEllipse(brush,267+j*12,219,10,16);g.FillEllipse(brush,267+j*12,244,10,16);}
   Text(g,"Vibración",new RectangleF(140,275,150,36),27,Gold);Text(g,"Sí",new RectangleF(156,312,40,28),22,Gold);Text(g,"No",new RectangleF(284,312,40,28),22,Gold);
   using(var pen=new Pen(Color.Gray,2)){foreach(int x in new[]{150,272})g.DrawPolygon(pen,new[]{new Point(x,315),new Point(x+6,326),new Point(x,337),new Point(x-6,326)});}
   int y=id==20?224:id==17?249:326;Text(g,"›",new RectangleF(83,y-16,45,32),30,Gold);Text(g,"‹",new RectangleF(398,y-16,45,32),30,Gold);
   FramedPanel(g,new Rectangle(125,431,270,31));Text(g,"↑↓ Elegir   ←→ Ajustar   [B] Volver",new RectangleF(128,433,264,27),13,Ink);
  } else throw new Exception("Unhandled texture "+id);
 }}
 static void Main(string[] args){
  Large=args.Length>0 && args[0]=="large";if(Large)Output="translation/es/rendered-large";
  var ser=new JavaScriptSerializer();
  var cat=ser.Deserialize<Dictionary<string,object>>(File.ReadAllText("translation/es/textos.es-ES.json"));int count=0;
  foreach(Dictionary<string,object> row in (System.Collections.IEnumerable)cat["entries"]){
   if((string)row["status"]!="translated")continue;int id=(int)row["id"];
   string source=Path.Combine("translation/extracted",Path.ChangeExtension((string)row["resource"],"png"));
   string dir=Path.Combine(Output,id.ToString("D4"));Directory.CreateDirectory(dir);
   using(var original=new Bitmap(source))using(var b=new Bitmap(original.Width,original.Height,PixelFormat.Format32bppArgb)){
    using(var g=Graphics.FromImage(b)){g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(original,0,0);}
    Render(id,b,(string)row["text"],(string)row["footer"]);b.Save(Path.Combine(dir,"preview.png"),ImageFormat.Png);
    int w=b.Width,h=b.Height,level=0;
    for(;;){using(var mip=new Bitmap(w,h,PixelFormat.Format32bppArgb)){
     using(var g=Graphics.FromImage(mip)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(b,new Rectangle(0,0,w,h));}
     mip.RotateFlip(RotateFlipType.RotateNoneFlipY);mip.Save(Path.Combine(dir,"mip"+level+".png"),ImageFormat.Png);
    }if(w==1&&h==1)break;w=Math.Max(1,w/2);h=Math.Max(1,h/2);level++;}
   }count++;
  }Console.WriteLine("Rendered "+count+" localized UI textures with mipmaps.");
 }

}
