using CommandLine;

namespace coreboy;

public class GameboyOptions
{
	public FileInfo? RomFile => string.IsNullOrWhiteSpace(Rom) ? null : new FileInfo(Rom);

	[Option('r', "rom", Required = false, HelpText = "Rom file.")]
	public string Rom { get; set; } = string.Empty;

	[Option('d', "force-dmg", Required = false, HelpText = "ForceDmg.")]
	public bool ForceDmg { get; set; }

	[Option('c', "force-cgb", Required = false, HelpText = "ForceCgb.")]
	public bool ForceCgb { get; set; }

	[Option('b', "use-bootstrap", Required = false, HelpText = "UseBootstrap.")]
	public bool UseBootstrap { get; set; }

	[Option("disable-battery-saves", Required = false, HelpText = "disable-battery-saves.")]
	public bool DisableBatterySaves { get; set; }

	[Option("debug", Required = false, HelpText = "Debug.")]
	public bool Debug { get; set; }

	[Option("headless", Required = false, HelpText = "headless.")]
	public bool Headless { get; set; }

	[Option("interactive", Required = false, HelpText = "Play on the console!")]
	public bool Interactive { get; set; }

	public bool ShowUi => !Headless;

	public bool IsSupportBatterySaves() => !DisableBatterySaves;

	public bool RomSpecified => !string.IsNullOrWhiteSpace(Rom);

	public GameboyOptions()
	{
	}

	public GameboyOptions(FileInfo romFile) : this(romFile, new string[0], new string[0])
	{
	}

	public GameboyOptions(FileInfo romFile, ICollection<string> longParameters, ICollection<string> shortParams)
	{
		Rom = romFile.FullName;
		ForceDmg = longParameters.Contains("force-dmg") || shortParams.Contains("d");
		ForceCgb = longParameters.Contains("force-cgb") || shortParams.Contains("c");


		UseBootstrap = longParameters.Contains("use-bootstrap") || shortParams.Contains("b");
		DisableBatterySaves = longParameters.Contains("disable-battery-saves") || shortParams.Contains("db");
		Debug = longParameters.Contains("debug");
		Headless = longParameters.Contains("headless");

		Verify();
	}

	public void Verify()
	{
		if (ForceDmg && ForceCgb)
		{
			throw new ArgumentException("force-dmg and force-cgb options are can't be used together");
		}
	}

	public const string UsageInfo =
		"""
		Usage:
		coreboy.cli.exe path\to\rom.gb --option
		
		Available options:
		    -d, --force-dmg                Use DMG mode
		    -c, --force-cgb                Use GBC mode
		    -b, --use-bootstrap            Start with the GB bootstrap
		        --disable-battery-saves    Disable battery saves
		        --debug                    Enable debug console
		        --headless                 Start in headless mode
		        --interactive              Play in the terminal
		""";

	public static GameboyOptions Parse(string[] args)
	{
		var parser = new Parser(cfg =>
		{
			cfg.AutoHelp = true;
			cfg.HelpWriter = Console.Out;
		});

		var result = parser.ParseArguments<GameboyOptions>(args)
			.WithParsed(o => { o.Verify(); });


		if (result is Parsed<GameboyOptions> parsed)
		{
			if (args.Length == 1 && args[0].Contains(".gb"))
			{
				parsed.Value.Rom = args[0];
			}

			return parsed.Value;
		}
		else
		{
			throw new Exception("Failed to parse options");
		}
	}
}
