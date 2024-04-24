using CommandLine;

namespace coreboy;

public class GameboyOptions
{
    [Option('r', "rom")]
    public string Rom { get; set; } = string.Empty;

    [Option('d', "force-dmg")]
    public bool ForceDmg { get; set; }

    [Option('c', "force-gbc")]
    public bool ForceGbc { get; set; }

    [Option('b', "use-bootstrap")]
    public bool UseBootstrap { get; set; }

    [Option("disable-battery-saves")]
    public bool DisableBatterySaves { get; set; }

    [Option("debug")]
    public bool Debug { get; set; }

    [Option("headless")]
    public bool Headless { get; set; }

    [Option("interactive")]
    public bool Interactive { get; set; }

    public FileInfo? RomFile
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Rom))
            {
                return new FileInfo(Rom);
            }
            return null;
        }
    }

    public bool ShowUi => !Headless;

    public bool IsSupportBatterySaves() => !DisableBatterySaves;

    public bool RomSpecified => !string.IsNullOrWhiteSpace(Rom);

    public GameboyOptions()
    {
    }

    public GameboyOptions(FileInfo romFile) : this(romFile, [], [])
    {
    }

    public GameboyOptions(
        FileInfo romFile, List<string> longParams, List<string> shortParams)
    {
        Rom = romFile.FullName;
        ForceDmg = longParams.Contains("force-dmg") || shortParams.Contains("d");
        ForceGbc = longParams.Contains("force-cgb") || shortParams.Contains("c");

        UseBootstrap = longParams.Contains("use-bootstrap") || shortParams.Contains("b");
        DisableBatterySaves = longParams.Contains("disable-battery-saves");
        Debug = longParams.Contains("debug");
        Headless = longParams.Contains("headless");

        Verify();
    }

    public void Verify()
    {
        if (ForceDmg && ForceGbc)
        {
            throw new ArgumentException(
                "force-dmg and force-cgb options cannot be used simultaneously");
        }
    }

    public const string Usage =
        """
        Usage:
        .\coreboy.cli.exe 'path\to\rom.gb'

        Available options:
            -d, --force-dmg              Emulate DMG hardware
            -c, --force-gbc              Emulate GBC hardware
            -b, --use-bootstrap          Start with GB bootstrap
                --disable-battery-saves  Disable battery saves
                --debug                  Enable debug console
                --headless               Start in headless mode
                --interactive            Play in the terminal
        """;

    public static GameboyOptions Parse(string[] args)
    {
        Parser parser = new(cfg =>
        {
            cfg.AutoHelp = true;
            cfg.HelpWriter = Console.Out;
        });

        var result = parser.ParseArguments<GameboyOptions>(args)
            .WithParsed(o => { o.Verify(); });

        if (result is not Parsed<GameboyOptions> parsed)
        {
            throw new ArgumentException("Invalid argument(s)");
        }

        if (args.Length != 1)
        {
            return parsed.Value;
        }

        string extension = Path.GetExtension(args[0]);
        
        if (extension == ".gb" || extension == ".gbc")
        {
            parsed.Value.Rom = args[0];
        }

        return parsed.Value;
    }
}
