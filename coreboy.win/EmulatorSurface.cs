#nullable disable

using coreboy.controller;
using coreboy.gui;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using ISImage = SixLabors.ImageSharp.Image;
using SDImage = System.Drawing.Image;
using Button = coreboy.controller.Button;
using Tsmi = System.Windows.Forms.ToolStripMenuItem;

namespace coreboy.win;

public partial class EmulatorSurface : Form, IController
{
	private IButtonListener listener;

	private byte[] lastFrame = [];
	private readonly MenuStrip menu;
	private readonly PictureBox pictureBox;
	private readonly Dictionary<Keys, Button> controls;

	private readonly Emulator emulator;
	private readonly GameboyOptions gbOptions;
	private CancellationTokenSource cancellationSource;

	private readonly object updateLock = new();

	public EmulatorSurface()
	{
		InitializeComponent();

		Controls.Add(menu = new MenuStrip
		{
			Items =
			{
				new Tsmi("Emulator")
				{
					DropDownItems =
					{
						new Tsmi("Load ROM", null, (_, _) =>
						{
							StartEmulator();
						}),
						new Tsmi("Pause", null, (_, _) =>
						{
							emulator?.TogglePause();
						}),
						new Tsmi("Quit", null, (_, _) =>
						{
							Close();
						})
					}
				},
				new Tsmi("Graphics")
				{
					DropDownItems =
					{
						new Tsmi("Screenshot", null, (_, _) =>
						{
							TakeScreenshot();
						})
					}
				}
			}
		});

		Controls.Add(pictureBox = new PictureBox
		{
			Top = menu.Height,
			Width = BitmapDisplay.DisplayWidth * 5,
			Height = BitmapDisplay.DisplayHeight * 5,
			BackColor = Color.Black,
			SizeMode = PictureBoxSizeMode.Zoom
		});

		controls = new Dictionary<Keys, Button>
		{
			{ Keys.Left,  Button.Left },
			{ Keys.Right, Button.Right },
			{ Keys.Up,    Button.Up },
			{ Keys.Down,  Button.Down },
			{ Keys.Z,     Button.A },
			{ Keys.X,     Button.B },
			{ Keys.Enter, Button.Start },
			{ Keys.Back,  Button.Select }
		};

		Height = menu.Height + pictureBox.Height + 50;
		Width = pictureBox.Width;

		cancellationSource = new CancellationTokenSource();
		gbOptions = new GameboyOptions();
		emulator = new Emulator(gbOptions);

		ConnectEmulatorToPanel();
	}

	private void ConnectEmulatorToPanel()
	{
		emulator.Controller = this;
		emulator.Display.OnFrameProduced += UpdateDisplay;

		KeyDown += EmulatorSurface_KeyDown!;
		KeyUp += EmulatorSurface_KeyUp!;
		Closed += (_, _) => { cancellationSource.Cancel(); };
	}

	private void StartEmulator()
	{
		if (emulator.Active)
		{
			emulator.Stop(cancellationSource);
			cancellationSource = new CancellationTokenSource();
			pictureBox.Image = null;
			Task.Delay(100).Wait();
		}

		using OpenFileDialog dialog = new();
		dialog.Filter = @"ROM files (*.gb;*.gbc)|*.gb;*.gbc";
		dialog.FilterIndex = 0;
		dialog.RestoreDirectory = true;

		if (dialog.ShowDialog() != DialogResult.OK)
		{
			return;
		}

		gbOptions.Rom = dialog.FileName;
		emulator.Run(cancellationSource.Token);
	}

	private void TakeScreenshot()
	{
		emulator.TogglePause();

        var image = ISImage.Load(lastFrame);
        image.Mutate(x => x.Resize(image.Width * 4, image.Height * 4));

        using SaveFileDialog dialog = new();
		dialog.Filter = @"PNG (*.png)|*.png";
		dialog.FilterIndex = 0;
		dialog.RestoreDirectory = true;

		if (dialog.ShowDialog() == DialogResult.OK)
		{
			try
			{
				Monitor.Enter(updateLock);
				using FileStream fs = new(dialog.FileName, FileMode.Create);
                image.Save(fs, new PngEncoder());
            }
			finally
			{
				Monitor.Exit(updateLock);
			}
		}

		emulator.TogglePause();
	}

	private void EmulatorSurface_KeyDown(object sender, KeyEventArgs e)
	{
		if (controls.TryGetValue(e.KeyCode, out var button))
		{
			listener.OnButtonPress(button);
		}
	}

	private void EmulatorSurface_KeyUp(object sender, KeyEventArgs e)
	{
		if (controls.TryGetValue(e.KeyCode, out var button))
		{
			listener.OnButtonRelease(button);
		}
	}

	public void SetButtonListener(IButtonListener buttonListener)
	{
		listener = buttonListener;
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);

		if (pictureBox == null)
		{
			return;
		}

		pictureBox.Width = Width;
		pictureBox.Height = Height - menu.Height - 50;
	}

	private void UpdateDisplay(object _, byte[] frameBytes)
	{
		if (!Monitor.TryEnter(updateLock))
		{
			return;
		}

		try
		{
			lastFrame = frameBytes;
			using MemoryStream ms = new(frameBytes);
			pictureBox.Image = SDImage.FromStream(ms);
        }
		catch
		{
			// YOLO
		}
		finally
		{
			Monitor.Exit(updateLock);
		}
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		base.OnFormClosed(e);
		Dispose(true);
		Environment.Exit(0);
	}
}
