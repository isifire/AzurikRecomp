@echo off
setlocal
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo No se encontro el compilador de .NET Framework.
  exit /b 1
)
"%CSC%" /nologo /target:winexe /optimize+ /out:Lanzador.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll launcher\Program.cs
exit /b %errorlevel%
