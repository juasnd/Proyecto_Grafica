namespace VisualBeatPlayer.Models;

public class AudioSpectrumData
{
    public AudioSpectrumData(
        double[] frecuencias,
        double[] magnitudes,
        double energiaTotal,
        double rms,
        double bajos,
        double medios,
        double agudos,
        bool pulsoDetectado,
        double promedioMovil)
    {
        Frecuencias = frecuencias;
        Magnitudes = magnitudes;
        EnergiaTotal = energiaTotal;
        Rms = rms;
        Bajos = bajos;
        Medios = medios;
        Agudos = agudos;
        PulsoDetectado = pulsoDetectado;
        PromedioMovil = promedioMovil;
        CapturadoEn = DateTime.Now;
    }

    public double[] Frecuencias { get; }
    public double[] Magnitudes { get; }
    public double EnergiaTotal { get; }
    public double Rms { get; }
    public double Bajos { get; }
    public double Medios { get; }
    public double Agudos { get; }
    public bool PulsoDetectado { get; }
    public double PromedioMovil { get; }
    public DateTime CapturadoEn { get; }

    public static AudioSpectrumData Vacio(int cantidadBarras = 96)
    {
        return new AudioSpectrumData(
            new double[cantidadBarras],
            new double[cantidadBarras],
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            false,
            0.0);
    }
}
