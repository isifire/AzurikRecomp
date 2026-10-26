from pathlib import Path
import struct,json,hashlib
from PIL import Image
root=Path('translation');root.mkdir(exist_ok=True)
b=Path('assets/gamedata/english.xbr').read_bytes();u=lambda o:struct.unpack_from('<I',b,o)[0]
names={}
for j in range(u(44)):
 i,n=struct.unpack_from('<II',b,u(48)+j*8);names[i]=b[u(52)+n:].split(b'\0',1)[0].decode()
rows=[]
for i in range(u(12)):
 off,size,t,flags=struct.unpack_from('<II4sI',b,60+i*16);off+=u(16)
 assert off+size<=len(b)
 row=dict(id=i,name=names.get(i,''),type=t.decode(),offset=off,size=size,flags=flags)
 if t==b'surf':
  w,h=map(int,struct.unpack_from('<ff',b,off+28));four=b'DXT1' if u(off)==0x10000020 else b'DXT3'
  row.update(width=w,height=h,format=four.decode())
  hdr=bytearray(128);hdr[:4]=b'DDS ';struct.pack_into('<7I',hdr,4,124,0x81007,h,w,w*h*(8 if four==b'DXT1' else 16)//16,0,1);struct.pack_into('<II4s',hdr,76,32,4,four);struct.pack_into('<I',hdr,108,0x1000)
  p=root/'extracted'/Path(names[i]).with_suffix('.dds');p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(hdr+b[off+128:off+size]);Image.open(p).transpose(Image.Transpose.FLIP_TOP_BOTTOM).save(p.with_suffix('.png'))
 rows.append(row)
(root/'manifest.json').write_text(json.dumps(dict(source_sha256=hashlib.sha256(b).hexdigest(),entries=rows),indent=2))
print('Extracted',sum(r['type']=='surf' for r in rows),'textures')
