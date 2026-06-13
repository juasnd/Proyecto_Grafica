using VisualBeatPlayer.Models;
using VisualBeatPlayer.Services;
using VisualBeatPlayer.Views;

namespace VisualBeatPlayer.Controllers;

public class ReproductorController : IDisposable
{
    private readonly IReproductorView _vista;
    private readonly PlaylistService _playlistService;
    private readonly AudioAnalyzerService _audioAnalyzerService;
    private readonly AudioPlayerService _audioPlayerService;
    private readonly ThemeService _themeService;

    public ReproductorController(IReproductorView vista)
    {
        _vista = vista;
        _playlistService = new PlaylistService();
        _audioAnalyzerService = new AudioAnalyzerService();
        _audioPlayerService = new AudioPlayerService(_audioAnalyzerService);
        _themeService = new ThemeService();

        _audioPlayerService.ReproduccionFinalizada += (_, _) =>
        {
            _vista.EjecutarEnUI(() => Siguiente(reproducirAutomaticamente: true));
        };
    }

    public void Inicializar()
    {
        _vista.AplicarTema(_themeService.ObtenerPaletaActual(), _themeService.ModoActual);
        _vista.MostrarCanciones(_playlistService.Canciones, _playlistService.IndiceActual);
        _vista.ActualizarControles(_playlistService.TieneCanciones);
        _vista.MostrarCancionActual(null);
        _vista.ActualizarProgreso(TimeSpan.Zero, TimeSpan.Zero);
    }

    public void CargarCanciones(IEnumerable<string> rutas)
    {
        string[] rutasValidas = rutas
            .Where(PlaylistService.EsArchivoAudioValido)
            .ToArray();

        if (rutasValidas.Length == 0)
        {
            _vista.MostrarMensaje("Selecciona al menos un archivo de audio valido.", "Archivo no valido");
            return;
        }

        bool estabaVacia = !_playlistService.TieneCanciones;
        IReadOnlyList<Cancion> agregadas = _playlistService.CargarCanciones(rutasValidas);

        if (agregadas.Count == 0)
        {
            _vista.MostrarMensaje("No se agregaron canciones nuevas. Revisa si ya estaban en la lista.", "Lista sin cambios");
            return;
        }

        if (estabaVacia)
        {
            CargarCancionActual();
        }

        _vista.MostrarCanciones(_playlistService.Canciones, _playlistService.IndiceActual);
        _vista.ActualizarControles(_playlistService.TieneCanciones);
    }

    public void SeleccionarCancion(int indice)
    {
        if (!_playlistService.SeleccionarCancion(indice))
        {
            return;
        }

        CargarCancionActual();
        _vista.MostrarCanciones(_playlistService.Canciones, _playlistService.IndiceActual);
    }

    public void QuitarCancion(int indice)
    {
        if (!_playlistService.TieneCanciones || indice < 0)
        {
            _vista.MostrarMensaje("Selecciona una cancion de la lista para quitarla.", "Sin seleccion");
            return;
        }

        bool quitandoActual = indice == _playlistService.IndiceActual;
        bool estabaReproduciendo = _audioPlayerService.EstaReproduciendo;

        if (quitandoActual)
        {
            _audioPlayerService.Detener();
        }

        if (!_playlistService.QuitarCancion(indice))
        {
            return;
        }

        if (!_playlistService.TieneCanciones)
        {
            _vista.MostrarCancionActual(null);
            _vista.ActualizarProgreso(TimeSpan.Zero, TimeSpan.Zero);
        }
        else if (quitandoActual)
        {
            CargarCancionActual();

            if (estabaReproduciendo)
            {
                Reproducir();
            }
        }

        _vista.MostrarCanciones(_playlistService.Canciones, _playlistService.IndiceActual);
        _vista.ActualizarControles(_playlistService.TieneCanciones);
    }

    public void LimpiarLista()
    {
        if (!_playlistService.TieneCanciones)
        {
            return;
        }

        _audioPlayerService.Detener();
        _playlistService.Limpiar();
        _vista.MostrarCanciones(_playlistService.Canciones, _playlistService.IndiceActual);
        _vista.ActualizarControles(false);
        _vista.MostrarCancionActual(null);
        _vista.ActualizarProgreso(TimeSpan.Zero, TimeSpan.Zero);
    }

    public void Reproducir()
    {
        if (!_playlistService.TieneCanciones)
        {
            _vista.MostrarMensaje("Primero carga una o varias canciones MP3.", "Sin musica");
            return;
        }

        if (!_audioPlayerService.EstaCargado)
        {
            CargarCancionActual();
        }

        try
        {
            _audioPlayerService.Reproducir();
        }
        catch (Exception ex)
        {
            _vista.MostrarMensaje($"No se pudo reproducir la cancion: {ex.Message}", "Error de reproduccion");
        }
    }

    public void Pausar()
    {
        _audioPlayerService.Pausar();
    }

    public void Detener()
    {
        _audioPlayerService.Detener();
        _vista.ActualizarProgreso(TimeSpan.Zero, _audioPlayerService.DuracionActual);
    }

    public void Siguiente(bool reproducirAutomaticamente = true)
    {
        if (!_playlistService.TieneCanciones)
        {
            _vista.MostrarMensaje("La lista esta vacia.", "Sin canciones");
            return;
        }

        _playlistService.Siguiente();
        CargarCancionActual();

        if (reproducirAutomaticamente)
        {
            Reproducir();
        }

        _vista.MostrarCanciones(_playlistService.Canciones, _playlistService.IndiceActual);
    }

    public void Anterior()
    {
        if (!_playlistService.TieneCanciones)
        {
            _vista.MostrarMensaje("La lista esta vacia.", "Sin canciones");
            return;
        }

        _playlistService.Anterior();
        CargarCancionActual();
        Reproducir();
        _vista.MostrarCanciones(_playlistService.Canciones, _playlistService.IndiceActual);
    }

    public void CambiarVolumen(int porcentaje)
    {
        _audioPlayerService.CambiarVolumen(Math.Clamp(porcentaje, 0, 100) / 100f);
    }

    public void CambiarPosicion(int segundos)
    {
        _audioPlayerService.CambiarPosicion(TimeSpan.FromSeconds(segundos));
        ActualizarProgreso();
    }

    public void AlternarTema()
    {
        PaletaTema paleta = _themeService.AlternarModo();
        _vista.AplicarTema(paleta, _themeService.ModoActual);
    }

    public void ActualizarProgreso()
    {
        _vista.ActualizarProgreso(_audioPlayerService.PosicionActual, _audioPlayerService.DuracionActual);
    }

    public AudioSpectrumData ObtenerEspectro()
    {
        return _audioAnalyzerService.ObtenerEspectro(72);
    }

    public void Dispose()
    {
        _audioPlayerService.Dispose();
        GC.SuppressFinalize(this);
    }

    private void CargarCancionActual()
    {
        Cancion? cancion = _playlistService.CancionActual;
        if (cancion is null)
        {
            return;
        }

        try
        {
            _audioPlayerService.Cargar(cancion);
            _vista.MostrarCancionActual(cancion);
            _vista.ActualizarProgreso(TimeSpan.Zero, cancion.Duracion);
        }
        catch (Exception ex)
        {
            _vista.MostrarMensaje($"No se pudo cargar el archivo seleccionado: {ex.Message}", "Error de carga");
        }
    }
}
