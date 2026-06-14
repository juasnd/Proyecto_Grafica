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
    private RoundButton _btnQuitar = null!;
    private RoundButton _btnLimpiar = null!;
    private RoundButton _btnPlay = null!;
    private RoundButton _btnPause = null!;
    private RoundButton _btnStop = null!;
    private RoundButton _btnSiguiente = null!;
    private RoundButton _btnAnterior = null!;
    private RoundButton _btnTema = null!;
    private RoundButton _btnModoVisual = null!;
    private RoundButton _btnCerrar = null!;
    private RoundButton _btnMinimizar = null!;
    private RoundButton _btnMaximizar = null!;
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
            Interval = 24
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
            ? "Agrega canciones para construir la lista"
            : $"{canciones.Count} cancion(es) en la cola";

        _actualizandoLista = false;
    }

    public void MostrarCancionActual(Cancion? cancion)
    {
        if (cancion is null)
        {
            _lblCancionActual.Text = "Ninguna cancion seleccionada";
            _lblArtista.Text = "Carga archivos de audio desde tu computadora";
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
        _btnQuitar.Enabled = hayCanciones;
        _btnLimpiar.Enabled = hayCanciones;
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
        _panelVisualizador.ColorFondo = Color.FromArgb(0, paleta.Fondo);
        _panelVisualizador.ColorBorde = Color.Transparent;
        _panelControles.ColorFondo = Color.Transparent;
        _panelControles.ColorBorde = Color.Transparent;
        _panelControles.ColorAcentoSuperior = Color.Transparent;

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

        AplicarTemaBoton(_btnCargar, true, modo);
        AplicarTemaBoton(_btnQuitar, false, modo);
        AplicarTemaBoton(_btnLimpiar, false, modo);
        AplicarTemaBoton(_btnPlay, true, modo);
        AplicarTemaBoton(_btnPause, false, modo);
        AplicarTemaBoton(_btnStop, false, modo);
        AplicarTemaBoton(_btnSiguiente, false, modo);
        AplicarTemaBoton(_btnAnterior, false, modo);
        AplicarTemaBoton(_btnModoVisual, false, modo);
        AplicarTemaBoton(_btnTema, false, modo);
        AplicarTemaBotonVentana(_btnCerrar, Color.FromArgb(245, 83, 102));
        AplicarTemaBotonVentana(_btnMinimizar, paleta.SuperficieSecundaria);
        AplicarTemaBotonVentana(_btnMaximizar, paleta.SuperficieSecundaria);

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
        _visualizador.BackColor = Color.Transparent;
        _visualizador.ColorLinea = paleta.Borde;
        _visualizador.ColorAcento1 = paleta.AcentoPrincipal;
        _visualizador.ColorAcento2 = paleta.AcentoSecundario;
        _visualizador.ColorAcento3 = paleta.AcentoTerciario;
        _visualizador.ColorTexto = paleta.Texto;
        ActualizarTextoModoVisual();

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
            Padding = new Padding(0),
            Tag = "fondo"
        };
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        Controls.Add(raiz);

        _barraTitulo = CrearBarraTitulo();
        _barraTitulo.Margin = new Padding(20, 10, 20, 0);
        raiz.Controls.Add(_barraTitulo, 0, 0);

        TableLayoutPanel cuerpo = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(20, 0, 20, 0),
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
            Text = "Audio reactivo en tiempo real",
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            Tag = "suave",
            Location = new Point(210, 22)
        };

        _btnTema = CrearBoton("Tema: Oscuro", 150, 36);
        _btnTema.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnTema.Location = new Point(Width - 280, 11);



        _btnMaximizar = CrearBoton("□", 42, 36);
        _btnMaximizar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnMaximizar.Location = new Point(Width - 114, 11);

        _btnMinimizar = CrearBoton("─", 42, 36);  // Nota: cambié "-" por "─"
        _btnMinimizar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnMinimizar.Location = new Point(Width - 160, 11);  // ← Cambia esto (estaba en -118)

        _btnCerrar = CrearBoton("X", 42, 36);
        _btnCerrar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnCerrar.Location = new Point(Width - 68, 11);

        panel.Controls.Add(titulo);
        panel.Controls.Add(subtitulo);
        panel.Controls.Add(_btnTema);
        panel.Controls.Add(_btnMinimizar);
        panel.Controls.Add(_btnMaximizar);
        panel.Controls.Add(_btnCerrar);

        panel.Resize += (_, _) =>
        {
            const int margenDerecho = 10;
            const int anchoBoton = 42;
            const int separacion = 4;

            _btnCerrar.Location =
                new Point(panel.Width - anchoBoton - margenDerecho, 11);

            _btnMaximizar.Location =
                new Point(panel.Width - (anchoBoton * 2) - separacion - margenDerecho, 11);

            _btnMinimizar.Location =
                new Point(panel.Width - (anchoBoton * 3) - (separacion * 2) - margenDerecho, 11);

            _btnTema.Location =
                new Point(_btnMinimizar.Left - 160, 11);
        };

        return panel;
    }
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        MaximizedBounds = Screen.FromHandle(Handle).WorkingArea;
    }


    private PanelTarjeta CrearPanelLista()
    {
        PanelTarjeta panel = new()
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 14, 12),
            Radio = 10
        };

        Label titulo = new()
        {
            Text = "Cola de reproduccion",
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Segoe UI", 15f, FontStyle.Bold)
        };

        _lblEstadoLista = new Label
        {
            Text = "Agrega canciones para construir la lista",
            Dock = DockStyle.Top,
            Height = 26,
            Tag = "suave"
        };

        FlowLayoutPanel acciones = new()
        {
            Dock = DockStyle.Top,
            Height = 104,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Tag = "superficie"
        };

        _btnCargar = CrearBoton("Agregar audio", 0, 42);
        _btnCargar.Width = 260;
        _btnCargar.Margin = new Padding(0, 8, 0, 4);

        FlowLayoutPanel gestion = new()
        {
            Width = 260,
            Height = 42,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Tag = "superficie"
        };

        _btnQuitar = CrearBoton("Quitar", 124, 38);
        _btnQuitar.Margin = new Padding(0, 2, 6, 2);
        _btnLimpiar = CrearBoton("Limpiar", 124, 38);
        _btnLimpiar.Margin = new Padding(6, 2, 0, 2);
        gestion.Controls.Add(_btnQuitar);
        gestion.Controls.Add(_btnLimpiar);
        acciones.Controls.Add(_btnCargar);
        acciones.Controls.Add(gestion);

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
        panel.Controls.Add(acciones);
        panel.Controls.Add(_lblEstadoLista);
        panel.Controls.Add(titulo);

        return panel;
    }

    private PanelTarjeta CrearPanelVisualizador()
    {
        PanelTarjeta panel = new()
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(14, 8, 0, 12),
            Radio = 10,
            MostrarBorde = false
        };

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Tag = "fondo"
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Panel encabezado = new()
        {
            Dock = DockStyle.Fill,
            Tag = "fondo"
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
            Text = "Carga archivos de audio desde tu computadora",
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
            Margin = new Padding(0),
            Padding = new Padding(24, 8, 24, 16),
            Radio = 0,
            MostrarBorde = false
        };

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Tag = "fondo"
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        TableLayoutPanel filaProgreso = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            Tag = "fondo"
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
            ColumnCount = 3,
            Tag = "fondo"
        };
        filaBotones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        filaBotones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        filaBotones.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));


        FlowLayoutPanel modoPanel = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 7, 0, 0),
            Tag = "fondo"
        };

        _btnModoVisual = CrearBoton("Modo", 150, 38);
        _btnModoVisual.Margin = new Padding(0, 4, 0, 0);
        modoPanel.Controls.Add(_btnModoVisual);

        FlowLayoutPanel controles = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            Anchor = AnchorStyles.None,
            Padding = new Padding(0, 3, 0, 0),
            Tag = "fondo"
        };

        _btnAnterior = CrearBoton("<<", 56, 42);
        _btnPlay = CrearBoton("▶", 76, 42);
        _btnPause = CrearBoton("||", 76, 42);
        _btnStop = CrearBoton("■", 68, 42);
        _btnSiguiente = CrearBoton(">>", 56, 42);


        controles.Controls.Add(_btnAnterior);
        controles.Controls.Add(_btnPlay);
        controles.Controls.Add(_btnPause);
        controles.Controls.Add(_btnStop);
        controles.Controls.Add(_btnSiguiente);
        controles.AutoSize = true;
        controles.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        Panel volumenPanel = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 4, 0, 0),
            Tag = "fondo"
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

        filaBotones.Controls.Add(modoPanel, 0, 0);
        filaBotones.Controls.Add(controles, 1, 0);
        filaBotones.Controls.Add(volumenPanel, 2, 0);

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

    private void ActualizarTextoModoVisual()
    {
        if (_btnModoVisual is null)
        {
            return;
        }

        _btnModoVisual.Text = _visualizador.Modo switch
        {
            ModoVisualizacion.Ondas => "Modo: Ondas",
            ModoVisualizacion.Particulas => "Modo: Particulas",
            ModoVisualizacion.Geometria => "Modo: Geometria",
            _ => "Modo: Espectro"
        };
    }
    private void AlternarMaximizar()
    {
        if (WindowState == FormWindowState.Maximized)
        {
            WindowState = FormWindowState.Normal;
            _btnMaximizar.Text = "□";  // Símbolo de maximizar
        }
        else
        {
            WindowState = FormWindowState.Maximized;
            _btnMaximizar.Text = "❐";  // Símbolo de restaurar (ventanas superpuestas)
        }
    }

    private void ConectarEventos()
    {
        _btnCargar.Click += (_, _) => AbrirDialogoCanciones();
        _btnQuitar.Click += (_, _) => _controller.QuitarCancion(_lstCanciones.SelectedIndex);
        _btnLimpiar.Click += (_, _) => _controller.LimpiarLista();
        _btnPlay.Click += (_, _) => _controller.Reproducir();
        _btnPause.Click += (_, _) => _controller.Pausar();
        _btnStop.Click += (_, _) => _controller.Detener();
        _btnSiguiente.Click += (_, _) => _controller.Siguiente();
        _btnAnterior.Click += (_, _) => _controller.Anterior();
        _btnModoVisual.Click += (_, _) =>
        {
            _visualizador.CambiarModo();
            ActualizarTextoModoVisual();
        };
        _btnTema.Click += (_, _) => _controller.AlternarTema();
        _btnCerrar.Click += (_, _) => Close();
        _btnMinimizar.Click += (_, _) => WindowState = FormWindowState.Minimized;
        _btnMaximizar.Click += (_, _) => AlternarMaximizar();

        _lstCanciones.SelectedIndexChanged += (_, _) =>
        {
            if (!_actualizandoLista && _lstCanciones.SelectedIndex >= 0)
            {
                _controller.SeleccionarCancion(_lstCanciones.SelectedIndex);
            }
        };

        _lstCanciones.MouseDoubleClick += (_, _) =>
        {
            if (_lstCanciones.SelectedIndex >= 0)
            {
                _controller.SeleccionarCancion(_lstCanciones.SelectedIndex);
                _controller.Reproducir();
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
            Title = "Seleccionar canciones",
            Filter = "Audio compatible (*.mp3;*.wav;*.aiff;*.aif;*.wma)|*.mp3;*.wav;*.aiff;*.aif;*.wma|Todos los archivos (*.*)|*.*",
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

    private void AplicarTemaBoton(RoundButton boton, bool principal, ModoTema modo)
    {
        if(modo == ModoTema.Claro)
        {
            boton.ColorFondo = principal ? _paleta.AcentoTerciario : _paleta.SuperficieSecundaria;
            boton.ColorHover = principal ? _paleta.AcentoSecundario : _paleta.Superficie;
            boton.ColorTexto = principal ? Color.FromArgb(8, 13, 24) : _paleta.Texto;
            boton.ColorBorde = principal ? Color.Transparent : _paleta.Borde;
        }
        else
        {
            boton.ColorFondo = principal ? _paleta.AcentoPrincipal : _paleta.SuperficieSecundaria;
            boton.ColorHover = principal ? _paleta.AcentoSecundario : _paleta.Superficie;
            boton.ColorTexto = principal ? Color.FromArgb(8, 13, 24) : _paleta.Texto;
            boton.ColorBorde = principal ? Color.Transparent : _paleta.Borde;
        }
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

    private void InitializeComponent()
    {

    }

    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
}
