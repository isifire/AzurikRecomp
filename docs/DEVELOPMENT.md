# Desarrollo y distribución

Estas herramientas solo son necesarias para desarrollar; el usuario final utiliza el ZIP precompilado.

Instala Git, Python 3.12 x64, Visual Studio 2022 / Build Tools con C++, Windows SDK, LLVM/Clang, CMake y Ninja.

```powershell
python tools/prepare_game.py "D:\Mis juegos\Azurik.iso"
.\build.bat
.\Compilar-Lanzador.bat
python -m venv out/packaging-env
.\out\packaging-env\Scripts\python.exe -m pip install Pillow==12.2.0 PyInstaller==6.22.3
.\out\packaging-env\Scripts\python.exe tools/build_installer.py
python -m unittest discover -s tests -v
python tools/audit_public.py
# Revisa y confirma los cambios antes de empaquetar:
python tools/package_release.py
```

El generador necesita tu propia copia compatible; no la publiques. CMake descarga dependencias fijadas en el SDK. `build_installer.py` empaqueta el preparador offline, compila el generador de traducción y copia las bibliotecas redistribuibles de Visual Studio. El ZIP de publicación usa una lista explícita de componentes y excluye recursos originales, traducciones generadas, rutas privadas y herramientas de desarrollo.

Para validar el flujo final, extrae el ZIP en otra carpeta y selecciona una ISO o carpeta propia desde el lanzador. La preparación no debe buscar Python, pip, Visual Studio ni Internet. Las comprobaciones del importador usan imágenes sintéticas; no prueban una partida completa.
