using coreboy.memory.cart.battery;

namespace coreboy.memory.cart.type;

public class Mbc2 : IAddressSpace
{
	private readonly int[] _cartridge;
	private readonly int[] _ram;
	private readonly IBattery _battery;

	private int selectedRomBank = 1;
	private bool ramWriteEnabled;

	public Mbc2(int[] cartridge, CartridgeType type, IBattery battery, int romBanks)
	{
		_cartridge = cartridge;
		_ram = new int[0x0200];

		for (int i = 0; i < _ram.Length; i++)
		{
			_ram[i] = 0xff;
		}

		_battery = battery;
		battery.LoadRam(_ram);
	}

	public bool Accepts(int address)
	{
		return address >= 0x0000 &&
			address < 0x8000 || address >= 0xa000 &&
			address < 0xc000;
	}

	public void SetByte(int address, int value)
	{
		if (address >= 0x0000 && address < 0x2000)
		{
			if ((address & 0x0100) == 0)
			{
				ramWriteEnabled = (value & 0b1010) != 0;
				if (!ramWriteEnabled)
				{
					_battery.SaveRam(_ram);
				}
			}
		}
		else if (address >= 0x2000 && address < 0x4000)
		{
			if ((address & 0x0100) != 0)
			{
				selectedRomBank = value & 0b00001111;
			}
		}
		else if (address >= 0xa000 && address < 0xc000 && ramWriteEnabled)
		{
			int ramAddress = GetRamAddress(address);
			if (ramAddress < _ram.Length)
			{
				_ram[ramAddress] = value & 0x0f;
			}
		}
	}

	public int GetByte(int address)
	{
		if (address >= 0x0000 && address < 0x4000)
		{
			return GetRomByte(0, address);
		}

		if (address >= 0x4000 && address < 0x8000)
		{
			return GetRomByte(selectedRomBank, address - 0x4000);
		}

		if (address >= 0xa000 && address < 0xb000)
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

	private int GetRomByte(int bank, int address)
	{
		int cartOffset = bank * 0x4000 + address;
		if (cartOffset < _cartridge.Length)
		{
			return _cartridge[cartOffset];
		}

		return 0xff;
	}

	private static int GetRamAddress(int address)
	{
		return address - 0xa000;
	}
}
