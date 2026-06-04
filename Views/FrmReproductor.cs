using System.Runtime.InteropServices;
using VisualBeatPlayer.Controllers;
using VisualBeatPlayer.Models;

namespace VisualBeatPlayer.Views;

public class FrmReproductor : Form, IReproductorView
{
    private readonly ReproductorController _controller;
    private readonly System.Windows.Forms.Timer _temporizador;

    private Panel _barraTitulo = null!;
    private PanelTarjeta _panelLista = null!;
    private PanelTarjeta _panelVisualizador = null!;
    private PanelTarjeta _panelControles = null!;
    private VisualizerControl _visualizador = null!;
    private ListBox _lstCanciones = null!;
    private Label _lblCancionActual = null!;
    private Label _lblArtista = null!;
    private Label _lblTiempoActual = null!;
    private Label _lblDuracion = null!;
    private Label _lblEstadoLista = null!;
    private RoundButton _btnCargar = null!;
    private RoundButton _btnPlay = null!;
    private RoundButton _btnPause = null!;
    private RoundButton _btnStop = null!;
    private RoundButton _btnSiguiente = null!;
    private RoundButton _btnAnterior = null!;
    private RoundButton _btnTema = null!;
    private RoundButton _btnCerrar = null!;
    private RoundButton _btnMinimizar = null!;
    private BarraDeslizante _barraProgreso = null!;
    private BarraDeslizante _barraVolumen = null!;
    private PaletaTema _paleta = null!;
    private bool _actualizandoLista;
    private bool _actualizandoProgreso;

    public FrmReproductor()
    {
        _controller = new ReproductorController(this);
        _temporizador = new System.Windows.Forms.Timer
        {
            Interval = 33
        };
        _temporizador.Tick += Temporizador_Tick;

        ConfigurarFormulario();
        CrearInterfaz();
        ConectarEventos();

        _controller.Inicializar();
        _temporizador.Start();
    }

    public void MostrarCanciones(IReadOnlyList<Cancion> canciones, int indiceActual)
    {
        _actualizandoLista = true;
        _lstCanciones.Items.Clear();

        foreach (Cancion cancion in canciones)
        {
            _lstCanciones.Items.Add(cancion);
        }

        if (indiceActual >= 0 && indiceActual < _lstCanciones.Items.Count)
        {
            _lstCanciones.SelectedIndex = indiceActual;
        }

        _lblEstadoLista.Text = canciones.Count == 0
            ? "No hay canciones cargadas"
            : $"{canciones.Count} cancion(es) en la lista";

        _actualizandoLista = false;
    }

    public void MostrarCancionActual(Cancion? cancion)
    {
        if (cancion is null)
        {
            _lblCancionActual.Text = "Ninguna cancion seleccionada";
            _lblArtista.Text = "Carga archivos MP3 desde tu computadora";
            return;
        }

        _lblCancionActual.Text = cancion.Nombre;
        _lblArtista.Text = $"{cancion.Artista}  |  {cancion.DuracionTexto}";
    }

    public void ActualizarProgreso(TimeSpan posicion, TimeSpan duracion)
    {
        _actualizandoProgreso = true;

        int duracionSegundos = Math.Max(0, (int)duracion.TotalSeconds);
        int posicionSegundos = Math.Clamp((int)posicion.TotalSeconds, 0, duracionSegundos);

        _barraProgreso.Maximum = Math.Max(1, duracionSegundos);
        _barraProgreso.EstablecerValorSinNotificar(posicionSegundos);
        _lblTiempoActual.Text = FormatearTiempo(posicion);
        _lblDuracion.Text = FormatearTiempo(duracion);

        _actualizandoProgreso = false;
    }

    public void ActualizarControles(bool hayCanciones)
    {
        _btnPlay.Enabled = hayCanciones;
        _btnPause.Enabled = hayCanciones;
        _btnStop.Enabled = hayCanciones;
        _btnSiguiente.Enabled = hayCanciones;
        _btnAnterior.Enabled = hayCanciones;
        _barraProgreso.Enabled = hayCanciones;
    }

