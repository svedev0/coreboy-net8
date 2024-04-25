using coreboy.gui;

namespace coreboy.cli;

public class Program
{
	static void Main(string[] args)
	{
		CancellationTokenSource cancellation = new();
		var options = GameboyOptions.Parse(args);
		Emulator emulator = new(options);

		if (!options.RomSpecified)
		{
			Console.WriteLine(GameboyOptions.UsageInfo);
			return;
		}

		if (options.Interactive)
		{
			CliInteractivity tui = new();
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

		cancellation.Cancel();
	}
}
