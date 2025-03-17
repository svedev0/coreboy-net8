using coreboy.controller;
using Button = coreboy.controller.Button;

namespace coreboy.cli;

public class CliInteractivity : IController
{
	private readonly Dictionary<ConsoleKey, Button> controls;
	private IButtonListener? listener;

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
		Button? lastButton = null;
		ConsoleKeyInfo input = Console.ReadKey(true);

		while (input.Key != ConsoleKey.Escape)
		{
			if (controls.TryGetValue(input.Key, out Button? button))
			{
				if (lastButton != button && lastButton != null)
				{
					listener?.OnButtonRelease(lastButton);
				}

				listener?.OnButtonPress(button);
				Button buttonSnapshot = button;

				Task.Run(() =>
				{
					Task.Delay(500).Wait();
					listener?.OnButtonRelease(buttonSnapshot);
				});

				lastButton = button;
			}

			input = Console.ReadKey(true);
		}
	}

	// Ignore the LSP/IntelliSense. This should not be marked as static
	public void UpdateDisplay(object _, byte[] frameBytes)
	{
		string frame = AsciiGenerator.GenerateFrame(frameBytes);
		Console.SetCursorPosition(0, 0);
		Console.Write(frame);
	}
}
