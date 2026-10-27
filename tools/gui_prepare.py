"""Frozen, offline game setup. End users never invoke Python or a compiler."""
import hashlib,json,os,runpy,subprocess,sys
from pathlib import Path
import prepare_game
VERSION=1

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def run(module,args=()):
 old=sys.argv;sys.argv=[module,*args]
 try:runpy.run_module(module,run_name='__main__')
 finally:sys.argv=old

def prepare(root):
 os.chdir(root);os.environ['AZURIK_PACKAGE_ROOT']=str(root)
 source=Path((root/'prepare-source.txt').read_text(encoding='utf-8-sig').strip())
 print('Comprobando tu copia del juego...',flush=True)
 prepare_game.prepare(source,root)
 # Translation toolchain is tied to the known original archive, not a guessed layout.
 if sha(root/'assets/gamedata/english.xbr')!='0702f131e9349d5fed6677d14aadc81eb4bb5ccb7284e51a9f0a468fd554dcfc':
  raise ValueError('El archivo de textos no corresponde a la versión compatible.')
 output=root/'out/build/release/localization/es-ES'
 marker=root/'.prepared.json'
 targets=[output/'english.txt',output/'gamedata/english.xbr',output/'gamedata/english-large.xbr']
 try:
  saved=json.loads(marker.read_text())
  if saved['version']==VERSION and all(p.is_file() and sha(p)==saved['hashes'][p.name] for p in targets):
   print('Listo: inglés, castellano y texto ampliado disponibles.',flush=True);return
 except (OSError,ValueError,KeyError):pass
 print('Preparando los textos e iconos del mando...',flush=True)
 run('extract_localization');run('prepare_translation_text')
 renderer=root/'installer/LocalizeTextures.exe'
 for large in (False,True):
  print('Generando castellano'+(' con texto ampliado...' if large else '...'),flush=True)
  subprocess.run([str(renderer)]+(['large'] if large else []),check=True,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
  run('encode_localization',['--large'] if large else [])
  run('rebuild_localization',['--replacements','translation/es/resources-large','--output','translation/es/gamedata/english-large.xbr'] if large else [])
 # Replace complete outputs only after all generation stages succeed. A failed setup
 # leaves no success marker, so the next attempt retries instead of accepting partial work.
 pairs=[(root/'translation/es/english.txt',targets[0]),(root/'translation/es/gamedata/english.xbr',targets[1]),(root/'translation/es/gamedata/english-large.xbr',targets[2])]
 for src,dst in pairs:
  dst.parent.mkdir(parents=True,exist_ok=True);tmp=dst.with_suffix(dst.suffix+'.tmp');tmp.write_bytes(src.read_bytes());os.replace(tmp,dst)
 marker.write_text(json.dumps({'version':VERSION,'hashes':{p.name:sha(p) for p in targets}}),encoding='utf-8')
 print('Listo: inglés, castellano y texto ampliado disponibles.',flush=True)

if __name__=='__main__':
 try:
  if hasattr(sys.stdout,'reconfigure'):sys.stdout.reconfigure(encoding='utf-8')
  root=Path(sys.executable).resolve().parent.parent if getattr(sys,'frozen',False) else Path(__file__).resolve().parents[1]
  prepare(root)
 except Exception as e:
  print('No se pudo preparar el juego: '+str(e),flush=True);sys.exit(1)
