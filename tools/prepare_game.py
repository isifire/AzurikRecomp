"""Read only the user's game; prepare local ignored build inputs."""
import argparse,hashlib,os,struct
from pathlib import Path
EXPECTED='a989c8b0a67147856f77518ad3823bb6c31dff0c'
MAGIC=b'MICROSOFT*XBOX*MEDIA'
class Xiso:
 def __init__(self,path):
  self.f=path.open('rb');self.size=path.stat().st_size
  for base in (0,0x18300000):
   if base+65536+2048>self.size:continue
   v=self.read(base+65536,2048)
   if v[:20]==MAGIC and v[0x7ec:0x800]==MAGIC:
    self.base=base;self.root=struct.unpack_from('<II',v,20);return
  self.f.close();raise ValueError('No se reconoce una ISO/XISO de Xbox original (partición 0 o 0x18300000).')
 def close(self):self.f.close()
 def read(self,at,size):
  if at<0 or size<0 or at+size>self.size:raise ValueError('ISO truncada o rango inválido')
  self.f.seek(at);b=self.f.read(size)
  if len(b)!=size:raise ValueError('Lectura incompleta de ISO')
  return b
 def entries(self,sector,size):
  if size>16*1024*1024:raise ValueError('Tabla XISO demasiado grande')
  if not size:return []
  table=self.read(self.base+sector*2048,size);todo=[0];seen=set();out=[]
  while todo:
   at=todo.pop()
   if at in seen:continue
   seen.add(at)
   if at+14>size:raise ValueError('Entrada XISO fuera de tabla')
   left,right,sec,n,attr,length=struct.unpack_from('<HHIIBB',table,at)
   if left==65535:continue
   if at+14+length>size:raise ValueError('Nombre XISO truncado')
   name=table[at+14:at+14+length].decode('ascii',errors='strict')
   if left:todo.append(left*4)
   if right:todo.append(right*4)
   out.append((name,sec,n,bool(attr&16)))
  return out
 def load(self,name):
  sector,size=self.root;parts=name.split('/')
  for i,part in enumerate(parts):
   matches=[e for e in self.entries(sector,size) if e[0].lower()==part.lower()]
   if len(matches)!=1:raise ValueError('No se encuentra '+name)
   _,sector,size,isdir=matches[0]
   if i<len(parts)-1 and not isdir:raise ValueError('Ruta no es directorio')
  if isdir or size>128*1024*1024:raise ValueError('Archivo inválido o demasiado grande: '+name)
  return self.read(self.base+sector*2048,size)
def folder_file(root,name):
 p=root
 for part in name.split('/'):
  matches=[x for x in p.iterdir() if x.name.lower()==part.lower()]
  if len(matches)!=1:raise ValueError('No se encuentra '+name)
  p=matches[0]
 if not p.is_file() or p.stat().st_size>128*1024*1024:raise ValueError('Archivo inválido: '+name)
 return p.read_bytes()
def prepare(source,root):
 source=source.resolve(strict=True);disc=None
 try:
  if source.is_dir():load=lambda n:folder_file(source,n)
  else:disc=Xiso(source);load=disc.load
  # Check executable before writing any files. Only this revision is supported.
  xbe=load('default.xbe')
  if hashlib.sha1(xbe).hexdigest()!=EXPECTED:raise ValueError('Versión de default.xbe incompatible. Se necesita SHA-1 '+EXPECTED)
  inputs={'default.xbe':xbe,'gamedata/english.xbr':load('gamedata/english.xbr'),'english.txt':load('english.txt')}
  assets=root/'assets'
  # Never write through a user's existing junction or into their source folder.
  if assets.exists() and (assets.resolve()!=assets.absolute() or assets.resolve()==source):raise ValueError('assets debe ser una carpeta local del proyecto, no un enlace ni la carpeta original.')
  for name,data in inputs.items():
   dest=assets/name;dest.parent.mkdir(parents=True,exist_ok=True)
   if dest.resolve()!=dest.absolute():raise ValueError('Destino enlazado no permitido')
   if not dest.exists() or dest.read_bytes()!=data:
    tmp=dest.with_suffix(dest.suffix+'.tmp');tmp.write_bytes(data);os.replace(tmp,dest)
  (root/'game-source.txt').write_text(str(source),encoding='utf-8')
  print('Copia compatible. Entradas locales preparadas; archivos originales intactos.')
 finally:
  if disc:disc.close()
if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('source',nargs='?');args=parser.parse_args();root=Path(__file__).resolve().parents[1]
 try:
  source=args.source or (root/'game-source.txt').read_text(encoding='utf-8').strip()
  prepare(Path(source),root)
 except (OSError,ValueError,UnicodeError) as e:parser.exit(1,str(e)+'\n')
