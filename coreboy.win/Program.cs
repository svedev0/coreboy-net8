namespace coreboy.win;

public class Program
{
	[STAThread]
	public static void Main(string[] _)
	{
		Application.SetCompatibleTextRenderingDefault(false);
		Application.SetHighDpiMode(HighDpiMode.SystemAware);
		Application.EnableVisualStyles();

		Form emuSurface = new EmulatorSurface
		{
			Text = @"CoreBoy",
			ClientSize = new Size(680, 600),
			AutoScaleMode = AutoScaleMode.Dpi,
			AutoScaleDimensions = new SizeF(144F, 144F)
		};
		Application.Run(emuSurface);
	}
}
