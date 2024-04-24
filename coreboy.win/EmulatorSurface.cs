using coreboy.controller;
using coreboy.gui;
using Button = coreboy.controller.Button;

namespace coreboy.win;

public partial class EmulatorSurface : Form, IController
{
    private IButtonListener? _listener;

    private readonly MenuStrip _menu;
    private readonly PictureBox _pictureBox;
    private readonly Dictionary<Keys, Button> _controls;

    private readonly Emulator _emulator;
    private readonly GameboyOptions _gameboyOptions;
    private CancellationTokenSource _cancellation;

    private readonly object _updateLock = new();

    public EmulatorSurface()
    {
        InitializeComponent();

        _cancellation = new();
        _gameboyOptions = new();
        _emulator = new(_gameboyOptions);

        Controls.Add(_menu = new MenuStrip
        {
            Items =
            {
                new ToolStripMenuItem("Emulator")
                {
                    DropDownItems =
                    {
                        new ToolStripMenuItem("Load ROM", null, (sender, args) => { StartEmulator(); }),
                        new ToolStripMenuItem("Pause", null, (sender, args) => { _emulator.TogglePause(); }),
                        new ToolStripMenuItem("Quit", null, (sender, args) => { Close(); })
                    }
                }
            }
        });

        Controls.Add(_pictureBox = new PictureBox
        {
            Top = _menu.Height,
            Width = BitmapDisplay.DisplayWidth * 5,
            Height = BitmapDisplay.DisplayHeight * 5,
            BackColor = Color.Black,
            SizeMode = PictureBoxSizeMode.Zoom
        });

        _controls = new Dictionary<Keys, Button>
        {
            {Keys.Left, Button.Left},
            {Keys.Right, Button.Right},
            {Keys.Up, Button.Up},
            {Keys.Down, Button.Down},
            {Keys.Z, Button.A},
            {Keys.X, Button.B},
            {Keys.Enter, Button.Start},
            {Keys.Back, Button.Select}
        };

        Height = _menu.Height + _pictureBox.Height + 50;
        Width = _pictureBox.Width;

        ConnectEmulatorToPanel();
    }

    private void ConnectEmulatorToPanel()
    {
        _emulator.Controller = this;
        _emulator.Display.OnFrameProduced += UpdateDisplay;

#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
        KeyDown += WinFormsEmulatorSurface_KeyDown;
        KeyUp += WinFormsEmulatorSurface_KeyUp;
#pragma warning restore CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
        
        Closed += (_, e) => { _cancellation.Cancel(); };
    }

    private void StartEmulator()
    {
        if (_emulator.Active)
        {
            _emulator.Stop(_cancellation);
            _cancellation = new();
            _pictureBox.Image = null;
            Task.Delay(100).Wait();
        }

        using OpenFileDialog dialog = new()
        {
            Filter = "ROM files (*.gb;*.gbc)|*.gb;*.gbc| All files(*.*) |*.*",
            FilterIndex = 0,
            RestoreDirectory = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _gameboyOptions.Rom = dialog.FileName;
            _emulator.Run(_cancellation.Token);
        }
    }

    private void WinFormsEmulatorSurface_KeyDown(object sender, KeyEventArgs e)
    {
        if (_controls.TryGetValue(e.KeyCode, out var button))
        {
            _listener?.OnButtonPress(button);
        }
    }

    private void WinFormsEmulatorSurface_KeyUp(object sender, KeyEventArgs e)
    {
        if (_controls.TryGetValue(e.KeyCode, out var button))
        {
            _listener?.OnButtonRelease(button);
        }
    }

    public void SetButtonListener(IButtonListener listener) => _listener = listener;

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_pictureBox == null)
        {
            return;
        }

        _pictureBox.Width = Width;
        _pictureBox.Height = Height - _menu.Height - 50;
    }

    public void UpdateDisplay(object _, byte[] frame)
    {
        if (!Monitor.TryEnter(_updateLock))
        {
            return;
        }

        try
        {
            using MemoryStream ms = new(frame);
            _pictureBox.Image = Image.FromStream(ms);
        }
        finally
        {
            Monitor.Exit(_updateLock);
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        Environment.Exit(0);
    }
}