    public void AplicarTema(PaletaTema paleta, ModoTema modo)
    {
        _paleta = paleta;
        BackColor = paleta.Fondo;
        _barraTitulo.BackColor = paleta.Fondo;

        AplicarTemaPanel(_panelLista, paleta);
        AplicarTemaPanel(_panelVisualizador, paleta);
        AplicarTemaPanel(_panelControles, paleta);
        AplicarFondosContenedores(paleta);

        foreach (Control control in ObtenerControles(this))
        {
            if (control is Label label)
            {
                label.ForeColor = label.Tag?.ToString() == "suave" ? paleta.TextoSuave : paleta.Texto;
                label.BackColor = label.Parent?.BackColor ?? paleta.Superficie;
            }
        }

        _lstCanciones.BackColor = paleta.Superficie;
        _lstCanciones.ForeColor = paleta.Texto;

        AplicarTemaBoton(_btnCargar, true);
        AplicarTemaBoton(_btnPlay, true);
        AplicarTemaBoton(_btnPause, false);
        AplicarTemaBoton(_btnStop, false);
        AplicarTemaBoton(_btnSiguiente, false);
        AplicarTemaBoton(_btnAnterior, false);
        AplicarTemaBoton(_btnTema, false);
        AplicarTemaBotonVentana(_btnCerrar, Color.FromArgb(245, 83, 102));
        AplicarTemaBotonVentana(_btnMinimizar, paleta.SuperficieSecundaria);

        _btnTema.Text = modo == ModoTema.Oscuro ? "Tema: Oscuro" : "Tema: Claro";
        _barraProgreso.ColorPista = paleta.SuperficieSecundaria;
        _barraProgreso.ColorAcento1 = paleta.AcentoPrincipal;
        _barraProgreso.ColorAcento2 = paleta.AcentoSecundario;
        _barraProgreso.ColorPulgar = paleta.Texto;

        _barraVolumen.ColorPista = paleta.SuperficieSecundaria;
        _barraVolumen.ColorAcento1 = paleta.AcentoSecundario;
        _barraVolumen.ColorAcento2 = paleta.AcentoTerciario;
        _barraVolumen.ColorPulgar = paleta.Texto;

        _visualizador.ColorFondo = paleta.Fondo;
        _visualizador.BackColor = paleta.Fondo;
        _visualizador.ColorLinea = paleta.Borde;
        _visualizador.ColorAcento1 = paleta.AcentoPrincipal;
        _visualizador.ColorAcento2 = paleta.AcentoSecundario;
        _visualizador.ColorAcento3 = paleta.AcentoTerciario;
        _visualizador.ColorTexto = paleta.Texto;

        Invalidate(true);
    }

