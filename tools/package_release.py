"""Package only tracked sources plus two explicitly allowed executables."""
from pathlib import Path
import hashlib,subprocess,sys,zipfile
root=Path(__file__).resolve().parents[1]
subprocess.run([sys.executable,str(root/'tools/audit_public.py')],cwd=root,check=True)
if subprocess.check_output(['git','status','--porcelain','--untracked-files=no'],cwd=root).strip():raise SystemExit('Commit changes before packaging')
files=[x for x in subprocess.check_output(['git','ls-files','-z'],cwd=root).decode().split('\0') if x]
extras=['Lanzador.exe','out/build/release/Azurik.exe']
for name in extras:
 if not (root/name).is_file() or (root/name).read_bytes()[:2]!=b'MZ':raise SystemExit('Missing executable: '+name)
out=root/'out/packages';out.mkdir(parents=True,exist_ok=True);dest=out/'AzurikRecomp-windows-x64-experimental.zip'
with zipfile.ZipFile(dest,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
 for name in files+extras:z.write(root/name,'AzurikRecomp/'+name)
with zipfile.ZipFile(dest) as z:
 assert z.testzip() is None
 for n in z.namelist():
  if Path(n).suffix.lower() in {'.iso','.xiso','.xbe','.xbr','.png','.bmp','.dds','.pdb','.dmp','.wav','.bik'}:raise SystemExit('Unexpected game/private file: '+n)
  if Path(n).name in {'game-source.txt','settings.toml','controles.ini','setup.log'}:raise SystemExit('Private file: '+n)
digest=hashlib.sha256(dest.read_bytes()).hexdigest();(out/'SHA256SUMS.txt').write_text(digest+'  '+dest.name+'\n')
print(dest);print('SHA256',digest)
