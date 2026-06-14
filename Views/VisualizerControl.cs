using System.Drawing.Drawing2D;
using VisualBeatPlayer.Models;
using VisualBeatPlayer.Utils;

namespace VisualBeatPlayer.Views;

public class VisualizerControl : Control
{
    private const int CantidadParticulas = 90;

    private readonly Particula[] _particulas = new Particula[CantidadParticulas];

    private float[] _barras = new float[96];
    private float[] _barrasSuavizadas = new float[96];
    private float[] _picos = new float[96];

    private float _energia;
    private float _energiaSuavizada;
    private float _graves;
    private float _medios;
    private float _agudos;
    private float _pulso;
    private float _fase;
    private int _fotograma;

    public VisualizerControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);

        BackColor = Color.Transparent;
        Font = new Font("Segoe UI", 12f, FontStyle.Bold);
        InicializarParticulas();
    }

    public ModoVisualizacion Modo { get; set; } = ModoVisualizacion.Espectro;
    public Color ColorFondo { get; set; } = Color.FromArgb(10, 14, 24);
    public Color ColorLinea { get; set; } = Color.FromArgb(42, 55, 84);
    public Color ColorAcento1 { get; set; } = Color.FromArgb(0, 224, 255);
    public Color ColorAcento2 { get; set; } = Color.FromArgb(54, 235, 161);
    public Color ColorAcento3 { get; set; } = Color.FromArgb(177, 96, 255);
    public Color ColorTexto { get; set; } = Color.FromArgb(237, 244, 255);

    public void CambiarModo()
    {
        Modo = Modo switch
        {
            ModoVisualizacion.Espectro => ModoVisualizacion.Ondas,
            ModoVisualizacion.Ondas => ModoVisualizacion.Particulas,
            ModoVisualizacion.Particulas => ModoVisualizacion.Geometria,
            _ => ModoVisualizacion.Espectro
        };

        Invalidate();
    }

    public void ActualizarEspectro(AudioSpectrumData espectro)
    {
        if (espectro.Magnitudes.Length != _barras.Length)
        {
            _barras = new float[espectro.Magnitudes.Length];
            _barrasSuavizadas = new float[espectro.Magnitudes.Length];
            _picos = new float[espectro.Magnitudes.Length];
        }

        for (int i = 0; i < espectro.Magnitudes.Length; i++)
        {
            _barras[i] = (float)Math.Clamp(espectro.Magnitudes[i], 0.0, 1.0);
        }

        if (espectro.PulsoDetectado)
        {
            _pulso = Math.Max(_pulso, 0.70f);
        }

        ActualizarMetricas();
        ActualizarParticulas();

        _fase += 0.014f + _energiaSuavizada * 0.035f;
        _fotograma++;

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 2 || Height <= 2)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? ColorFondo);

        Rectangle area = new(0, 0, Width, Height);
        DibujarReticula(e.Graphics, area);

        if (_barras.All(v => v < 0.006f))
        {
            DibujarEstadoIdle(e.Graphics, area);
            base.OnPaint(e);
            return;
        }

        switch (Modo)
        {
            case ModoVisualizacion.Ondas:
                DibujarOndas(e.Graphics, area);
                break;

            case ModoVisualizacion.Particulas:
                DibujarParticulas(e.Graphics, area);
                break;

            case ModoVisualizacion.Geometria:
                DibujarGeometria(e.Graphics, area);
                break;

            default:
                DibujarEspectro(e.Graphics, area);
                break;
        }

        base.OnPaint(e);
    }

    private void ActualizarMetricas()
    {
        float suma = 0f;
        float graves = PromedioRango(0f, 0.18f);
        float medios = PromedioRango(0.18f, 0.58f);
        float agudos = PromedioRango(0.58f, 1f);

        for (int i = 0; i < _barras.Length; i++)
        {
            float objetivo = Math.Clamp(_barras[i], 0f, 0.95f);

            if (objetivo < 0.015f)
            {
                objetivo = 0f;
            }

            float velocidad = objetivo > _barrasSuavizadas[i] ? 0.68f : 0.14f;
            _barrasSuavizadas[i] += (objetivo - _barrasSuavizadas[i]) * velocidad;

            // Se conserva el arreglo de picos por compatibilidad, pero ya no se dibuja como puntos.
            _picos[i] += (objetivo - _picos[i]) * 0.30f;

            suma += _barrasSuavizadas[i];
        }

        _energia = suma / Math.Max(1, _barrasSuavizadas.Length);
        _energiaSuavizada += (_energia - _energiaSuavizada) * 0.14f;
        _graves += (graves - _graves) * 0.22f;
        _medios += (medios - _medios) * 0.18f;
        _agudos += (agudos - _agudos) * 0.16f;

        float golpe = Math.Max(0f, _graves - _energiaSuavizada * 0.86f);
        _pulso = Math.Max(_pulso * 0.84f, Math.Clamp(golpe * 1.55f, 0f, 1f));
    }

    private float PromedioRango(float inicioNormalizado, float finNormalizado)
    {
        int inicio = Math.Clamp((int)(_barras.Length * inicioNormalizado), 0, _barras.Length - 1);
        int fin = Math.Clamp((int)(_barras.Length * finNormalizado), inicio + 1, _barras.Length);
        float suma = 0f;

        for (int i = inicio; i < fin; i++)
        {
            suma += _barras[i];
        }

        return suma / (fin - inicio);
    }

    private void DibujarReticula(Graphics g, Rectangle area)
    {
        using Pen linea = new(Color.FromArgb(18, ColorLinea), 1f);
        int centroY = area.Height / 2;

        for (int y = centroY % 42; y < area.Height; y += 42)
        {
            g.DrawLine(linea, 18, y, area.Width - 18, y);
        }
    }

    private void DibujarEspectro(Graphics g, Rectangle area)
    {
        int cantidad = _barrasSuavizadas.Length;
        float margenX = 28f;
        float margenY = 26f;
        float espacio = 4f;
        float anchoDisponible = Math.Max(1f, area.Width - margenX * 2);
        float anchoBarra = Math.Max(3f, (anchoDisponible - espacio * (cantidad - 1)) / cantidad);
        float altoMaximo = area.Height - margenY * 2;
        float baseY = area.Bottom - margenY;

        for (int i = 0; i < cantidad; i++)
        {
            float valor = Math.Clamp(_barrasSuavizadas[i], 0f, 1f);

            // Curva natural: evita saturación arriba y evita barras demasiado tiesas.
            valor = MathF.Pow(valor, 1.15f);

            float alto = 5f + valor * altoMaximo * 0.78f;

            if (valor < 0.025f)
            {
                alto = 2f;
            }

            float x = margenX + i * (anchoBarra + espacio);
            RectangleF rect = new(x, baseY - alto, anchoBarra, alto);
            Color color = ColorPorIndice(i, cantidad);

            using GraphicsPath brilloPath = DibujoHelper.CrearRectanguloRedondeado(
                new RectangleF(rect.X - 1.4f, rect.Y - 2.5f, rect.Width + 2.8f, rect.Height + 4.5f),
                anchoBarra);

            using SolidBrush brillo = new(Color.FromArgb(18 + (int)(_pulso * 20), color));
            using GraphicsPath barraPath = DibujoHelper.CrearRectanguloRedondeado(rect, anchoBarra / 2f);
            using LinearGradientBrush gradiente = new(
                rect,
                color,
                DibujoHelper.Mezclar(ColorAcento2, color, 0.32f),
                LinearGradientMode.Vertical);

            g.FillPath(brillo, brilloPath);
            g.FillPath(gradiente, barraPath);

            // IMPORTANTE:
            // Antes aquí se dibujaban picos con FillEllipse.
            // Eso era lo que se veía como puntos/partículas desordenadas encima de las barras.
        }
    }

    private void DibujarOndas(Graphics g, Rectangle area)
    {
        int puntos = Math.Max(96, area.Width / 8);
        float centroY = area.Height * 0.5f;

        for (int capa = 0; capa < 3; capa++)
        {
            using GraphicsPath path = new();
            Color color = capa == 0 ? ColorAcento1 : capa == 1 ? ColorAcento2 : ColorAcento3;
            float amplitud = (28f + capa * 18f) * (0.22f + _energiaSuavizada * 1.35f);
            float frecuencia = 1.6f + capa * 0.58f + _agudos * 1.0f;
            float desplazamiento = _fase * (1.0f + capa * 0.28f);

            for (int i = 0; i < puntos; i++)
            {
                float t = (float)i / (puntos - 1);
                float x = t * area.Width;
                float envolvente = MathF.Sin(MathF.PI * t);
                float banda = _barrasSuavizadas[Math.Min(_barrasSuavizadas.Length - 1, (int)(t * _barrasSuavizadas.Length))];

                float y = centroY
                    + MathF.Sin(t * MathF.Tau * frecuencia + desplazamiento) * amplitud * envolvente
                    + MathF.Cos(t * MathF.Tau * (frecuencia * 0.38f) - desplazamiento) * amplitud * 0.26f * banda;

                if (i == 0)
                {
                    path.StartFigure();
                    path.AddLine(x, y, x, y);
                }
                else
                {
                    path.AddLine(x, y, x, y);
                }
            }

            using Pen pen = new(Color.FromArgb(105 - capa * 22, color), 2.0f + _pulso * 1.8f);
            g.DrawPath(pen, path);
        }
    }

    private void DibujarParticulas(Graphics g, Rectangle area)
    {
        float escala = Math.Min(area.Width, area.Height) * 0.42f;
        float centroX = area.Width * 0.5f;
        float centroY = area.Height * 0.5f;

        using Pen linea = new(Color.FromArgb(16 + (int)(_pulso * 34), ColorAcento1), 1f);

        for (int i = 0; i < _particulas.Length; i++)
        {
            Particula p = _particulas[i];

            float x = centroX + p.X * escala;
            float y = centroY + p.Y * escala;

            float tamano = 2.0f + p.Intensidad * 6.5f + _pulso * 2.8f;
            Color color = ColorPorIndice(i, _particulas.Length);

            if (i % 12 == 0)
            {
                g.DrawLine(linea, centroX, centroY, x, y);
            }

            using SolidBrush brush = new(Color.FromArgb(62 + (int)(p.Intensidad * 145), color));
            g.FillEllipse(brush, x - tamano / 2f, y - tamano / 2f, tamano, tamano);
        }
    }

    private void DibujarGeometria(Graphics g, Rectangle area)
    {
        float centroX = area.Width * 0.5f;
        float centroY = area.Height * 0.5f;
        int capas = 7;

        for (int capa = capas; capa >= 1; capa--)
        {
            int lados = 3 + capa;
            float radioBase = Math.Min(area.Width, area.Height) * (0.07f + capa * 0.047f);
            float radio = radioBase * (1f + _graves * 0.42f + _pulso * 0.18f);
            float rotacion = _fase * (0.38f + capa * 0.07f);
            PointF[] puntos = new PointF[lados];

            for (int i = 0; i < lados; i++)
            {
                float angulo = rotacion + MathF.Tau * i / lados;
                float deformacion = 1f + MathF.Sin(_fase * 1.8f + capa + i * 1.35f) * _medios * 0.18f;

                float x = centroX + MathF.Cos(angulo) * radio * deformacion;
                float y = centroY + MathF.Sin(angulo) * radio * deformacion;

                puntos[i] = new PointF(x, y);
            }

            Color color = ColorPorIndice(capa, capas);
            using Pen pen = new(Color.FromArgb(50 + capa * 16, color), 1.3f + _agudos * 2.2f);
            g.DrawPolygon(pen, puntos);
        }

        float nucleo = 18f + _energiaSuavizada * 60f + _pulso * 18f;
        using SolidBrush brush = new(Color.FromArgb(105, ColorAcento2));
        g.FillEllipse(brush, centroX - nucleo / 2f, centroY - nucleo / 2f, nucleo, nucleo);
    }

    private void DibujarEstadoIdle(Graphics g, Rectangle area)
    {
        float centroX = area.Width * 0.5f;
        float centroY = area.Height * 0.52f;
        float respiracion = 0.5f + MathF.Sin(_fotograma * 0.045f) * 0.5f;

        for (int i = 0; i < 4; i++)
        {
            float radio = 54f + i * 36f + respiracion * 14f;
            using Pen pen = new(Color.FromArgb(44 - i * 6, ColorPorIndice(i, 4)), 1.8f);
            g.DrawEllipse(pen, centroX - radio, centroY - radio, radio * 2f, radio * 2f);
        }

        string titulo = "VisualBeat";
        string subtitulo = "Carga audio y elige un modo visual";

        using Font tituloFont = new("Segoe UI", 24f, FontStyle.Bold);
        using Font subtituloFont = new("Segoe UI", 10f, FontStyle.Regular);

        Size tituloSize = TextRenderer.MeasureText(titulo, tituloFont);
        Size subtituloSize = TextRenderer.MeasureText(subtitulo, subtituloFont);

        using SolidBrush tituloBrush = new(ColorTexto);
        using SolidBrush subtituloBrush = new(Color.FromArgb(135, ColorTexto));

        g.DrawString(titulo, tituloFont, tituloBrush, centroX - tituloSize.Width / 2f, area.Height * 0.23f);
        g.DrawString(subtitulo, subtituloFont, subtituloBrush, centroX - subtituloSize.Width / 2f, area.Height * 0.23f + 42f);
    }

    private void InicializarParticulas()
    {
        Random rng = new(42);

        for (int i = 0; i < _particulas.Length; i++)
        {
            float t = (float)i / _particulas.Length;
            float angulo = t * MathF.Tau * 4.0f;

            float radioBase = 0.22f + 0.70f * t;
            float variacion = 0.92f + (float)rng.NextDouble() * 0.16f;

            _particulas[i] = new Particula
            {
                Angulo = angulo,
                Radio = radioBase,
                RadioBase = radioBase,
                RadioA = radioBase,
                RadioB = radioBase * variacion,
                Excentricidad = variacion,
                Velocidad = 0.0012f + (i % 7) * 0.00025f,
                Fase = t * MathF.Tau,
                Intensidad = 0f,
                X = MathF.Cos(angulo) * radioBase,
                Y = MathF.Sin(angulo) * radioBase * variacion
            };
        }
    }

    private void ActualizarParticulas()
    {
        int n = _particulas.Length;

        for (int i = 0; i < n; i++)
        {
            int indiceBanda = i * _barrasSuavizadas.Length / n;
            float banda = MathF.Pow(_barrasSuavizadas[indiceBanda], 0.70f);

            Particula p = _particulas[i];

            float velocidadActual = p.Velocidad * (0.55f + _energiaSuavizada * 1.75f + _graves * 0.80f);
            p.Angulo += velocidadActual;

            float expansion = _pulso * 0.16f
                + banda * 0.10f
                + MathF.Sin(_fase * 1.1f + p.Fase) * _medios * 0.05f;

            float objetivoA = Math.Clamp(p.RadioBase + expansion, 0f, 1f);
            float objetivoB = Math.Clamp(p.RadioBase * p.Excentricidad + expansion * 0.86f, 0f, 1f);

            p.RadioA += (objetivoA - p.RadioA) * (objetivoA > p.RadioA ? 0.26f : 0.10f);
            p.RadioB += (objetivoB - p.RadioB) * (objetivoB > p.RadioB ? 0.26f : 0.10f);

            p.X = MathF.Cos(p.Angulo) * p.RadioA;
            p.Y = MathF.Sin(p.Angulo) * p.RadioB;

            p.Intensidad += (banda - p.Intensidad) * 0.24f;

            _particulas[i] = p;
        }
    }

    private Color ColorPorIndice(int indice, int total)
    {
        float t = (float)indice / Math.Max(1, total - 1);

        return t < 0.5f
            ? DibujoHelper.Mezclar(ColorAcento1, ColorAcento2, t * 2f)
            : DibujoHelper.Mezclar(ColorAcento2, ColorAcento3, (t - 0.5f) * 2f);
    }

    public void AplicarTema(PaletaTema paleta)
    {
        ColorFondo = paleta.Fondo;
        ColorLinea = paleta.Borde;
        ColorAcento1 = paleta.AcentoPrincipal;
        ColorAcento2 = paleta.AcentoSecundario;
        ColorAcento3 = paleta.AcentoTerciario;
        ColorTexto = paleta.Texto;
        Invalidate();
    }

    private struct Particula
    {
        public float X;
        public float Y;
        public float Angulo;
        public float Radio;
        public float RadioBase;
        public float RadioA;
        public float RadioB;
        public float Velocidad;
        public float Excentricidad;
        public float Intensidad;
        public float Fase;
    }
}
