using System.Runtime.InteropServices;
using coreboy.controller;
using coreboy.gpu;
using coreboy.memory.cart;
using coreboy.serial;
using coreboy.sound;

namespace coreboy.gui;

public class Emulator(GameboyOptions options) : IRunnable
{
	public Gameboy? Gameboy { get; set; }
	public IDisplay Display { get; set; } = new BitmapDisplay();
	public IController Controller { get; set; } = new NullController();
	public ISerialEndpoint ISerialEndpoint { get; set; } = new NullSerialEndpoint();
	public GameboyOptions Options { get; set; } = options;
	public bool Active { get; set; }

	private readonly List<Thread> _runnables = [];

	public void Run(CancellationToken token)
	{
		if (!Options.RomSpecified || !Path.Exists(Options.RomFile?.FullName))
		{
			throw new ArgumentException("The ROM path doesn't exist");
		}

		Cartridge rom = new(Options);
		Gameboy = CreateGameboy(rom);

		if (Options.Headless)
		{
			Gameboy.Run(token);
			return;
		}

		if (Display is IRunnable runnableDisplay)
		{
			_runnables.Add(new Thread(() => runnableDisplay.Run(token))
			{
				Priority = ThreadPriority.AboveNormal
			});
		}

		_runnables.Add(new Thread(() => Gameboy.Run(token))
		{
			Priority = ThreadPriority.AboveNormal
		});

		_runnables.ForEach(t => t.Start());
		Active = true;
	}

	public void Stop(CancellationTokenSource source)
	{
		if (!Active)
		{
			return;
		}

		source.Cancel();
		_runnables.Clear();
	}

	public void TogglePause()
	{
		if (Gameboy != null)
		{
			Gameboy.Pause = !Gameboy.Pause;
		}
	}

	private Gameboy CreateGameboy(Cartridge rom)
	{
		if (Options.Headless)
		{
			return new Gameboy(
				Options,
				rom,
				new NullDisplay(),
				new NullController(),
				new NullSoundOutput(),
				new NullSerialEndpoint());
		}

		// TODO: Make real things work
		// throw new NotImplementedException("Not implemented not headless.");
		// sound = new AudioSystemSoundOutput();
		// display = new SwingDisplay(SCALE);
		// controller = new SwingController(properties);
		// gameboy = new Gameboy(options, rom, display, controller, sound, serialEndpoint, console);

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return new Gameboy(
				Options,
				rom,
				Display,
				Controller,
				new WinSound(),
				ISerialEndpoint);
		}

		return new Gameboy(
			Options,
			rom,
			Display,
			Controller,
			new NullSoundOutput(),
			ISerialEndpoint);
	}
}
