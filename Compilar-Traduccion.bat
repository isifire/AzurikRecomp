@echo off
setlocal
cd /d "%~dp0"
python tools\extract_localization.py
if errorlevel 1 exit /b 1
python tools\prepare_translation_text.py
if errorlevel 1 exit /b 1
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
"%CSC%" /nologo /optimize+ /out:tools\LocalizeTextures.exe /reference:System.Drawing.dll /reference:System.Web.Extensions.dll tools\LocalizeTextures.cs
if errorlevel 1 exit /b 1
tools\LocalizeTextures.exe
if errorlevel 1 exit /b 1
python tools\encode_localization.py
if errorlevel 1 exit /b 1
python tools\rebuild_localization.py
if errorlevel 1 exit /b 1
if not exist "out\build\release\localization\es-ES\gamedata" mkdir "out\build\release\localization\es-ES\gamedata"
copy /y "translation\es\gamedata\english.xbr" "out\build\release\localization\es-ES\gamedata\english.xbr" >nul
if errorlevel 1 exit /b 1
copy /y "translation\es\english.txt" "out\build\release\localization\es-ES\english.txt" >nul
if errorlevel 1 exit /b 1
tools\LocalizeTextures.exe large
if errorlevel 1 exit /b 1
python tools\encode_localization.py --large
if errorlevel 1 exit /b 1
python tools\rebuild_localization.py --replacements translation/es/resources-large --output translation/es/gamedata/english-large.xbr
if errorlevel 1 exit /b 1
copy /y "translation\es\gamedata\english-large.xbr" "out\build\release\localization\es-ES\gamedata\english-large.xbr" >nul
exit /b %errorlevel%
