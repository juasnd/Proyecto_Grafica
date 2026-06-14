using VisualBeatPlayer.Models;

namespace VisualBeatPlayer.Services;

public class ThemeService
{
    public ModoTema ModoActual { get; private set; } = ModoTema.Oscuro;

    public PaletaTema ObtenerPaletaActual()
    {
        return ObtenerPaleta(ModoActual);
    }

    public PaletaTema CambiarModo(ModoTema modo)
    {
        ModoActual = modo;
        return ObtenerPaleta(modo);
    }

    public PaletaTema AlternarModo()
    {
        ModoActual = ModoActual == ModoTema.Oscuro ? ModoTema.Claro : ModoTema.Oscuro;
        return ObtenerPaletaActual();
    }

    private static PaletaTema ObtenerPaleta(ModoTema modo)
    {
        if (modo == ModoTema.Claro)
        {
            return new PaletaTema
            {
                Fondo = Color.FromArgb(243, 247, 252),
                Superficie = Color.FromArgb(255, 255, 255),
                SuperficieSecundaria = Color.FromArgb(231, 239, 249),
                Texto = Color.FromArgb(24, 31, 44),
                TextoSuave = Color.FromArgb(92, 104, 122),
                Borde = Color.FromArgb(207, 220, 235),
                AcentoPrincipal = Color.FromArgb(97, 15, 11),   
                AcentoSecundario = Color.FromArgb(255, 40, 21),   
                AcentoTerciario = Color.FromArgb(255, 167, 3),  
                Sombra = Color.FromArgb(30, 97, 118, 140)
            };
        }

        return new PaletaTema
        {
            Fondo = Color.FromArgb(10, 14, 24),
            Superficie = Color.FromArgb(17, 24, 39),
            SuperficieSecundaria = Color.FromArgb(26, 35, 56),
            Texto = Color.FromArgb(237, 244, 255),
            TextoSuave = Color.FromArgb(151, 164, 185),
            Borde = Color.FromArgb(42, 55, 84),
            AcentoPrincipal = Color.FromArgb(0, 224, 255),
            AcentoSecundario = Color.FromArgb(54, 235, 161),
            AcentoTerciario = Color.FromArgb(177, 96, 255),
            Sombra = Color.FromArgb(90, 0, 0, 0)
        };
    }
}
