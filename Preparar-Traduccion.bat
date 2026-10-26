@echo off
setlocal
cd /d "%~dp0"
python tools\prepare_game.py
if errorlevel 1 exit /b 1
call Compilar-Traduccion.bat
exit /b %errorlevel%
