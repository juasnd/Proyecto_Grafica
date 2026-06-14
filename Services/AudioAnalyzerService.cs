using VisualBeatPlayer.Models;

namespace VisualBeatPlayer.Services;

public class AudioAnalyzerService
{
    private const int TamanoFft = 4096;
    private const double FrecuenciaMinimaVisual = 40.0;

    // 20 kHz hacía que las últimas barras se vieran tiesas porque muchas canciones casi no tienen energía útil ahí.
    private const double FrecuenciaMaximaVisual = 14000.0;

    private const double PisoDecibeles = -80.0;
    private const double TechoDecibeles = -18.0;
    private const double DuracionHistorialEnergiaSegundos = 0.5;
    private const double UmbralPulso = 1.45;
    private const double EnergiaMinimaPulso = 0.0005;

    private readonly object _candado = new();
    private readonly object _candadoEnergia = new();
    private readonly float[] _bufferCircular = new float[16384];
    private readonly Queue<MuestraEnergia> _historialEnergia = new();

    private int _sampleRate = 44100;
    private int _posicionEscritura;
    private bool _bufferLleno;
    private double _sumaHistorialEnergia;

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
        EstadisticasTiempo estadisticasTiempo = CalcularEstadisticasTiempo(muestras);
        (bool pulsoDetectado, double promedioMovil) = DetectarPulso(estadisticasTiempo.EnergiaTotal);

        if (estadisticasTiempo.Rms < 0.00008)
        {
            return new AudioSpectrumData(
                new double[cantidadBarras],
                new double[cantidadBarras],
                estadisticasTiempo.EnergiaTotal,
                estadisticasTiempo.Rms,
                0.0,
                0.0,
                0.0,
                false,
                promedioMovil);
        }

        MuestraCompleja[] fft = CrearEntradaFftConVentanaHann(muestras);
        TransformadaRapidaFourier(fft);

        int sampleRate = ObtenerSampleRate();
        (double[] frecuencias, double[] magnitudes) = ConvertirFftEnBarras(fft, cantidadBarras, sampleRate);

