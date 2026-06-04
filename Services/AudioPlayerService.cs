using NAudio.Wave;
using VisualBeatPlayer.Models;

namespace VisualBeatPlayer.Services;

public class AudioPlayerService : IDisposable
{
    private readonly AudioAnalyzerService _analizador;
    private WaveOutEvent? _salidaAudio;
    private AudioFileReader? _lectorAudio;
    private SampleCapturadorProvider? _capturador;
    private bool _detencionSolicitada;

    public AudioPlayerService(AudioAnalyzerService analizador)
    {
        _analizador = analizador;
    }

    public event EventHandler? ReproduccionFinalizada;

    public float Volumen { get; private set; } = 0.70f;
    public bool EstaCargado => _lectorAudio is not null;
    public bool EstaReproduciendo => _salidaAudio?.PlaybackState == PlaybackState.Playing;
    public TimeSpan PosicionActual => _lectorAudio?.CurrentTime ?? TimeSpan.Zero;
    public TimeSpan DuracionActual => _lectorAudio?.TotalTime ?? TimeSpan.Zero;

    public void Cargar(Cancion cancion)
    {
        LiberarRecursosActuales();

        _lectorAudio = new AudioFileReader(cancion.Ruta)
        {
            Volume = Volumen
        };
        _analizador.ConfigurarFormato(_lectorAudio.WaveFormat.SampleRate);

        // El capturador copia las muestras que NAudio reproduce y las envia al analizador FFT.
        _capturador = new SampleCapturadorProvider(_lectorAudio, _analizador.AgregarMuestras);
        _salidaAudio = new WaveOutEvent
        {
            DesiredLatency = 100
        };

        _salidaAudio.PlaybackStopped += AlDetenerReproduccion;
        _salidaAudio.Init(_capturador);
        _analizador.Limpiar();
        _detencionSolicitada = false;
    }

    public void Reproducir()
    {
        if (_salidaAudio is null)
        {
            throw new InvalidOperationException("No hay una cancion cargada.");
        }

        _detencionSolicitada = false;
        _salidaAudio.Play();
    }

    public void Pausar()
    {
        _salidaAudio?.Pause();
    }

    public void Detener()
    {
        if (_salidaAudio is null)
        {
            return;
        }

        _detencionSolicitada = true;
        _salidaAudio.Stop();

        if (_lectorAudio is not null)
        {
            _lectorAudio.CurrentTime = TimeSpan.Zero;
        }

        _analizador.Limpiar();
    }

    public void CambiarVolumen(float volumen)
    {
        Volumen = Math.Clamp(volumen, 0f, 1f);

        if (_lectorAudio is not null)
        {
            _lectorAudio.Volume = Volumen;
        }
    }

    public void CambiarPosicion(TimeSpan posicion)
    {
        if (_lectorAudio is null)
        {
            return;
        }

        TimeSpan posicionSegura = posicion < TimeSpan.Zero
            ? TimeSpan.Zero
            : posicion > _lectorAudio.TotalTime
                ? _lectorAudio.TotalTime
                : posicion;

        _lectorAudio.CurrentTime = posicionSegura;
        _analizador.Limpiar();
    }

    public void Dispose()
    {
        LiberarRecursosActuales();
        GC.SuppressFinalize(this);
    }

    private void AlDetenerReproduccion(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null || _lectorAudio is null || _detencionSolicitada)
        {
            return;
        }

        long margenFinal = _lectorAudio.WaveFormat.AverageBytesPerSecond / 3;
        bool llegoAlFinal = _lectorAudio.Position >= _lectorAudio.Length - margenFinal;

        if (llegoAlFinal)
        {
            ReproduccionFinalizada?.Invoke(this, EventArgs.Empty);
        }
    }

    private void LiberarRecursosActuales()
    {
        if (_salidaAudio is not null)
        {
            _salidaAudio.PlaybackStopped -= AlDetenerReproduccion;
            _salidaAudio.Dispose();
            _salidaAudio = null;
        }

        _lectorAudio?.Dispose();
        _lectorAudio = null;
        _capturador = null;
        _analizador.Limpiar();
    }

    private sealed class SampleCapturadorProvider : ISampleProvider
    {
        private readonly ISampleProvider _origen;
        private readonly Action<float[]> _alCapturar;

        public SampleCapturadorProvider(ISampleProvider origen, Action<float[]> alCapturar)
        {
            _origen = origen;
            _alCapturar = alCapturar;
        }

        public WaveFormat WaveFormat => _origen.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int muestrasLeidas = _origen.Read(buffer, offset, count);

            if (muestrasLeidas > 0)
            {
                _alCapturar(ConvertirAMono(buffer, offset, muestrasLeidas));
            }

            return muestrasLeidas;
        }

        private float[] ConvertirAMono(float[] buffer, int offset, int muestrasLeidas)
        {
            int canales = Math.Max(1, WaveFormat.Channels);
            int cuadros = muestrasLeidas / canales;
            float[] mono = new float[cuadros];

            for (int cuadro = 0; cuadro < cuadros; cuadro++)
            {
                float suma = 0f;
                int inicio = offset + cuadro * canales;

                for (int canal = 0; canal < canales; canal++)
                {
                    suma += buffer[inicio + canal];
                }

                mono[cuadro] = suma / canales;
            }

            return mono;
        }
    }
}
