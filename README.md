# AzurikRecomp

Recompilación experimental para Windows de **Azurik: Rise of Perathia**, de Xbox original, basada en [XboxXBERecomp](https://github.com/TGP482/XboxXBERecomp).

**No incluye el juego. Aporta tu propia ISO/XISO o carpeta extraída con `default.xbe`.** No se proporcionan enlaces de descarga del juego.

## Jugar con el paquete precompilado

1. Descarga y extrae el ZIP de [Releases](https://github.com/isifire/AzurikRecomp/releases) en una carpeta donde tengas permiso de escritura.
2. Abre `Lanzador.exe`, pestaña **Juego**.
3. Elige tu ISO/XISO o la carpeta extraída que contiene `default.xbe` y las carpetas del juego.
4. Espera a que el lanzador prepare automáticamente el juego y los textos. La primera preparación puede tardar unos minutos; verás el progreso en la ventana.
5. Elige **English** o **Castellano**, configura gráficos, filtros, controles y texto ampliado, y pulsa **Guardar y jugar**.

**No necesitas compilar, instalar Python ni descargar herramientas.** Todos los componentes de preparación están incluidos y funcionan sin conexión. Descarga el ZIP `AzurikRecomp-windows-x64-experimental.zip` de la versión **v0.2.0-experimental o posterior**, no el archivo de código fuente de GitHub.

Requisitos: Windows 10/11 de 64 bits con .NET Framework 4.8, CPU x86-64-v2 y GPU compatible con Direct3D 12. Las bibliotecas Visual C++ necesarias se incluyen junto al ejecutable. El juego se lee desde la ruta seleccionada, que debe permanecer accesible. No se modifica la copia original.

### Versión compatible

Esta compilación solo admite el `default.xbe` cuyo SHA-1 es:

`a989c8b0a67147856f77518ad3823bb6c31dff0c`

Title ID: `4D530007`. Otras ediciones requieren adaptar y verificar la recompilación; se rechazan para evitar ejecutar código incompatible. Se admiten imágenes XISO y volcados con partición Xbox en `0x18300000`, sin comprimir. No se admiten ZIP, CHD ni discos divididos.

### Castellano integrado

Al seleccionar la copia del juego, el lanzador genera localmente los textos traducidos y su variante ampliada. Puedes cambiar de idioma antes de jugar en los ajustes. Los originales necesarios se leen de tu copia: no se distribuyen en el paquete.

Botones del mando con iconos y paneles oscuros opacos. Las voces siguen en inglés. La letra ampliada se aplica a avisos/tutoriales; no añade subtítulos a cinemáticas. Al reasignar el mando, los avisos muestran el botón lógico original.

## Funciones y estado

- Selección de ISO o carpeta, controles de teclado y mando, reasignación de botones, sensibilidad/inversión de cámara y vibración.
- Resolución interna, perfiles Original/Equilibrado/Calidad, filtro anisotrópico, suavizado, nitidez, brillo, contraste y saturación.
- Reescalado bilineal, vecino próximo y bicúbico; caché local de shaders.
- Diagnóstico opcional con tiempos por fotograma y memoria. `tools/analyze_frame_times.py` resume los registros.

**Es experimental, no una versión terminada.** Persisten problemas del agua/transparencias, efectos que desaparecen según distancia y posibles tirones. No se garantiza completar el juego ni la estabilidad de sesiones largas. La lógica del juego puede mantenerse a 30 Hz aunque el límite del lanzador sea superior.

La configuración, partidas, cachés y registros quedan dentro de esta instalación. No publiques `assets`, `generated`, `out`, partidas, volcados o registros con datos personales. Para reportar problemas, indica versión, GPU y pasos para reproducirlo; revisa cualquier archivo antes de adjuntarlo.

## Desarrollo

El código y las instrucciones para desarrolladores están en el [repositorio](https://github.com/isifire/AzurikRecomp), en `docs/DEVELOPMENT.md`. Compilar es únicamente para quienes quieran modificar el proyecto.

## Pruebas y licencias

`python -m unittest discover -s tests -v` comprueba importación y lectura de imágenes sintéticas, sin usar archivos del juego. Las herramientas `check_*` permiten comprobar muestreo, control y shaders sin abrir una partida.

Runtime/SDK bajo MIT; se conservan las licencias de las dependencias en `sdk/THIRD_PARTY.txt`. Consulta [UPSTREAM.md](UPSTREAM.md). Proyecto de aficionados, no oficial; los nombres y archivos originales pertenecen a sus titulares. No se incluyen ISO, XBE, recursos gráficos, audio ni partidas originales en el repositorio o paquete.