        return new AudioSpectrumData(
            frecuencias,
            magnitudes,
            estadisticasTiempo.EnergiaTotal,
            estadisticasTiempo.Rms,
            CalcularEnergiaEspectral(fft, sampleRate, 0.0, 200.0),
            CalcularEnergiaEspectral(fft, sampleRate, 200.0, 2000.0),
            CalcularEnergiaEspectral(fft, sampleRate, 2000.0, 14000.0),
            pulsoDetectado,
            promedioMovil);
    }

    public void Limpiar()
    {
        lock (_candado)
        {
            Array.Clear(_bufferCircular);
            _posicionEscritura = 0;
            _bufferLleno = false;
        }

        lock (_candadoEnergia)
        {
            _historialEnergia.Clear();
            _sumaHistorialEnergia = 0.0;
        }
    }

    private int ObtenerSampleRate()
    {
        lock (_candado)
        {
            return _sampleRate;
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

    private static EstadisticasTiempo CalcularEstadisticasTiempo(float[] muestras)
    {
        double energia = 0.0;

        for (int i = 0; i < muestras.Length; i++)
        {
            double muestra = muestras[i];
            energia += muestra * muestra;
        }

        double rms = Math.Sqrt(energia / Math.Max(1, muestras.Length));
        return new EstadisticasTiempo(energia, rms);
    }

    private (bool PulsoDetectado, double PromedioMovil) DetectarPulso(double energiaActual)
    {
        DateTime ahora = DateTime.UtcNow;

        lock (_candadoEnergia)
        {
            while (_historialEnergia.Count > 0 &&
                   (ahora - _historialEnergia.Peek().Instante).TotalSeconds > DuracionHistorialEnergiaSegundos)
            {
                _sumaHistorialEnergia -= _historialEnergia.Dequeue().Energia;
            }

            double promedioPrevio = _historialEnergia.Count == 0
                ? 0.0
                : _sumaHistorialEnergia / _historialEnergia.Count;

            bool pulsoDetectado = promedioPrevio > EnergiaMinimaPulso &&
                                  energiaActual > promedioPrevio * UmbralPulso;

            _historialEnergia.Enqueue(new MuestraEnergia(ahora, energiaActual));
            _sumaHistorialEnergia += energiaActual;

            double promedioActual = _historialEnergia.Count == 0
                ? 0.0
                : _sumaHistorialEnergia / _historialEnergia.Count;

            return (pulsoDetectado, promedioActual);
        }
    }

    private static MuestraCompleja[] CrearEntradaFftConVentanaHann(float[] muestras)
    {
        MuestraCompleja[] fft = new MuestraCompleja[TamanoFft];

        for (int i = 0; i < TamanoFft; i++)
        {
            double ventanaHann = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / (TamanoFft - 1));
            fft[i] = new MuestraCompleja(muestras[i] * ventanaHann, 0.0);
        }

        return fft;
    }

    private static void TransformadaRapidaFourier(MuestraCompleja[] datos)
    {
        int n = datos.Length;

        if (!EsPotenciaDeDos(n))
        {
            throw new InvalidOperationException("La FFT manual requiere un tamano potencia de dos.");
        }

        int indiceInvertido = 0;

        for (int i = 1; i < n; i++)
        {
            int bit = n >> 1;

            while ((indiceInvertido & bit) != 0)
            {
                indiceInvertido ^= bit;
                bit >>= 1;
            }

            indiceInvertido ^= bit;

            if (i < indiceInvertido)
            {
                (datos[i], datos[indiceInvertido]) = (datos[indiceInvertido], datos[i]);
            }
        }

        for (int longitud = 2; longitud <= n; longitud <<= 1)
        {
            double angulo = -2.0 * Math.PI / longitud;
            double raizReal = Math.Cos(angulo);
            double raizImaginaria = Math.Sin(angulo);
            int mitad = longitud >> 1;

            for (int inicio = 0; inicio < n; inicio += longitud)
            {
                double giroReal = 1.0;
                double giroImaginario = 0.0;

                for (int j = 0; j < mitad; j++)
                {
                    int indicePar = inicio + j;
                    int indiceImpar = indicePar + mitad;

                    MuestraCompleja par = datos[indicePar];
                    MuestraCompleja impar = datos[indiceImpar];
                    MuestraCompleja rotado = Multiplicar(impar, giroReal, giroImaginario);

                    datos[indicePar] = new MuestraCompleja(
                        par.Real + rotado.Real,
                        par.Imaginaria + rotado.Imaginaria);

                    datos[indiceImpar] = new MuestraCompleja(
                        par.Real - rotado.Real,
                        par.Imaginaria - rotado.Imaginaria);

                    double siguienteReal = giroReal * raizReal - giroImaginario * raizImaginaria;
                    double siguienteImaginario = giroReal * raizImaginaria + giroImaginario * raizReal;

                    giroReal = siguienteReal;
                    giroImaginario = siguienteImaginario;
                }
            }
        }
    }

    private static (double[] Frecuencias, double[] Magnitudes) ConvertirFftEnBarras(
        MuestraCompleja[] fft,
        int cantidadBarras,
        int sampleRate)
    {
        double[] frecuencias = new double[cantidadBarras];
        double[] magnitudes = new double[cantidadBarras];

        double nyquist = sampleRate / 2.0;
        double frecuenciaMaxima = Math.Min(FrecuenciaMaximaVisual, nyquist * 0.92);

        double logMin = Math.Log10(FrecuenciaMinimaVisual);
        double logMax = Math.Log10(Math.Max(FrecuenciaMinimaVisual + 1.0, frecuenciaMaxima));

        for (int barra = 0; barra < cantidadBarras; barra++)
        {
            double tInicio = (double)barra / cantidadBarras;
            double tFin = (double)(barra + 1) / cantidadBarras;

            double frecuenciaInicio = Math.Pow(10.0, logMin + (logMax - logMin) * tInicio);
            double frecuenciaFin = Math.Pow(10.0, logMin + (logMax - logMin) * tFin);
            double frecuenciaCentral = Math.Sqrt(frecuenciaInicio * frecuenciaFin);

            int binInicio = Math.Max(1, FrecuenciaABin(frecuenciaInicio, sampleRate));
            int binFin = Math.Max(binInicio + 1, FrecuenciaABin(frecuenciaFin, sampleRate));
            binFin = Math.Min(binFin, fft.Length / 2 - 1);

            double sumaCuadrados = 0.0;
            double maximo = 0.0;
            int cantidad = 0;

            for (int bin = binInicio; bin <= binFin; bin++)
            {
                double amplitud = ObtenerAmplitudBin(fft[bin]);
                sumaCuadrados += amplitud * amplitud;
                maximo = Math.Max(maximo, amplitud);
                cantidad++;
            }

            double rmsBanda = cantidad == 0 ? 0.0 : Math.Sqrt(sumaCuadrados / cantidad);
            double magnitud = rmsBanda * 0.72 + maximo * 0.28;
            magnitud *= ObtenerPesoFrecuencia(frecuenciaCentral);

            double decibeles = 20.0 * Math.Log10(magnitud + 0.000000001);
            double normalizado = (decibeles - PisoDecibeles) / (TechoDecibeles - PisoDecibeles);

            frecuencias[barra] = frecuenciaCentral;
            magnitudes[barra] = Math.Clamp(
                Math.Pow(Math.Clamp(normalizado, 0.0, 1.0), 0.70),
                0.0,
                1.0);
        }

        return (frecuencias, magnitudes);
    }

    private static double CalcularEnergiaEspectral(
        MuestraCompleja[] fft,
        int sampleRate,
        double frecuenciaInicio,
        double frecuenciaFin)
    {
        double nyquist = sampleRate / 2.0;
        double inicioSeguro = Math.Clamp(frecuenciaInicio, 0.0, nyquist);
        double finSeguro = Math.Clamp(frecuenciaFin, inicioSeguro, nyquist);

        int binInicio = Math.Max(1, FrecuenciaABin(inicioSeguro, sampleRate));
        int binFin = Math.Max(binInicio, FrecuenciaABin(finSeguro, sampleRate));
        binFin = Math.Min(binFin, fft.Length / 2 - 1);

        double energia = 0.0;

        for (int bin = binInicio; bin <= binFin; bin++)
        {
            double amplitud = ObtenerAmplitudBin(fft[bin]);
            energia += amplitud * amplitud;
        }

        return energia;
    }

    private static int FrecuenciaABin(double frecuencia, int sampleRate)
    {
        return (int)Math.Round(frecuencia * TamanoFft / sampleRate);
    }

    private static double ObtenerAmplitudBin(MuestraCompleja muestra)
    {
        double magnitud = Math.Sqrt(muestra.Real * muestra.Real + muestra.Imaginaria * muestra.Imaginaria);
        return (4.0 * magnitud) / TamanoFft;
    }

    private static double ObtenerPesoFrecuencia(double frecuencia)
    {
        if (frecuencia < 120.0)
        {
            return 1.28;
        }

        if (frecuencia < 600.0)
        {
            return 1.14;
        }

        if (frecuencia < 2500.0)
        {
            return 1.04;
        }

        if (frecuencia < 9000.0)
        {
            return 0.96;
        }

        return 0.88;
    }

    private static MuestraCompleja Multiplicar(MuestraCompleja muestra, double real, double imaginaria)
    {
        return new MuestraCompleja(
            muestra.Real * real - muestra.Imaginaria * imaginaria,
            muestra.Real * imaginaria + muestra.Imaginaria * real);
    }

    private static bool EsPotenciaDeDos(int valor)
    {
        return valor > 0 && (valor & (valor - 1)) == 0;
    }

    private readonly struct EstadisticasTiempo
    {
        public EstadisticasTiempo(double energiaTotal, double rms)
        {
            EnergiaTotal = energiaTotal;
            Rms = rms;
        }

        public double EnergiaTotal { get; }
        public double Rms { get; }
    }

    private readonly struct MuestraCompleja
    {
        public MuestraCompleja(double real, double imaginaria)
        {
            Real = real;
            Imaginaria = imaginaria;
        }

        public double Real { get; }
        public double Imaginaria { get; }
    }

    private readonly struct MuestraEnergia
    {
        public MuestraEnergia(DateTime instante, double energia)
        {
            Instante = instante;
            Energia = energia;
        }

        public DateTime Instante { get; }
        public double Energia { get; }
    }
}
