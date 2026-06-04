using System.Drawing.Drawing2D;
using VisualBeatPlayer.Utils;

namespace VisualBeatPlayer.Views;

public class RoundButton : Button
{
    private bool _mouseEncima;

    public RoundButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Radio = 14;
        Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
    }

    public int Radio { get; set; }
    public Color ColorFondo { get; set; } = Color.FromArgb(0, 224, 255);
    public Color ColorHover { get; set; } = Color.FromArgb(54, 235, 161);
    public Color ColorTexto { get; set; } = Color.FromArgb(10, 14, 24);
    public Color ColorBorde { get; set; } = Color.Transparent;

    protected override void OnMouseEnter(EventArgs e)
    {
        _mouseEncima = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _mouseEncima = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        pevent.Graphics.Clear(Parent?.BackColor ?? BackColor);

        Color fondo = Enabled
            ? (_mouseEncima ? ColorHover : ColorFondo)
            : Color.FromArgb(80, ColorFondo);

        RectangleF rect = new(0.5f, 0.5f, Width - 1f, Height - 1f);
        using GraphicsPath path = DibujoHelper.CrearRectanguloRedondeado(rect, Radio);
        using SolidBrush brush = new(fondo);
        using Pen borde = new(ColorBorde, 1f);

        pevent.Graphics.FillPath(brush, path);
        if (ColorBorde.A > 0)
        {
            pevent.Graphics.DrawPath(borde, path);
        }

        TextRenderer.DrawText(
            pevent.Graphics,
            Text,
            Font,
            ClientRectangle,
            Enabled ? ColorTexto : Color.FromArgb(130, ColorTexto),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