    public void MostrarMensaje(string mensaje, string titulo)
    {
        MessageBox.Show(this, mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public void EjecutarEnUI(Action accion)
    {
        if (InvokeRequired)
        {
            BeginInvoke(accion);
            return;
        }

        accion();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _temporizador.Stop();
        _controller.Dispose();
        base.OnFormClosing(e);
    }

    private void ConfigurarFormulario()
    {
        Text = "VisualBeatPlayer";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1180, 740);
        MinimumSize = new Size(980, 620);
        FormBorderStyle = FormBorderStyle.None;
        DoubleBuffered = true;
        Font = new Font("Segoe UI", 10f, FontStyle.Regular);
    }

    private void CrearInterfaz()
    {
        TableLayoutPanel raiz = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(20, 10, 20, 20),
            Tag = "fondo"
        };
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 148));
        Controls.Add(raiz);

        _barraTitulo = CrearBarraTitulo();
        raiz.Controls.Add(_barraTitulo, 0, 0);

        TableLayoutPanel cuerpo = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Tag = "fondo"
        };
        cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310));
        cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        raiz.Controls.Add(cuerpo, 0, 1);

        _panelLista = CrearPanelLista();
        cuerpo.Controls.Add(_panelLista, 0, 0);

        _panelVisualizador = CrearPanelVisualizador();
        cuerpo.Controls.Add(_panelVisualizador, 1, 0);

        _panelControles = CrearPanelControles();
        raiz.Controls.Add(_panelControles, 0, 2);
    }

    private Panel CrearBarraTitulo()
    {
        Panel panel = new()
        {
            Dock = DockStyle.Fill
        };
        panel.MouseDown += BarraTitulo_MouseDown;

        Label titulo = new()
        {
            Text = "VisualBeatPlayer",
            AutoSize = true,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            Location = new Point(4, 10)
        };

        Label subtitulo = new()
        {
            Text = "Reproductor local MP3 con visualizador FFT",
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            Tag = "suave",
            Location = new Point(210, 22)
        };

        _btnTema = CrearBoton("Tema: Oscuro", 150, 36);
        _btnTema.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnTema.Location = new Point(Width - 280, 11);

        _btnMinimizar = CrearBoton("-", 42, 36);
        _btnMinimizar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnMinimizar.Location = new Point(Width - 118, 11);

        _btnCerrar = CrearBoton("X", 42, 36);
        _btnCerrar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnCerrar.Location = new Point(Width - 68, 11);

        panel.Controls.Add(titulo);
        panel.Controls.Add(subtitulo);
        panel.Controls.Add(_btnTema);
        panel.Controls.Add(_btnMinimizar);
        panel.Controls.Add(_btnCerrar);

        panel.Resize += (_, _) =>
        {
            _btnTema.Location = new Point(panel.Width - 272, 11);
            _btnMinimizar.Location = new Point(panel.Width - 114, 11);
            _btnCerrar.Location = new Point(panel.Width - 64, 11);
        };

        return panel;
    }

    private PanelTarjeta CrearPanelLista()
    {
        PanelTarjeta panel = new()
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 14, 8)
        };

        Label titulo = new()
        {
            Text = "Biblioteca",
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Segoe UI", 15f, FontStyle.Bold)
        };

        _lblEstadoLista = new Label
        {
            Text = "No hay canciones cargadas",
            Dock = DockStyle.Top,
            Height = 26,
            Tag = "suave"
        };

        _btnCargar = CrearBoton("Cargar MP3", 0, 44);
        _btnCargar.Dock = DockStyle.Top;
        _btnCargar.Margin = new Padding(0, 8, 0, 14);

        _lstCanciones = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 54,
            IntegralHeight = false
        };
        _lstCanciones.DrawItem += LstCanciones_DrawItem;

        panel.Controls.Add(_lstCanciones);
        panel.Controls.Add(_btnCargar);
        panel.Controls.Add(_lblEstadoLista);
        panel.Controls.Add(titulo);

        return panel;
    }

    private PanelTarjeta CrearPanelVisualizador()
    {
        PanelTarjeta panel = new()
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(14, 8, 0, 8)
        };

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Tag = "superficie"
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Panel encabezado = new()
        {
            Dock = DockStyle.Fill,
            Tag = "superficie"
        };

        _lblCancionActual = new Label
        {
            Text = "Ninguna cancion seleccionada",
            AutoEllipsis = true,
            Dock = DockStyle.Top,
            Height = 38,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold)
        };

        _lblArtista = new Label
        {
            Text = "Carga archivos MP3 desde tu computadora",
            AutoEllipsis = true,
            Dock = DockStyle.Top,
            Height = 24,
            Tag = "suave"
        };

        encabezado.Controls.Add(_lblArtista);
        encabezado.Controls.Add(_lblCancionActual);

        _visualizador = new VisualizerControl
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 6, 0, 0)
        };

        layout.Controls.Add(encabezado, 0, 0);
        layout.Controls.Add(_visualizador, 0, 1);
        panel.Controls.Add(layout);

        return panel;
    }

    private PanelTarjeta CrearPanelControles()
    {
        PanelTarjeta panel = new()
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 0, 0)
        };

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Tag = "superficie"
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        TableLayoutPanel filaProgreso = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            Tag = "superficie"
        };
        filaProgreso.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        filaProgreso.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        filaProgreso.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));

        _lblTiempoActual = new Label
        {
            Text = "00:00",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Tag = "suave"
        };

        _lblDuracion = new Label
        {
            Text = "00:00",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Tag = "suave"
        };

        _barraProgreso = new BarraDeslizante
        {
            Dock = DockStyle.Fill,
            Minimum = 0,
            Maximum = 1
        };

        filaProgreso.Controls.Add(_lblTiempoActual, 0, 0);
        filaProgreso.Controls.Add(_barraProgreso, 1, 0);
        filaProgreso.Controls.Add(_lblDuracion, 2, 0);

        TableLayoutPanel filaBotones = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Tag = "superficie"
        };
        filaBotones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        filaBotones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

        FlowLayoutPanel controles = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            Tag = "superficie"
        };

        _btnAnterior = CrearBoton("Anterior", 96, 44);
        _btnPlay = CrearBoton("Play", 92, 44);
        _btnPause = CrearBoton("Pause", 92, 44);
        _btnStop = CrearBoton("Stop", 92, 44);
        _btnSiguiente = CrearBoton("Siguiente", 108, 44);

        controles.Controls.Add(_btnAnterior);
        controles.Controls.Add(_btnPlay);
        controles.Controls.Add(_btnPause);
        controles.Controls.Add(_btnStop);
        controles.Controls.Add(_btnSiguiente);

        Panel volumenPanel = new()
        {
            Dock = DockStyle.Fill,
            Tag = "superficie"
        };

        Label lblVolumen = new()
        {
            Text = "Volumen",
            Dock = DockStyle.Top,
            Height = 24,
            Tag = "suave"
        };

        _barraVolumen = new BarraDeslizante
        {
            Dock = DockStyle.Top,
            Minimum = 0,
            Maximum = 100,
            Value = 70
        };

        volumenPanel.Controls.Add(_barraVolumen);
        volumenPanel.Controls.Add(lblVolumen);

        filaBotones.Controls.Add(controles, 0, 0);
        filaBotones.Controls.Add(volumenPanel, 1, 0);

        layout.Controls.Add(filaProgreso, 0, 0);
        layout.Controls.Add(filaBotones, 0, 1);
        panel.Controls.Add(layout);

        return panel;
    }

    private RoundButton CrearBoton(string texto, int ancho, int alto)
    {
        return new RoundButton
        {
            Text = texto,
            Width = ancho,
            Height = alto,
            Margin = new Padding(5),
            Radio = 14
        };
    }

    private void ConectarEventos()
    {
        _btnCargar.Click += (_, _) => AbrirDialogoCanciones();
        _btnPlay.Click += (_, _) => _controller.Reproducir();
        _btnPause.Click += (_, _) => _controller.Pausar();
        _btnStop.Click += (_, _) => _controller.Detener();
        _btnSiguiente.Click += (_, _) => _controller.Siguiente();
        _btnAnterior.Click += (_, _) => _controller.Anterior();
        _btnTema.Click += (_, _) => _controller.AlternarTema();
        _btnCerrar.Click += (_, _) => Close();
        _btnMinimizar.Click += (_, _) => WindowState = FormWindowState.Minimized;

        _lstCanciones.SelectedIndexChanged += (_, _) =>
        {
            if (!_actualizandoLista && _lstCanciones.SelectedIndex >= 0)
            {
                _controller.SeleccionarCancion(_lstCanciones.SelectedIndex);
            }
        };

        _barraProgreso.ValueChanged += (_, _) =>
        {
            if (!_actualizandoProgreso && _barraProgreso.EstaArrastrando)
            {
                _controller.CambiarPosicion(_barraProgreso.Value);
            }
        };

        _barraVolumen.ValueChanged += (_, _) => _controller.CambiarVolumen(_barraVolumen.Value);
    }

    private void AbrirDialogoCanciones()
    {
        using OpenFileDialog dialogo = new()
        {
            Title = "Seleccionar canciones MP3",
            Filter = "Archivos MP3 (*.mp3)|*.mp3",
            Multiselect = true
        };

        if (dialogo.ShowDialog(this) == DialogResult.OK)
        {
            _controller.CargarCanciones(dialogo.FileNames);
        }
    }

    private void Temporizador_Tick(object? sender, EventArgs e)
    {
        _controller.ActualizarProgreso();
        _visualizador.ActualizarEspectro(_controller.ObtenerEspectro());
    }

    private void LstCanciones_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0)
        {
            return;
        }

        Cancion cancion = (Cancion)_lstCanciones.Items[e.Index];
        bool seleccionado = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        Color fondo = seleccionado ? Color.FromArgb(48, _paleta.AcentoPrincipal) : _paleta.Superficie;
        Color texto = seleccionado ? _paleta.Texto : _paleta.Texto;
        Color textoSuave = seleccionado ? _paleta.AcentoSecundario : _paleta.TextoSuave;

        using SolidBrush fondoBrush = new(fondo);
        e.Graphics.FillRectangle(fondoBrush, e.Bounds);

        Rectangle nombreRect = new(e.Bounds.X + 12, e.Bounds.Y + 8, e.Bounds.Width - 24, 22);
        Rectangle detalleRect = new(e.Bounds.X + 12, e.Bounds.Y + 31, e.Bounds.Width - 24, 18);

        TextRenderer.DrawText(e.Graphics, cancion.Nombre, new Font("Segoe UI", 10f, FontStyle.Bold), nombreRect, texto, TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(e.Graphics, cancion.DuracionTexto, new Font("Segoe UI", 8.5f, FontStyle.Regular), detalleRect, textoSuave, TextFormatFlags.EndEllipsis);
    }

    private void AplicarTemaPanel(PanelTarjeta panel, PaletaTema paleta)
    {
        panel.ColorFondo = paleta.Superficie;
        panel.ColorBorde = paleta.Borde;
        panel.BackColor = paleta.Fondo;
    }

    private void AplicarTemaBoton(RoundButton boton, bool principal)
    {
        boton.ColorFondo = principal ? _paleta.AcentoPrincipal : _paleta.SuperficieSecundaria;
        boton.ColorHover = principal ? _paleta.AcentoSecundario : Color.FromArgb(70, _paleta.AcentoPrincipal);
        boton.ColorTexto = principal ? Color.FromArgb(8, 13, 24) : _paleta.Texto;
        boton.ColorBorde = principal ? Color.Transparent : _paleta.Borde;
        boton.BackColor = boton.Parent?.BackColor ?? _paleta.Superficie;
        boton.Invalidate();
    }

    private void AplicarTemaBotonVentana(RoundButton boton, Color fondo)
    {
        boton.ColorFondo = fondo;
        boton.ColorHover = _paleta.AcentoPrincipal;
        boton.ColorTexto = _paleta.Texto;
        boton.ColorBorde = Color.Transparent;
        boton.BackColor = boton.Parent?.BackColor ?? _paleta.Fondo;
        boton.Invalidate();
    }

    private void AplicarFondosContenedores(PaletaTema paleta)
    {
        foreach (Control control in ObtenerControles(this))
        {
            if (control is PanelTarjeta or VisualizerControl or RoundButton or BarraDeslizante or ListBox or Label)
            {
                continue;
            }

            if (control is Panel or TableLayoutPanel or FlowLayoutPanel)
            {
                control.BackColor = control.Tag?.ToString() == "fondo" ? paleta.Fondo : paleta.Superficie;
            }
        }
    }

    private IEnumerable<Control> ObtenerControles(Control contenedor)
    {
        foreach (Control control in contenedor.Controls)
        {
            yield return control;

            foreach (Control hijo in ObtenerControles(control))
            {
                yield return hijo;
            }
        }
    }

    private static string FormatearTiempo(TimeSpan tiempo)
    {
        if (tiempo <= TimeSpan.Zero)
        {
            return "00:00";
        }

        return $"{(int)tiempo.TotalMinutes:00}:{tiempo.Seconds:00}";
    }

    private void BarraTitulo_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        ReleaseCapture();
        SendMessage(Handle, 0x112, 0xf012, 0);
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
}
