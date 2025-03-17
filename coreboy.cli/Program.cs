using coreboy.gui;

namespace coreboy.cli;

public class Program
{
	static void Main(string[] args)
	{
		CancellationTokenSource cancellation = new();
		GameboyOptions options = GameboyOptions.Parse(args);
		Emulator emulator = new(options);
		CliInteractivity? tui = null;

		if (!options.RomSpecified || !Path.Exists(options.Rom))
		{
			Console.WriteLine("Invalid ROM file path");
			Console.WriteLine(GameboyOptions.UsageInfo);
			return;
		}

		if (options.Interactive)
		{
			tui = new CliInteractivity();
			emulator.Controller = tui;
			emulator.Display.OnFrameProduced += tui.UpdateDisplay;
			emulator.Run(cancellation.Token);
			tui.ProcessInput();
		}
		else
		{
			emulator.Run(cancellation.Token);
			Console.WriteLine("Running in headless mode");
			Console.WriteLine("Press any key to exit...");
			Console.ReadKey(true);
		}

		// On exit
		if (tui != null)
		{
			emulator.Display.OnFrameProduced -= tui.UpdateDisplay;
		}

		cancellation.Cancel();
	}
}
