using coreboy.controller;
using coreboy.gui;
using Button = coreboy.controller.Button;
using Tsmi = System.Windows.Forms.ToolStripMenuItem;

namespace coreboy.win;

public partial class EmulatorSurface : Form, IController
{
	private IButtonListener _listener = null!;

	private byte[] _lastFrame = [];
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

		Controls.Add(_menu = new MenuStrip
		{
			Items =
			{
				new Tsmi("Emulator")
				{
					DropDownItems =
					{
						new Tsmi("Load ROM", null, (sender, args) =>
						{
							StartEmulator();
						}),
						new Tsmi("Pause", null, (sender, args) =>
						{
							_emulator?.TogglePause();
						}),
						new Tsmi("Quit", null, (sender, args) =>
						{
							Close();
						})
					}
				},
				new Tsmi("Graphics")
				{
					DropDownItems =
					{
						new Tsmi("Screenshot", null, (sender, args) =>
						{
							TakeScreenshot();
						})
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

		_cancellation = new CancellationTokenSource();
		_gameboyOptions = new GameboyOptions();
		_emulator = new Emulator(_gameboyOptions);

		ConnectEmulatorToPanel();
	}

	private void ConnectEmulatorToPanel()
	{
		_emulator.Controller = this;
		_emulator.Display.OnFrameProduced += UpdateDisplay;

		KeyDown += EmulatorSurface_KeyDown!;
		KeyUp += EmulatorSurface_KeyUp!;
		Closed += (_, e) => { _cancellation.Cancel(); };
	}

	private void StartEmulator()
	{
		if (_emulator.Active)
		{
			_emulator.Stop(_cancellation);
			_cancellation = new CancellationTokenSource();
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

	private void TakeScreenshot()
	{
		_emulator.TogglePause();

		using SaveFileDialog dialog = new()
		{
			Filter = "Bitmap (*.bmp)|*.bmp",
			FilterIndex = 0,
			RestoreDirectory = true
		};

		if (dialog.ShowDialog() == DialogResult.OK)
		{
			try
			{
				Monitor.Enter(_updateLock);
				File.WriteAllBytes(dialog.FileName, _lastFrame);
			}
			finally
			{
				Monitor.Exit(_updateLock);
			}
		}

		_emulator.TogglePause();
	}

	private void EmulatorSurface_KeyDown(object sender, KeyEventArgs e)
	{
		if (_controls.TryGetValue(e.KeyCode, out var button))
		{
			_listener.OnButtonPress(button);
		}
	}

	private void EmulatorSurface_KeyUp(object sender, KeyEventArgs e)
	{
		if (_controls.TryGetValue(e.KeyCode, out var button))
		{
			_listener.OnButtonRelease(button);
		}
	}

	public void SetButtonListener(IButtonListener listener)
	{
		_listener = listener;
	}

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

	public void UpdateDisplay(object _, byte[] frameBytes)
	{
		if (!Monitor.TryEnter(_updateLock))
		{
			return;
		}

		try
		{
			_lastFrame = frameBytes;
			using MemoryStream ms = new(frameBytes);
			_pictureBox.Image = Image.FromStream(ms);
		}
		catch
		{
			// YOLO
		}
		finally
		{
			Monitor.Exit(_updateLock);
		}
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		base.OnFormClosed(e);
		Dispose(true);
		Environment.Exit(0);
	}
}
