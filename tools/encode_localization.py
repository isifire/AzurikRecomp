"""Encode the localized UI mipmaps into same-size XBR surface resources."""
from pathlib import Path
import os
import json,io,hashlib,sys
large="--large" in sys.argv
from PIL import Image
root=Path(os.environ.get('AZURIK_PACKAGE_ROOT',Path(__file__).resolve().parents[1]))
m=json.loads((root/'translation/manifest.json').read_text());original=(root/'assets/gamedata/english.xbr').read_bytes()
assert hashlib.sha256(original).hexdigest()==m['source_sha256']
output=root/('translation/es/resources-large' if large else 'translation/es/resources');output.mkdir(exist_ok=True)
for r in m['entries']:
 folder=root/('translation/es/rendered-large' if large else 'translation/es/rendered')/f'{r["id"]:04}'
 if not folder.exists():continue
 payload=bytearray(original[r['offset']:r['offset']+r['size']]);pos=128;level=0
 while (folder/f'mip{level}.png').exists():
  im=Image.open(folder/f'mip{level}.png').convert('RGBA');buf=io.BytesIO();im.save(buf,format='DDS',pixel_format=r['format']);data=buf.getvalue()[128:]
  expected=max(1,(im.width+3)//4)*max(1,(im.height+3)//4)*(8 if r['format']=='DXT1' else 16)
  assert len(data)==expected
  assert pos+len(data)<=len(payload),(r['id'],pos,len(data),len(payload))
  payload[pos:pos+len(data)]=data;pos+=len(data);level+=1
 # Archive allocation padding must contain no residual English mip data.
 payload[pos:]=bytes(len(payload)-pos)
 (output/f'{r["id"]:04}.bin').write_bytes(payload)
print('Encoded',len(list(output.glob('*.bin'))),'localized resources')
