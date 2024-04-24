namespace coreboy.win;

public class Program
{
    [STAThread]
    public static void Main(string[] _)
    {
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();

        Application.Run(new EmulatorSurface());
    }
}
