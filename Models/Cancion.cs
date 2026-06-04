namespace VisualBeatPlayer.Models;

public class Cancion
{
    public Cancion(string ruta)
    {
        Ruta = ruta;
        Nombre = Path.GetFileNameWithoutExtension(ruta);
        Artista = "Artista desconocido";
    }

    public string Ruta { get; }
    public string Nombre { get; }
    public string Artista { get; set; }
    public TimeSpan Duracion { get; set; }
    public bool EstaCargada { get; set; }

    public string DuracionTexto
    {
        get
        {
            if (Duracion <= TimeSpan.Zero)
            {
                return "--:--";
            }

            return $"{(int)Duracion.TotalMinutes:00}:{Duracion.Seconds:00}";
        }
    }

    public override string ToString()
    {
        return Nombre;
    }
}
