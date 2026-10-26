"""Audit the tracked publication, not ignored local game/build inputs."""
import subprocess
from pathlib import Path
files=subprocess.check_output(['git','ls-files','-z']).decode().split('\0')
blocked=[]
for name in filter(None,files):
 p=Path(name)
 if p.suffix.lower() in {'.iso','.xiso','.xbe','.xbr','.dds','.bmp','.png','.exe','.dll','.dmp','.pdb','.wav','.bik'} or name.startswith(('assets/','generated/','out/','reports/')) or p.name in {'game-source.txt','controles.ini','settings.toml','setup.log'}:blocked.append(name)
 if p.is_file() and b'\x00' in p.read_bytes():blocked.append(name+' (binary)')
if blocked:raise SystemExit('Blocked publication files: '+', '.join(blocked))
print('PASS: tracked files contain no game assets, binaries, generated code or private runtime files')
