using System.ComponentModel;
using System.Drawing.Drawing2D;
using VisualBeatPlayer.Utils;

namespace VisualBeatPlayer.Views;

public class BarraDeslizante : Control
{
    private int _minimum;
    private int _maximum = 100;
    private int _valor;
    private bool _arrastrando;

    public BarraDeslizante()
    {
        Height = 32;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
    }

    public event EventHandler? ValueChanged;

    [DefaultValue(0)]
    public int Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_maximum < _minimum)
            {
                _maximum = _minimum;
            }

            EstablecerValor(_valor, false);
        }
    }

    [DefaultValue(100)]
    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(value, _minimum);
            EstablecerValor(_valor, false);
        }
    }

    [DefaultValue(0)]
    public int Value
    {
        get => _valor;
        set => EstablecerValor(value, true);
    }

    public bool EstaArrastrando => _arrastrando;
    public Color ColorPista { get; set; } = Color.FromArgb(42, 55, 84);
    public Color ColorAcento1 { get; set; } = Color.FromArgb(0, 224, 255);
    public Color ColorAcento2 { get; set; } = Color.FromArgb(54, 235, 161);
    public Color ColorPulgar { get; set; } = Color.White;

    public void EstablecerValorSinNotificar(int valor)
    {
        EstablecerValor(valor, false);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? BackColor);

        Rectangle pista = new(8, Height / 2 - 4, Width - 16, 8);
        float porcentaje = _maximum == _minimum ? 0f : (float)(_valor - _minimum) / (_maximum - _minimum);
        int anchoLleno = (int)(pista.Width * porcentaje);
        Rectangle lleno = new(pista.X, pista.Y, Math.Max(8, anchoLleno), pista.Height);

        using GraphicsPath pistaPath = DibujoHelper.CrearRectanguloRedondeado(pista, 4);
        using SolidBrush pistaBrush = new(ColorPista);
        e.Graphics.FillPath(pistaBrush, pistaPath);

        if (anchoLleno > 0)
        {
            using GraphicsPath llenoPath = DibujoHelper.CrearRectanguloRedondeado(lleno, 4);
            using LinearGradientBrush gradiente = new(lleno, ColorAcento1, ColorAcento2, LinearGradientMode.Horizontal);
            e.Graphics.FillPath(gradiente, llenoPath);
        }

        int xPulgar = pista.X + anchoLleno;
        Rectangle pulgar = new(xPulgar - 7, Height / 2 - 7, 14, 14);
        using SolidBrush pulgarBrush = new(ColorPulgar);
        using Pen bordePulgar = new(Color.FromArgb(90, ColorAcento1), 3f);
        e.Graphics.FillEllipse(pulgarBrush, pulgar);
        e.Graphics.DrawEllipse(bordePulgar, pulgar);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        _arrastrando = true;
        EstablecerDesdeMouse(e.X);
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_arrastrando)
        {
            EstablecerDesdeMouse(e.X);
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _arrastrando = false;
        base.OnMouseUp(e);
    }

    private void EstablecerDesdeMouse(int x)
    {
        int ancho = Math.Max(1, Width - 16);
        float porcentaje = Math.Clamp((x - 8f) / ancho, 0f, 1f);
        int nuevoValor = _minimum + (int)Math.Round((_maximum - _minimum) * porcentaje);
        EstablecerValor(nuevoValor, true);
    }

    private void EstablecerValor(int valor, bool notificar)
    {
        int valorSeguro = Math.Clamp(valor, _minimum, _maximum);
        if (_valor == valorSeguro)
        {
            Invalidate();
            return;
        }

        _valor = valorSeguro;
        Invalidate();

        if (notificar)
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
