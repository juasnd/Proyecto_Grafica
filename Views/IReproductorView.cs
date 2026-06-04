using VisualBeatPlayer.Models;

namespace VisualBeatPlayer.Views;

public interface IReproductorView
{
    void MostrarCanciones(IReadOnlyList<Cancion> canciones, int indiceActual);
    void MostrarCancionActual(Cancion? cancion);
    void ActualizarProgreso(TimeSpan posicion, TimeSpan duracion);
    void ActualizarControles(bool hayCanciones);
    void AplicarTema(PaletaTema paleta, ModoTema modo);
    void MostrarMensaje(string mensaje, string titulo);
    void EjecutarEnUI(Action accion);
}
