@echo off
setlocal
cd /d "%~dp0"
if not exist Lanzador.exe call Compilar-Lanzador.bat
if errorlevel 1 exit /b 1
start "" "%~dp0Lanzador.exe"
