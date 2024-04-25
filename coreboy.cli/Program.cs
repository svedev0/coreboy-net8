using coreboy.gui;

namespace coreboy.cli;

public class Program
{
	static void Main(string[] args)
	{
		CancellationTokenSource cancellation = new();
		var arguments = GameboyOptions.Parse(args);
		Emulator emulator = new(arguments);

		if (!arguments.RomSpecified)
		{
			GameboyOptions.PrintUsage(Console.Out);
			Console.Out.Flush();
			return;
		}

		CliInteractivity tui = new();
		emulator.Controller = tui;
		emulator.Display.OnFrameProduced += tui.UpdateDisplay;
		emulator.Run(cancellation.Token);
		tui.ProcessInput();

		// TODO: Figure out why arguments.Interactive is never resolved as true
		/* if (arguments.Interactive)
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
		} */

		cancellation.Cancel();
	}
}
