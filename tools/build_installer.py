"""Developer-only build of the offline end-user preparation engine (Windows)."""
import os, shutil, subprocess, sys
from pathlib import Path

root = Path(__file__).resolve().parents[1]
os.chdir(root)
subprocess.run([sys.executable, '-m', 'PyInstaller', '--noconfirm', '--clean',
    '--onedir', '--name', 'PrepararJuego', '--distpath', 'out/frozen',
    '--workpath', 'out/freeze-work', '--specpath', 'out',
    '--hidden-import', 'extract_localization', '--hidden-import', 'prepare_translation_text',
    '--hidden-import', 'encode_localization', '--hidden-import', 'rebuild_localization',
    '--hidden-import', 'PIL.DdsImagePlugin', 'tools/gui_prepare.py'], check=True)
shutil.copytree(root/'out/frozen/PrepararJuego', root/'installer', dirs_exist_ok=True)
csc = Path(os.environ['WINDIR'])/'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
subprocess.run([str(csc), '/nologo', '/optimize+', '/out:installer/LocalizeTextures.exe',
    '/reference:System.Drawing.dll', '/reference:System.Web.Extensions.dll',
    'tools/LocalizeTextures.cs'], check=True)
vswhere = Path(os.environ['ProgramFiles(x86)'])/'Microsoft Visual Studio/Installer/vswhere.exe'
vs = Path(subprocess.check_output([str(vswhere), '-latest', '-products', '*',
    '-requires', 'Microsoft.VisualStudio.Component.VC.Tools.x86.x64',
    '-property', 'installationPath'], text=True).strip())
versions = sorted((vs/'VC/Redist/MSVC').glob('*/x64/Microsoft.VC143.CRT'))
if not versions:
    raise SystemExit('Visual Studio x64 redistributable libraries not found')
target = root/'out/build/release'
target.mkdir(parents=True, exist_ok=True)
for dll in versions[-1].glob('*.dll'):
    shutil.copy2(dll, target/dll.name)
print('Offline installer and app-local runtime ready. Commit source changes, then package_release.py.')
