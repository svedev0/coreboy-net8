using coreboy.memory.cart.battery;

namespace coreboy.memory.cart.type;

public class Mbc1 : IAddressSpace
{
	private static readonly int[] NintendoLogo = [
		0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B,
		0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D,
		0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E,
		0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99,
		0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC,
		0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E
	];

	private readonly int _romBanks;
	private readonly int _ramBanks;
	private readonly int[] _cartridge;
	private readonly int[] _ram;
	private readonly IBattery _battery;
	private readonly bool _multicart;

	private int selectedRamBank;
	private int selectedRomBank = 1;
	private int memoryModel;
	private bool ramWriteEnabled;
	private int cachedRomBankFor0x0000 = -1;
	private int cachedRomBankFor0x4000 = -1;

	public Mbc1(
		int[] cartridge,
		CartridgeType type,
		IBattery battery,
		int romBanks,
		int ramBanks)
	{
		_multicart = romBanks == 64 && IsMulticart(cartridge);
		_cartridge = cartridge;
		_ramBanks = ramBanks;
		_romBanks = romBanks;
		_ram = new int[0x2000 * _ramBanks];

		for (int i = 0; i < _ram.Length; i++)
		{
			_ram[i] = 0xff;
		}

		_battery = battery;
		battery.LoadRam(_ram);
	}

	public bool Accepts(int address)
	{
		return (address >= 0x0000 && address < 0x8000) ||
			(address >= 0xa000 && address < 0xc000);
	}

	public void SetByte(int address, int value)
	{
		if (address >= 0x0000 && address < 0x2000)
		{
			ramWriteEnabled = (value & 0b1111) == 0b1010;
			if (!ramWriteEnabled)
			{
				_battery.SaveRam(_ram);
			}
		}
		else if (address >= 0x2000 && address < 0x4000)
		{
			int bank = selectedRomBank & 0b01100000;
			bank |= value & 0b00011111;
			SelectRomBank(bank);
			cachedRomBankFor0x0000 = cachedRomBankFor0x4000 = -1;
		}
		else if (address >= 0x4000 && address < 0x6000 && memoryModel == 0)
		{
			int bank = selectedRomBank & 0b00011111;
			bank |= (value & 0b11) << 5;
			SelectRomBank(bank);
			cachedRomBankFor0x0000 = cachedRomBankFor0x4000 = -1;
		}
		else if (address >= 0x4000 && address < 0x6000 && memoryModel == 1)
		{
			int bank = value & 0b11;
			selectedRamBank = bank;
			cachedRomBankFor0x0000 = cachedRomBankFor0x4000 = -1;
		}
		else if (address >= 0x6000 && address < 0x8000)
		{
			memoryModel = value & 1;
			cachedRomBankFor0x0000 = cachedRomBankFor0x4000 = -1;
		}
		else if (address >= 0xa000 && address < 0xc000 && ramWriteEnabled)
		{
			int ramAddress = GetRamAddress(address);
			if (ramAddress < _ram.Length)
			{
				_ram[ramAddress] = value;
			}
		}
	}

	private void SelectRomBank(int bank)
	{
		selectedRomBank = bank;
	}

	public int GetByte(int address)
	{
		if (address >= 0x0000 && address < 0x4000)
		{
			return GetRomByte(GetRomBankFor0x0000(), address);
		}

		if (address >= 0x4000 && address < 0x8000)
		{
			return GetRomByte(GetRomBankFor0x4000(), address - 0x4000);
		}

		if (address >= 0xa000 && address < 0xc000)
		{
			if (ramWriteEnabled)
			{
				int ramAddress = GetRamAddress(address);
				if (ramAddress < _ram.Length)
				{
					return _ram[ramAddress];
				}

				return 0xff;
			}

			return 0xff;
		}

		throw new ArgumentException("Invalid address");
	}

	private int GetRomBankFor0x0000()
	{
		if (cachedRomBankFor0x0000 == -1)
		{
			if (memoryModel == 0)
			{
				cachedRomBankFor0x0000 = 0;
			}
			else
			{
				int bank = selectedRamBank << 5;

				if (_multicart)
				{
					bank >>= 1;
				}

				bank %= _romBanks;
				cachedRomBankFor0x0000 = bank;
			}
		}

		return cachedRomBankFor0x0000;
	}

	private int GetRomBankFor0x4000()
	{
		if (cachedRomBankFor0x4000 == -1)
		{
			int bank = selectedRomBank;
			if (bank % 0x20 == 0)
			{
				bank++;
			}

			if (memoryModel == 1)
			{
				bank &= 0b00011111;
				bank |= selectedRamBank << 5;
			}

			if (_multicart)
			{
				bank = ((bank >> 1) & 0x30) | (bank & 0x0f);
			}

			bank %= _romBanks;
			cachedRomBankFor0x4000 = bank;
		}

		return cachedRomBankFor0x4000;
	}

	private int GetRomByte(int bank, int address)
	{
		int cartOffset = bank * 0x4000 + address;
		if (cartOffset < _cartridge.Length)
		{
			return _cartridge[cartOffset];
		}
		else
		{
			return 0xff;
		}
	}

	private int GetRamAddress(int address)
	{
		if (memoryModel == 0)
		{
			return address - 0xa000;
		}
		else
		{
			return selectedRamBank % _ramBanks * 0x2000 + (address - 0xa000);
		}
	}

	private static bool IsMulticart(int[] rom)
	{
		int logoCount = 0;

		for (int i = 0; i < rom.Length; i += 0x4000)
		{
			bool logoMatches = true;

			for (int j = 0; j < NintendoLogo.Length; j++)
			{
				if (rom[i + 0x104 + j] != NintendoLogo[j])
				{
					logoMatches = false;
					break;
				}
			}

			if (logoMatches)
			{
				logoCount++;
			}
		}

		return logoCount > 1;
	}
}
