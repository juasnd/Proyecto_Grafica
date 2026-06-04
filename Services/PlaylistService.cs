using NAudio.Wave;
using VisualBeatPlayer.Models;

namespace VisualBeatPlayer.Services;

public class PlaylistService
{
    private readonly List<Cancion> _canciones = [];
    private int _indiceActual = -1;

    public IReadOnlyList<Cancion> Canciones => _canciones;
    public int IndiceActual => _indiceActual;
    public Cancion? CancionActual => _indiceActual >= 0 && _indiceActual < _canciones.Count ? _canciones[_indiceActual] : null;
    public bool TieneCanciones => _canciones.Count > 0;

    public IReadOnlyList<Cancion> CargarCanciones(IEnumerable<string> rutas)
    {
        List<Cancion> cancionesAgregadas = [];

        foreach (string ruta in rutas)
        {
            if (!EsArchivoMp3Valido(ruta) || YaExiste(ruta))
            {
                continue;
            }

            Cancion cancion = new(ruta)
            {
                Duracion = ObtenerDuracion(ruta),
                EstaCargada = true
            };

            _canciones.Add(cancion);
            cancionesAgregadas.Add(cancion);
        }

        if (_indiceActual == -1 && _canciones.Count > 0)
        {
            _indiceActual = 0;
        }

        return cancionesAgregadas;
    }

    public bool SeleccionarCancion(int indice)
    {
        if (indice < 0 || indice >= _canciones.Count)
        {
            return false;
        }

        _indiceActual = indice;
        return true;
    }

    public Cancion? Siguiente()
    {
        if (_canciones.Count == 0)
        {
            return null;
        }

        _indiceActual = (_indiceActual + 1) % _canciones.Count;
        return CancionActual;
    }

    public Cancion? Anterior()
    {
        if (_canciones.Count == 0)
        {
            return null;
        }

        _indiceActual = (_indiceActual - 1 + _canciones.Count) % _canciones.Count;
        return CancionActual;
    }

    private static bool EsArchivoMp3Valido(string ruta)
    {
        return File.Exists(ruta) && string.Equals(Path.GetExtension(ruta), ".mp3", StringComparison.OrdinalIgnoreCase);
    }

    private bool YaExiste(string ruta)
    {
        return _canciones.Any(c => string.Equals(c.Ruta, ruta, StringComparison.OrdinalIgnoreCase));
    }

    private static TimeSpan ObtenerDuracion(string ruta)
    {
        try
        {
            using AudioFileReader lector = new(ruta);
            return lector.TotalTime;
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }
}
