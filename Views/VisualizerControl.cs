using System.Drawing.Drawing2D;
using VisualBeatPlayer.Models;
using VisualBeatPlayer.Utils;

namespace VisualBeatPlayer.Views;

public class VisualizerControl : Control
{
    private const int CantidadParticulas = 110;
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
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
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
        if (espectro.Frecuencias.Length != _barras.Length)
        {
            _barras = new float[espectro.Frecuencias.Length];
            _barrasSuavizadas = new float[espectro.Frecuencias.Length];
            _picos = new float[espectro.Frecuencias.Length];
        }

        Array.Copy(espectro.Frecuencias, _barras, espectro.Frecuencias.Length);
        ActualizarMetricas();
        ActualizarParticulas();
        _fase += 0.018f + _energiaSuavizada * 0.055f;
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
            float objetivo = Math.Clamp(_barras[i], 0f, 1f);
            float velocidad = objetivo > _barrasSuavizadas[i] ? 0.58f : 0.18f;
            _barrasSuavizadas[i] += (objetivo - _barrasSuavizadas[i]) * velocidad;
            _picos[i] = objetivo > _picos[i] ? objetivo : Math.Max(0f, _picos[i] - 0.01f);
            suma += _barrasSuavizadas[i];
        }

        _energia = suma / Math.Max(1, _barrasSuavizadas.Length);
        _energiaSuavizada += (_energia - _energiaSuavizada) * 0.16f;
        _graves += (graves - _graves) * 0.24f;
        _medios += (medios - _medios) * 0.20f;
        _agudos += (agudos - _agudos) * 0.18f;

        float golpe = Math.Max(0f, _graves - _energiaSuavizada * 0.82f);
        _pulso = Math.Max(_pulso * 0.88f, Math.Clamp(golpe * 1.8f, 0f, 1f));
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
        using Pen linea = new(Color.FromArgb(24, ColorLinea), 1f);
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
            float valor = MathF.Pow(_barrasSuavizadas[i], 0.78f);
            float alto = 5f + valor * altoMaximo * 0.9f;
            float x = margenX + i * (anchoBarra + espacio);
            RectangleF rect = new(x, baseY - alto, anchoBarra, alto);
            Color color = ColorPorIndice(i, cantidad);

            using GraphicsPath brilloPath = DibujoHelper.CrearRectanguloRedondeado(new RectangleF(rect.X - 2f, rect.Y - 5f, rect.Width + 4f, rect.Height + 8f), anchoBarra);
            using SolidBrush brillo = new(Color.FromArgb(28 + (int)(_pulso * 32), color));
            using GraphicsPath barraPath = DibujoHelper.CrearRectanguloRedondeado(rect, anchoBarra / 2f);
            using LinearGradientBrush gradiente = new(rect, color, DibujoHelper.Mezclar(ColorAcento2, color, 0.35f), LinearGradientMode.Vertical);
            g.FillPath(brillo, brilloPath);
            g.FillPath(gradiente, barraPath);

            float yPico = baseY - (6f + MathF.Pow(_picos[i], 0.78f) * altoMaximo * 0.9f) - 5f;
            using SolidBrush pico = new(Color.FromArgb(160, color));
            g.FillEllipse(pico, x + anchoBarra / 2f - 2f, yPico, 4f, 4f);
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
            float amplitud = (34f + capa * 22f) * (0.25f + _energiaSuavizada * 1.8f);
            float frecuencia = 1.8f + capa * 0.72f + _agudos * 1.4f;
            float desplazamiento = _fase * (1.1f + capa * 0.35f);

            for (int i = 0; i < puntos; i++)
            {
                float t = (float)i / (puntos - 1);
                float x = t * area.Width;
                float envolvente = MathF.Sin(MathF.PI * t);
                float banda = _barrasSuavizadas[Math.Min(_barrasSuavizadas.Length - 1, (int)(t * _barrasSuavizadas.Length))];
                float y = centroY
                    + MathF.Sin(t * MathF.Tau * frecuencia + desplazamiento) * amplitud * envolvente
                    + MathF.Cos(t * MathF.Tau * (frecuencia * 0.42f) - desplazamiento) * amplitud * 0.34f * banda;

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

            using Pen pen = new(Color.FromArgb(110 - capa * 20, color), 2.2f + _pulso * 2.5f);
            g.DrawPath(pen, path);
        }
    }

    private void DibujarParticulas(Graphics g, Rectangle area)
    {
        float escalaX = area.Width * 0.44f;   // semiejes de pantalla independientes
        float escalaY = area.Height * 0.44f;
        float centroX = area.Width * 0.5f;
        float centroY = area.Height * 0.5f;

        using Pen linea = new(Color.FromArgb(28 + (int)(_pulso * 72), ColorAcento1), 1f);

        for (int i = 0; i < _particulas.Length; i++)
        {
            Particula p = _particulas[i];

            // X e Y ya vienen calculados con la elipse desde ActualizarParticulas
            float x = centroX + p.X * escalaX;
            float y = centroY + p.Y * escalaY;

            float tamano = 2.2f + p.Intensidad * 9f + _pulso * 4.5f;
            Color color = ColorPorIndice(i, _particulas.Length);

            if (i % 7 == 0)
                g.DrawLine(linea, centroX, centroY, x, y);

            using SolidBrush brush = new(Color.FromArgb(70 + (int)(p.Intensidad * 160), color));
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
            float radio = radioBase * (1f + _graves * 0.52f + _pulso * 0.22f);
            float rotacion = _fase * (0.45f + capa * 0.08f);
            PointF[] puntos = new PointF[lados];

            for (int i = 0; i < lados; i++)
            {
                float angulo = rotacion + MathF.Tau * i / lados;
                float indice = (float)i / lados;
                float deformacion = 1f + MathF.Sin(_fase * 2.1f + capa + i * 1.7f) * _medios * 0.24f;
                float x = centroX + MathF.Cos(angulo) * radio * deformacion;
                float y = centroY + MathF.Sin(angulo) * radio * deformacion;
                puntos[i] = new PointF(x, y);
            }

            Color color = ColorPorIndice(capa, capas);
            using Pen pen = new(Color.FromArgb(52 + capa * 18, color), 1.4f + _agudos * 3f);
            g.DrawPolygon(pen, puntos);
        }

        float nucleo = 22f + _energiaSuavizada * 80f + _pulso * 24f;
        using SolidBrush brush = new(Color.FromArgb(120, ColorAcento2));
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
            float angulo = t * MathF.Tau * 8.0f;

            float radioBase = (i % 3) switch
            {
                0 => 0.28f,
                1 => 0.54f,
                _ => 0.80f
            };

            float exc = 0.55f + (float)rng.NextDouble() * 0.45f;

            _particulas[i] = new Particula
            {
                Angulo = angulo,
                Radio = radioBase,
                RadioBase = radioBase,
                RadioA = radioBase,
                RadioB = radioBase * exc,
                Excentricidad = exc,          // ← guardado aquí
                Velocidad = 0.0015f + (i % 9) * 0.00035f,
                Fase = t * MathF.Tau,
                X = MathF.Cos(angulo) * radioBase,
                Y = MathF.Sin(angulo) * radioBase * exc
            };
        }
    }

    private void ActualizarParticulas()
    {
        int n = _particulas.Length;
        for (int i = 0; i < n; i++)
        {
            int indiceBanda = i * _barrasSuavizadas.Length / n;
            float banda = MathF.Pow(_barrasSuavizadas[indiceBanda], 0.55f);

            Particula p = _particulas[i];

            
            float velocidadActual = p.Velocidad * (0.5f + _energiaSuavizada * 2.8f + _graves * 1.4f);
            p.Angulo += velocidadActual;

            float expansionA = _pulso * 0.38f                              
                             + MathF.Sin(_fase * 1.3f + p.Fase) * _medios * 0.16f  
                             + banda * 0.14f;                              

            float expansionB = _pulso * 0.46f                              
                             + MathF.Cos(_fase * 1.1f + p.Fase) * _graves * 0.20f  
                             + banda * 0.10f;                              
            float objetivoA = Math.Clamp(p.RadioBase + expansionA, 0f, 1f);
            float objetivoB = Math.Clamp(p.RadioBase * p.Excentricidad + expansionB, 0f, 1f);

            // Expansión más agresiva, contracción más notoria
            p.RadioA += (objetivoA - p.RadioA) * (objetivoA > p.RadioA ? 0.55f : 0.12f);
            p.RadioB += (objetivoB - p.RadioB) * (objetivoB > p.RadioB ? 0.55f : 0.12f);



            p.X = MathF.Cos(p.Angulo) * p.RadioA;
            p.Y = MathF.Sin(p.Angulo) * p.RadioB;

            p.Intensidad += (banda - p.Intensidad) * 0.45f;
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
        public float Radio;      // mantener por compatibilidad
        public float RadioBase;
        public float RadioA;     // semieje horizontal de la elipse
        public float RadioB;     // semieje vertical de la elipse
        public float Velocidad;
        public float Excentricidad;
        public float Intensidad;
        public float Fase;       // desfase individual para que no puls en todas igual
    }
}
