using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using coreboy.controller;
using coreboy.gui;
using Button = coreboy.controller.Button;

namespace coreboy.avalonia.ViewModels;

public partial class MainViewModel : ObservableObject, IController
{
	[ObservableProperty]
	private SKBitmap? bitmap;

	private readonly Emulator emulator;
	private readonly GameboyOptions gbOptions;
	private CancellationTokenSource cancellationSource;
	private IButtonListener? listener;
	private readonly object updateLock = new();
	private readonly Dictionary<Key, Button> controls;

	public MainViewModel()
	{
		cancellationSource = new CancellationTokenSource();
		gbOptions = new GameboyOptions();
		emulator = new Emulator(gbOptions);

		controls = new Dictionary<Key, Button>
		{
			{ Key.Left,  Button.Left },
			{ Key.Right, Button.Right },
			{ Key.Up,    Button.Up },
			{ Key.Down,  Button.Down },
			{ Key.Z,     Button.A },
			{ Key.X,     Button.B },
			{ Key.Enter, Button.Start },
			{ Key.Back,  Button.Select }
		};

		ConnectEmulatorToPanel();
	}

	public void SetButtonListener(IButtonListener buttonListener)
	{
		listener = buttonListener;
	}

	private void ConnectEmulatorToPanel()
	{
		emulator.Controller = this;
		emulator.Display.OnFrameProduced += UpdateDisplay;
	}

	private void UpdateDisplay(object _, byte[] frameBytes)
	{
		if (!Monitor.TryEnter(updateLock))
		{
			return;
		}

		try
		{
			Bitmap = SKBitmap.Decode(frameBytes);
		}
		catch
		{
			// Ignored
		}
		finally
		{
			Monitor.Exit(updateLock);
		}
	}

	internal async Task LoadRom(string fileName)
	{
		if (emulator.Active)
		{
			emulator.Stop(cancellationSource);
			cancellationSource = new CancellationTokenSource();
			await Task.Delay(100);
		}

		gbOptions.Rom = fileName;
		emulator.Run(cancellationSource.Token);
	}

	public void Pause()
	{
		emulator.TogglePause();
	}

	// Ignore the LSP/IntelliSense. This should not be marked as static
	public void SetEmulationSpeed(int multiplier)
	{
		Gameboy.SetSpeedMultiplier(multiplier);
	}

	public async Task Screenshot(string path)
	{
		if (Bitmap == null)
		{
			return;
		}

		string? outputDir = Path.GetDirectoryName(path);
		if (string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
		{
			return;
		}

		SKBitmap scaledBitmap = new(Bitmap.Width * 4, Bitmap.Height * 4);
		Bitmap.ScalePixels(scaledBitmap, SKFilterQuality.None);

		using SKImage image = SKImage.FromBitmap(scaledBitmap);
		using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
		await File.WriteAllBytesAsync(path, data.ToArray());
	}

	internal void KeyDown(KeyEventArgs e)
	{
		if (controls.TryGetValue(e.Key, out Button? button))
		{
			listener?.OnButtonPress(button);
		}
	}

	internal void KeyUp(KeyEventArgs e)
	{
		if (controls.TryGetValue(e.Key, out Button? button))
		{
			listener?.OnButtonRelease(button);
		}
	}

	internal void Close()
	{
		emulator.Display.OnFrameProduced -= UpdateDisplay;
		cancellationSource.Cancel();
	}
}
