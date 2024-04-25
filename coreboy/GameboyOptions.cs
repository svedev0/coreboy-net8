using CommandLine;

namespace coreboy;

public class GameboyOptions
{
	public FileInfo? RomFile
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Rom))
			{
				return null;
			}
			return new FileInfo(Rom);
		}
	}

	[Option('r', "rom")]
	public string Rom { get; set; } = string.Empty;

	[Option('d', "force-dmg")]
	public bool ForceDmg { get; set; }

	[Option('c', "force-gbc")]
	public bool ForceGbc { get; set; }

	[Option('b', "use-bootstrap")]
	public bool UseBootstrap { get; set; }

	[Option('i', "interactive")]
	public bool Interactive { get; set; }

	[Option("headless")]
	public bool Headless { get; set; }

	[Option("disable-battery-saves")]
	public bool DisableBatterySaves { get; set; }

	public bool IsSupportBatterySaves() => !DisableBatterySaves;

	public bool RomSpecified => !string.IsNullOrWhiteSpace(Rom);

	public const string UsageInfo =
		"""
		Usage:
		coreboy.cli.exe path\to\rom.gb --option
		
		Available options:
		    -d, --force-dmg              Use DMG mode
		    -c, --force-gbc              Use GBC mode
		    -b, --use-bootstrap          Start with the GB bootstrap
		    -i, --interactive            Play in the terminal
		        --headless               Start in headless mode
		        --disable-battery-saves  Disable battery saves
		""";

	public GameboyOptions()
	{
	}

	public void Verify()
	{
		if (ForceDmg && ForceGbc)
		{
			throw new ArgumentException(
				"force-dmg and force-gbc options are can't be used together");
		}
	}

	public static GameboyOptions Parse(string[] args)
	{
		var result = Parser.Default.ParseArguments<GameboyOptions>(args)
			.WithParsed(opts => { opts.Verify(); });

		if (result is not Parsed<GameboyOptions> parsed)
		{
			throw new Exception("Failed to parse options");
		}

		if (args.Length == 1)
		{
			if (Path.GetExtension(args[0]) is ".gb" or ".gbc")
			{
				parsed.Value.Rom = args[0];
			}
		}

		return parsed.Value;
	}
}
