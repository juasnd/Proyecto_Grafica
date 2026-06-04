using System.Drawing.Drawing2D;
using VisualBeatPlayer.Models;
using VisualBeatPlayer.Utils;

namespace VisualBeatPlayer.Views;

public class VisualizerControl : Control
{
    private float[] _barras = new float[72];
    private float[] _barrasSuavizadas = new float[72];
    private float[] _picos = new float[72];
    private int _pulsoIdle;

    public VisualizerControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = ColorFondo;
        Font = new Font("Segoe UI", 12f, FontStyle.Bold);
    }

    public Color ColorFondo { get; set; } = Color.FromArgb(10, 14, 24);
    public Color ColorLinea { get; set; } = Color.FromArgb(42, 55, 84);
    public Color ColorAcento1 { get; set; } = Color.FromArgb(0, 224, 255);
    public Color ColorAcento2 { get; set; } = Color.FromArgb(54, 235, 161);
    public Color ColorAcento3 { get; set; } = Color.FromArgb(177, 96, 255);
    public Color ColorTexto { get; set; } = Color.FromArgb(237, 244, 255);

    public void ActualizarEspectro(AudioSpectrumData espectro)
    {
        if (espectro.Frecuencias.Length != _barras.Length)
        {
            _barras = new float[espectro.Frecuencias.Length];
            _barrasSuavizadas = new float[espectro.Frecuencias.Length];
            _picos = new float[espectro.Frecuencias.Length];
        }

        Array.Copy(espectro.Frecuencias, _barras, espectro.Frecuencias.Length);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 2 || Height <= 2)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(ColorFondo);

        Rectangle area = new(0, 0, Width, Height);
        DibujarFondo(e.Graphics, area);
        DibujarBarras(e.Graphics, area);

        base.OnPaint(e);
    }

    private void DibujarFondo(Graphics g, Rectangle area)
    {
        using LinearGradientBrush fondo = new(area, ColorFondo, DibujoHelper.Mezclar(ColorFondo, ColorAcento3, 0.12f), LinearGradientMode.ForwardDiagonal);
        g.FillRectangle(fondo, area);

        using Pen linea = new(Color.FromArgb(42, ColorLinea), 1f);
        for (int y = 42; y < area.Height; y += 48)
        {
            g.DrawLine(linea, 24, y, area.Width - 24, y);
        }
    }

    private void DibujarBarras(Graphics g, Rectangle area)
    {
        if (_barras.Length == 0)
        {
            return;
        }

        bool sinAudio = _barras.All(v => v < 0.006f);
        if (sinAudio)
        {
            DibujarEstadoVacio(g, area);
            return;
        }

        int cantidad = _barras.Length;
        float margenX = 26f;
        float margenY = 24f;
        float espacio = 5f;
        float anchoDisponible = Math.Max(1f, area.Width - margenX * 2);
        float anchoBarra = Math.Max(4f, (anchoDisponible - espacio * (cantidad - 1)) / cantidad);
        float altoMaximo = area.Height - margenY * 2;
        float baseY = area.Bottom - margenY;

        for (int i = 0; i < cantidad; i++)
        {
            float objetivo = Math.Clamp(_barras[i], 0f, 1f);
            float velocidad = objetivo > _barrasSuavizadas[i] ? 0.64f : 0.22f;
            _barrasSuavizadas[i] += (objetivo - _barrasSuavizadas[i]) * velocidad;

            if (objetivo > _picos[i])
            {
                _picos[i] = objetivo;
            }
            else
            {
                _picos[i] = Math.Max(0f, _picos[i] - 0.012f);
            }

            float alto = 6f + MathF.Pow(_barrasSuavizadas[i], 0.82f) * altoMaximo * 0.88f;
            float x = margenX + i * (anchoBarra + espacio);
            RectangleF rect = new(x, baseY - alto, anchoBarra, alto);

            DibujarBrilloBarra(g, rect, anchoBarra, i, cantidad);
            DibujarBarra(g, rect, anchoBarra, i, cantidad);
            DibujarPico(g, x, anchoBarra, baseY, altoMaximo, i, cantidad);
        }
    }

    private void DibujarBrilloBarra(Graphics g, RectangleF rect, float anchoBarra, int indice, int cantidad)
    {
        Color color = DibujoHelper.Mezclar(ColorAcento1, ColorAcento3, (float)indice / Math.Max(1, cantidad - 1));
        RectangleF brillo = new(rect.X - 2f, rect.Y - 5f, rect.Width + 4f, rect.Height + 8f);

        using GraphicsPath brilloPath = DibujoHelper.CrearRectanguloRedondeado(brillo, anchoBarra);
        using SolidBrush brilloBrush = new(Color.FromArgb(34, color));
        g.FillPath(brilloBrush, brilloPath);
    }

    private void DibujarBarra(Graphics g, RectangleF rect, float anchoBarra, int indice, int cantidad)
    {
        float posicion = (float)indice / Math.Max(1, cantidad - 1);
        Color colorSuperior = DibujoHelper.Mezclar(ColorAcento1, ColorAcento3, posicion);
        Color colorInferior = DibujoHelper.Mezclar(ColorAcento2, ColorAcento1, 0.35f);

        using GraphicsPath barraPath = DibujoHelper.CrearRectanguloRedondeado(rect, anchoBarra / 2f);
        using LinearGradientBrush gradiente = new(rect, colorSuperior, colorInferior, LinearGradientMode.Vertical);
        g.FillPath(gradiente, barraPath);
    }

    private void DibujarPico(Graphics g, float x, float anchoBarra, float baseY, float altoMaximo, int indice, int cantidad)
    {
        float altoPico = 6f + MathF.Pow(_picos[indice], 0.82f) * altoMaximo * 0.88f;
        float y = baseY - altoPico - 5f;
        Color color = DibujoHelper.Mezclar(ColorAcento1, ColorAcento3, (float)indice / Math.Max(1, cantidad - 1));

        using SolidBrush brush = new(Color.FromArgb(185, color));
        g.FillEllipse(brush, x + anchoBarra / 2f - 2.5f, y, 5f, 5f);
    }

    private void DibujarEstadoVacio(Graphics g, Rectangle area)
    {
        _pulsoIdle++;
        int cantidad = 42;
        float margenX = 60f;
        float espacio = 7f;
        float ancho = Math.Max(4f, (area.Width - margenX * 2 - espacio * cantidad) / cantidad);
        float centroY = area.Height * 0.58f;

        for (int i = 0; i < cantidad; i++)
        {
            double onda = Math.Sin((_pulsoIdle * 0.08) + i * 0.45);
            float alto = 18f + (float)(onda + 1) * 18f;
            float x = margenX + i * (ancho + espacio);
            RectangleF rect = new(x, centroY - alto / 2f, ancho, alto);
            Color color = DibujoHelper.Mezclar(ColorAcento1, ColorAcento2, (float)i / cantidad);

            using GraphicsPath path = DibujoHelper.CrearRectanguloRedondeado(rect, ancho / 2f);
            using SolidBrush brush = new(Color.FromArgb(80, color));
            g.FillPath(brush, path);
        }

        string titulo = "VisualBeat";
        string subtitulo = "Carga musica MP3 para ver el espectro en tiempo real";
        using Font tituloFont = new("Segoe UI", 24f, FontStyle.Bold);
        using Font subtituloFont = new("Segoe UI", 10f, FontStyle.Regular);
        Size tituloSize = TextRenderer.MeasureText(titulo, tituloFont);
        Size subtituloSize = TextRenderer.MeasureText(subtitulo, subtituloFont);

        using SolidBrush tituloBrush = new(ColorTexto);
        using SolidBrush subtituloBrush = new(Color.FromArgb(150, ColorTexto));
        g.DrawString(titulo, tituloFont, tituloBrush, area.Width / 2f - tituloSize.Width / 2f, area.Height * 0.28f);
        g.DrawString(subtitulo, subtituloFont, subtituloBrush, area.Width / 2f - subtituloSize.Width / 2f, area.Height * 0.28f + 44);
    }
}
