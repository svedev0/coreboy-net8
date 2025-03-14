using System.Text;
using coreboy.memory.cart.battery;
using coreboy.memory.cart.type;

namespace coreboy.memory.cart;

public class Cartridge : IAddressSpace
{
	public enum GameboyTypeFlag
	{
		UNIVERSAL = 0x80,
		GBC = 0xc0,
		NON_GBC = 0
	}

	public bool Gbc { get; }
	public string Title { get; }

	private readonly IAddressSpace _addressSpace;

	private int dmgBootstrap;

	public Cartridge(GameboyOptions options)
	{
		var file = options.RomFile;
		int[] rom = LoadFile(file);
		var type = CartridgeTypeExtensions.GetById(rom[0x0147]);

		Title = GetTitle(rom);

		var gameboyType = GetFlag(rom[0x0143]);
		int romBanks = GetRomBanks(rom[0x0148]);
		int ramBanks = GetRamBanks(rom[0x0149]);

		if (ramBanks == 0 && type.IsRam())
		{
			ramBanks = 1;
		}

		IBattery battery = new NullBattery();

		if (type.IsBattery() && options.IsSupportBatterySaves())
		{
			battery = new FileBattery(file?.Name ?? string.Empty);
		}

		if (type.IsMbc1())
		{
			_addressSpace = new Mbc1(rom, battery, romBanks, ramBanks);
		}
		else if (type.IsMbc2())
		{
			_addressSpace = new Mbc2(rom, battery);
		}
		else if (type.IsMbc3())
		{
			_addressSpace = new Mbc3(rom, battery, ramBanks);
		}
		else if (type.IsMbc5())
		{
			_addressSpace = new Mbc5(rom, battery, ramBanks);
		}
		else
		{
			_addressSpace = new Rom(rom, type, romBanks, ramBanks);
		}

		dmgBootstrap = options.UseBootstrap ? 0 : 1;

		if (options.ForceGbc)
		{
			Gbc = true;
			return;
		}

		Gbc = gameboyType switch
		{
			GameboyTypeFlag.NON_GBC => false,
			GameboyTypeFlag.GBC => true,
			_ => !options.ForceDmg // UNIVERSAL
		};
	}

	private static string GetTitle(int[] rom)
	{
		StringBuilder sb = new();

		for (int i = 0x0134; i < 0x0143; i++)
		{
			char c = (char)rom[i];
			if (c == 0)
			{
				break;
			}

			sb.Append(c);
		}

		return sb.ToString();
	}

	public bool Accepts(int address)
	{
		return _addressSpace.Accepts(address) || address == 0xff50;
	}

	public void SetByte(int address, int value)
	{
		if (address == 0xff50)
		{
			dmgBootstrap = 1;
		}
		else
		{
			_addressSpace.SetByte(address, value);
		}
	}

	public int GetByte(int address)
	{
		return dmgBootstrap switch
		{
			0 when !Gbc && address >= 0x0000 && address < 0x0100 =>
				BootRom.GameboyClassic[address],
			0 when Gbc && address >= 0x000 && address < 0x0100 =>
				BootRom.GameboyColor[address],
			0 when Gbc && address >= 0x200 && address < 0x0900 =>
				BootRom.GameboyColor[address - 0x0100],
			_ => address == 0xff50 ? 0xff : _addressSpace.GetByte(address),
		};
	}

	private static int[] LoadFile(FileSystemInfo? file)
	{
		if (file == null)
		{
			throw new Exception("Cartridge file is null");
		}

		return [.. File.ReadAllBytes(file.FullName).Select(x => (int)x)];
	}

	private static int GetRomBanks(int id)
	{
		return id switch
		{
			0 => 2,
			1 => 4,
			2 => 8,
			3 => 16,
			4 => 32,
			5 => 64,
			6 => 128,
			7 => 256,
			0x52 => 72,
			0x53 => 80,
			0x54 => 96,
			_ => throw new ArgumentException("Unsupported ROM size")
		};
	}

	private static int GetRamBanks(int id)
	{
		return id switch
		{
			0 => 0,
			1 => 1,
			2 => 1,
			3 => 4,
			4 => 16,
			_ => throw new ArgumentException("Unsupported RAM size")
		};
	}

	public static GameboyTypeFlag GetFlag(int value)
	{
		return value switch
		{
			0x80 => GameboyTypeFlag.UNIVERSAL,
			0xc0 => GameboyTypeFlag.GBC,
			_ => GameboyTypeFlag.NON_GBC
		};
	}
}
