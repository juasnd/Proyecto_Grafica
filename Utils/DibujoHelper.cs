using System.Drawing.Drawing2D;

namespace VisualBeatPlayer.Utils;

public static class DibujoHelper
{
    public static GraphicsPath CrearRectanguloRedondeado(RectangleF rectangulo, float radio)
    {
        GraphicsPath path = new();
        float diametro = radio * 2f;
        RectangleF arco = new(rectangulo.Location, new SizeF(diametro, diametro));

        path.AddArc(arco, 180, 90);
        arco.X = rectangulo.Right - diametro;
        path.AddArc(arco, 270, 90);
        arco.Y = rectangulo.Bottom - diametro;
        path.AddArc(arco, 0, 90);
        arco.X = rectangulo.Left;
        path.AddArc(arco, 90, 90);
        path.CloseFigure();

        return path;
    }

    public static Color Mezclar(Color color1, Color color2, float cantidad)
    {
        cantidad = Math.Clamp(cantidad, 0f, 1f);
        int r = (int)(color1.R + (color2.R - color1.R) * cantidad);
        int g = (int)(color1.G + (color2.G - color1.G) * cantidad);
        int b = (int)(color1.B + (color2.B - color1.B) * cantidad);
        return Color.FromArgb(r, g, b);
    }
}
