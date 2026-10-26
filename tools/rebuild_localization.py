"""Rebuild localized XBR from exact-size resource replacements; never edits source."""
import argparse,hashlib,json
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--source',type=Path,default=Path('assets/gamedata/english.xbr'));p.add_argument('--manifest',type=Path,default=Path('translation/manifest.json'));p.add_argument('--replacements',type=Path,default=Path('translation/es/resources'));p.add_argument('--output',type=Path,default=Path('translation/es/gamedata/english.xbr'));a=p.parse_args()
if a.output.resolve()==a.source.resolve():p.error('Output must not overwrite original')
b=a.source.read_bytes();m=json.loads(a.manifest.read_text());assert hashlib.sha256(b).hexdigest()==m['source_sha256'],'Source differs from extracted archive'
out=bytearray(b);count=0
for r in m['entries']:
 replacement=a.replacements/f'{r["id"]:04d}.bin'
 if not replacement.exists():continue
 payload=replacement.read_bytes()
 assert len(payload)==r['size'],f'Wrong resource size: {replacement}'
 if r['type']=='surf':assert payload[:128]==b[r['offset']:r['offset']+128],'Surface metadata must remain unchanged'
 out[r['offset']:r['offset']+r['size']]=payload;count+=1
assert len(out)==len(b)
a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_bytes(out)
print(f'{count} resources replaced; {len(out)} bytes; sha256 {hashlib.sha256(out).hexdigest()}')
