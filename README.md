# VisualBeatPlayer

Reproductor de musica local MP3 creado con C# Windows Forms, arquitectura MVC adaptada y visualizador de audio en tiempo real usando FFT.

## Estructura

```text
VisualBeatPlayer/
  Controllers/
    ReproductorController.cs
  Models/
    Cancion.cs
    AudioSpectrumData.cs
    ModoTema.cs
    PaletaTema.cs
  Services/
    AudioPlayerService.cs
    AudioAnalyzerService.cs
    PlaylistService.cs
    ThemeService.cs
  Utils/
    DibujoHelper.cs
  Views/
    FrmReproductor.cs
    IReproductorView.cs
    VisualizerControl.cs
    RoundButton.cs
    BarraDeslizante.cs
    PanelTarjeta.cs
  Program.cs
  VisualBeatPlayer.csproj
```

## Paquetes NuGet

Desde Visual Studio 2022:

1. Abrir el proyecto `VisualBeatPlayer.csproj`.
2. Ir a `Herramientas > Administrador de paquetes NuGet > Administrar paquetes NuGet para la solucion`.
3. Instalar o restaurar:
   - `NAudio` version `2.2.1`

Desde consola:

```powershell
dotnet restore
```

## Como ejecutar

1. Abrir `VisualBeatPlayer.sln` o `VisualBeatPlayer.csproj` en Visual Studio 2022.
2. Restaurar NuGet si Visual Studio lo solicita.
3. Presionar `F5`.
4. Usar `Cargar MP3` para seleccionar una o varias canciones.
5. Usar `Play`, `Pause`, `Stop`, `Anterior`, `Siguiente`, barra de progreso, volumen y selector de tema.

## Como se aplica MVC

- `Views`: contienen la interfaz visual. `FrmReproductor` solo atiende eventos de botones, lista y barras, y llama al controlador.
- `Controllers`: `ReproductorController` conecta la vista con los servicios. Decide que ocurre al cargar, reproducir, pausar, avanzar, cambiar volumen o tema.
- `Models`: representan los datos principales, como `Cancion`, `AudioSpectrumData`, `ModoTema` y `PaletaTema`.
- `Services`: contienen la logica real de audio, playlist, analisis FFT y temas.
- `Utils`: guarda ayuda de dibujo para bordes redondeados y mezcla de colores.

## Como funciona la FFT

`AudioPlayerService` reproduce el MP3 con NAudio. Mientras el audio suena, un proveedor personalizado copia las muestras reales que pasan por la salida de audio y las envia a `AudioAnalyzerService`.

`AudioAnalyzerService` guarda las muestras recientes en un buffer circular. En cada actualizacion toma 4096 muestras, calcula energia total y RMS, aplica una ventana Hann y ejecuta una FFT manual Cooley-Tukey radix-2 escrita dentro del proyecto. Despues calcula magnitudes por bandas logaritmicas, energia de bajos, medios y agudos, y detecta pulsos ritmicos comparando la energia actual contra un promedio movil de 0.5 segundos.

`VisualizerControl` recibe esas barras, las suaviza visualmente y las dibuja con `Graphics`: gradientes, brillo, base luminosa y animacion de espera cuando no hay musica.

## Validaciones incluidas

- Si no hay canciones, los controles de reproduccion quedan desactivados.
- Solo se aceptan archivos `.mp3`.
- Si no hay musica cargada y se intenta reproducir, aparece un mensaje.
- Si falla la carga o reproduccion, se muestra un mensaje claro.
- El tema claro/oscuro cambia sin reiniciar la aplicacion.
