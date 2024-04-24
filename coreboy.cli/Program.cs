using coreboy.controller;
using coreboy.gui;
using Button = coreboy.controller.Button;

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
            Console.WriteLine(GameboyOptions.Usage);
            Environment.Exit(1);
        }

        if (options.Interactive)
        {
            CommandLineInteractivity ui = new();
            emulator.Controller = ui;
            emulator.Display.OnFrameProduced += ui.UpdateDisplay;
            emulator.Run(cancellation.Token);
            ui.ProcessInput();
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

public class CommandLineInteractivity : IController
{
    private IButtonListener? _listener;
    private readonly Dictionary<ConsoleKey, Button> _controls;

    public CommandLineInteractivity()
    {
        Console.Clear();
        Console.SetCursorPosition(0, 0);
        Console.WindowHeight = 92;

        _controls = new()
        {
            {ConsoleKey.LeftArrow, Button.Left},
            {ConsoleKey.RightArrow, Button.Right},
            {ConsoleKey.UpArrow, Button.Up},
            {ConsoleKey.DownArrow, Button.Down},
            {ConsoleKey.Z, Button.A},
            {ConsoleKey.X, Button.B},
            {ConsoleKey.Enter, Button.Start},
            {ConsoleKey.Backspace, Button.Select}
        };
    }

    public void SetButtonListener(IButtonListener listener) => _listener = listener;

    public void ProcessInput()
    {
        Button? lastButton = null;
        var input = Console.ReadKey(true);
        
        while (input.Key != ConsoleKey.Escape)
        {
            if (!_controls.TryGetValue(input.Key, out var button))
            {
                input = Console.ReadKey(true);
                continue;
            }

            if (lastButton != null && lastButton != button)
            {
                _listener?.OnButtonRelease(lastButton);
            }

            _listener?.OnButtonPress(button);

            // TODO: Clean up this thread start
            Button snapshot = button;
            new Thread(() =>
            {
                Task.Delay(500).Wait();
                _listener?.OnButtonRelease(snapshot);
            }).Start();

            lastButton = button;

            input = Console.ReadKey(true);
        }
    }

    public void UpdateDisplay(object sender, byte[] frameData)
    {
        string frame = AsciiGenerator.GenerateFrame(frameData);
        Console.SetCursorPosition(0, 0);
        Console.WriteLine(frame);
    }
}
