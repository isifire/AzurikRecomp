@echo off
setlocal
cd /d "%~dp0"
where python >nul 2>nul || (echo Instala Python 3.11 o posterior. & exit /b 1)
python tools\prepare_game.py
if errorlevel 1 exit /b 1
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" (echo Instala Visual Studio 2022 con desarrollo de escritorio C++. & exit /b 1)
for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do set "VSDIR=%%i"
if not defined VSDIR (echo No se encuentra el toolchain C++. & exit /b 1)
call "%VSDIR%\Common7\Tools\VsDevCmd.bat" -arch=amd64 -host_arch=amd64 >nul
set "PATH=%ProgramFiles%\LLVM\bin;%VSDIR%\VC\Tools\Llvm\x64\bin;%VSDIR%\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin;%VSDIR%\Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja;%PATH%"
cmake --preset release
if errorlevel 1 exit /b 1
cmake --build --preset release --parallel 4
if errorlevel 1 exit /b 1
python -c "import PIL" >nul 2>nul
if errorlevel 1 (echo Juego compilado en ingles. Para castellano instala Pillow y ejecuta Compilar-Traduccion.bat. & exit /b 0)
call Compilar-Traduccion.bat
exit /b %errorlevel%
