using System.Numerics;
using MathNet.Numerics.IntegralTransforms;
using VisualBeatPlayer.Models;

namespace VisualBeatPlayer.Services;

public class AudioAnalyzerService
{
    private const int TamanoFft = 4096;
    private const double FrecuenciaMinima = 40.0;
    private const double FrecuenciaMaximaObjetivo = 16000.0;
    private const double PisoDecibeles = -85.0;
    private const double TechoDecibeles = -18.0;
    private readonly object _candado = new();
    private readonly float[] _bufferCircular = new float[16384];
    private int _sampleRate = 44100;
    private int _posicionEscritura;
    private bool _bufferLleno;

    public void ConfigurarFormato(int sampleRate)
    {
        if (sampleRate <= 0)
        {
            return;
        }

        lock (_candado)
        {
            _sampleRate = sampleRate;
        }
    }

    public void AgregarMuestras(float[] muestras)
    {
        lock (_candado)
        {
            foreach (float muestra in muestras)
            {
                _bufferCircular[_posicionEscritura] = muestra;
                _posicionEscritura = (_posicionEscritura + 1) % _bufferCircular.Length;

                if (_posicionEscritura == 0)
                {
                    _bufferLleno = true;
                }
            }
        }
    }

    public AudioSpectrumData ObtenerEspectro(int cantidadBarras)
    {
        if (cantidadBarras <= 0)
        {
            return AudioSpectrumData.Vacio();
        }

        float[] muestras = ObtenerUltimasMuestras();

        if (muestras.All(m => Math.Abs(m) < 0.00008f))
        {
            return AudioSpectrumData.Vacio(cantidadBarras);
        }

        Complex[] fft = new Complex[TamanoFft];

        for (int i = 0; i < TamanoFft; i++)
        {
            double ventanaHann = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / (TamanoFft - 1));
            fft[i] = new Complex(muestras[i] * ventanaHann, 0.0);
        }

        Fourier.Forward(fft, FourierOptions.Matlab);
        return new AudioSpectrumData(ConvertirFftEnBarras(fft, cantidadBarras));
    }

    public void Limpiar()
    {
        lock (_candado)
        {
            Array.Clear(_bufferCircular);
            _posicionEscritura = 0;
            _bufferLleno = false;
        }
    }

    private float[] ObtenerUltimasMuestras()
    {
        float[] resultado = new float[TamanoFft];

        lock (_candado)
        {
            int muestrasDisponibles = _bufferLleno ? _bufferCircular.Length : _posicionEscritura;

            if (muestrasDisponibles < TamanoFft)
            {
                int inicioDestino = TamanoFft - muestrasDisponibles;
                for (int i = 0; i < muestrasDisponibles; i++)
                {
                    resultado[inicioDestino + i] = _bufferCircular[i];
                }

                return resultado;
            }

            int inicio = _posicionEscritura - TamanoFft;
            if (inicio < 0)
            {
                inicio += _bufferCircular.Length;
            }

            for (int i = 0; i < TamanoFft; i++)
            {
                resultado[i] = _bufferCircular[(inicio + i) % _bufferCircular.Length];
            }
        }

        return resultado;
    }

    private float[] ConvertirFftEnBarras(Complex[] fft, int cantidadBarras)
    {
        float[] barras = new float[cantidadBarras];
        int sampleRate;

        lock (_candado)
        {
            sampleRate = _sampleRate;
        }

        double nyquist = sampleRate / 2.0;
        double frecuenciaMaxima = Math.Min(FrecuenciaMaximaObjetivo, nyquist * 0.92);
        double logMin = Math.Log10(FrecuenciaMinima);
        double logMax = Math.Log10(frecuenciaMaxima);

        for (int barra = 0; barra < cantidadBarras; barra++)
        {
            double tInicio = (double)barra / cantidadBarras;
            double tFin = (double)(barra + 1) / cantidadBarras;
            double frecuenciaInicio = Math.Pow(10, logMin + (logMax - logMin) * tInicio);
            double frecuenciaFin = Math.Pow(10, logMin + (logMax - logMin) * tFin);

            int binInicio = Math.Max(1, FrecuenciaABin(frecuenciaInicio, sampleRate));
            int binFin = Math.Max(binInicio + 1, FrecuenciaABin(frecuenciaFin, sampleRate));
            binFin = Math.Min(binFin, fft.Length / 2 - 1);

            double sumaCuadrados = 0.0;
            double maximo = 0.0;
            int cantidad = 0;

            for (int bin = binInicio; bin <= binFin; bin++)
            {
                // Escala dBFS clasica: se compensa la ganancia coherente de la ventana Hann.
                double amplitud = (4.0 * fft[bin].Magnitude) / TamanoFft;
                sumaCuadrados += amplitud * amplitud;
                maximo = Math.Max(maximo, amplitud);
                cantidad++;
            }

            double rms = cantidad == 0 ? 0.0 : Math.Sqrt(sumaCuadrados / cantidad);
            double magnitudBanda = rms * 0.78 + maximo * 0.22;
            magnitudBanda *= ObtenerPesoFrecuencia((frecuenciaInicio + frecuenciaFin) * 0.5);

            double decibeles = 20.0 * Math.Log10(magnitudBanda + 0.000000001);
            double normalizado = (decibeles - PisoDecibeles) / (TechoDecibeles - PisoDecibeles);

            // Compresion suave: levanta detalles pequenos sin convertir todo en una pared arriba.
            normalizado = Math.Pow(Math.Clamp(normalizado, 0.0, 1.0), 0.62);
            barras[barra] = (float)Math.Clamp(normalizado * 0.96, 0.0, 1.0);
        }

        return barras;
    }

    private static int FrecuenciaABin(double frecuencia, int sampleRate)
    {
        return (int)Math.Round(frecuencia * TamanoFft / sampleRate);
    }

    private static double ObtenerPesoFrecuencia(double frecuencia)
    {
        if (frecuencia < 120.0)
        {
            return 1.18;
        }

        if (frecuencia < 600.0)
        {
            return 1.10;
        }

        if (frecuencia < 2500.0)
        {
            return 1.04;
        }

        return 0.96;
    }
}
