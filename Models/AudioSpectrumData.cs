namespace VisualBeatPlayer.Models;

public class AudioSpectrumData
{
    public AudioSpectrumData(float[] frecuencias)
    {
        Frecuencias = frecuencias;
        CapturadoEn = DateTime.Now;
    }

    public float[] Frecuencias { get; }
    public DateTime CapturadoEn { get; }

    public static AudioSpectrumData Vacio(int cantidadBarras = 72)
    {
        return new AudioSpectrumData(new float[cantidadBarras]);
    }
}
