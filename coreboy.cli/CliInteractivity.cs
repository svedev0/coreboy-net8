#nullable disable

using coreboy.controller;
using Button = coreboy.controller.Button;

namespace coreboy.cli;

public class CliInteractivity : IController
{
	private readonly Dictionary<ConsoleKey, Button> controls;
	private IButtonListener listener;

	public CliInteractivity()
	{
		Console.Clear();
		Console.SetCursorPosition(0, 0);
		Console.CursorVisible = false;

		controls = new()
		{
			{ ConsoleKey.LeftArrow,  Button.Left },
			{ ConsoleKey.RightArrow, Button.Right },
			{ ConsoleKey.UpArrow,    Button.Up },
			{ ConsoleKey.DownArrow,  Button.Down },
			{ ConsoleKey.Z,          Button.A },
			{ ConsoleKey.X,          Button.B },
			{ ConsoleKey.Enter,      Button.Start },
			{ ConsoleKey.Backspace,  Button.Select }
		};
	}

	public void SetButtonListener(IButtonListener buttonListener)
	{
		listener = buttonListener;
	}

	public void ProcessInput()
	{
		Button lastButton = null;
		var input = Console.ReadKey(true);

		while (input.Key != ConsoleKey.Escape)
		{
			if (controls.TryGetValue(input.Key, out var button))
			{
				if (lastButton != button && lastButton != null)
				{
					listener?.OnButtonRelease(lastButton);
				}

				listener?.OnButtonPress(button);

				Button snapshot = button;

				Task.Run(() =>
				{
					Task.Delay(500).Wait();
					listener?.OnButtonRelease(snapshot);
				});

				lastButton = button;
			}

			input = Console.ReadKey(true);
		}
	}

	public void UpdateDisplay(object sender, byte[] frameBytes)
	{
		string frame = AsciiGenerator.GenerateFrame(frameBytes);
		Console.SetCursorPosition(0, 0);
		Console.Write(frame);
	}
}
