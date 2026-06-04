using System.Drawing.Drawing2D;
using VisualBeatPlayer.Utils;

namespace VisualBeatPlayer.Views;

public class PanelTarjeta : Panel
{
    public PanelTarjeta()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        Padding = new Padding(18);
        Radio = 18;
    }

    public int Radio { get; set; }
    public Color ColorFondo { get; set; } = Color.FromArgb(17, 24, 39);
    public Color ColorBorde { get; set; } = Color.FromArgb(42, 55, 84);

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? BackColor);
        RectangleF rect = new(0.5f, 0.5f, Width - 1f, Height - 1f);

        using GraphicsPath path = DibujoHelper.CrearRectanguloRedondeado(rect, Radio);
        using SolidBrush fondo = new(ColorFondo);
        using Pen borde = new(ColorBorde, 1f);

        e.Graphics.FillPath(fondo, path);
        e.Graphics.DrawPath(borde, path);

        base.OnPaint(e);
    }
}
