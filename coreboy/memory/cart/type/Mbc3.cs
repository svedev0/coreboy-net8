using coreboy.memory.cart.battery;
using coreboy.memory.cart.rtc;

namespace coreboy.memory.cart.type;

public class Mbc3 : IAddressSpace
{
	private readonly int[] _cartridge;
	private readonly int[] _ram;
	private readonly RealTimeClock _clock;
	private readonly IBattery _battery;

	private int selectedRamBank;
	private int selectedRomBank = 1;
	private bool ramWriteEnabled;
	private int latchClockReg = 0xff;
	private bool clockLatched;

	public Mbc3(int[] cartridge, IBattery battery, int ramBanks)
	{
		_cartridge = cartridge;
		_ram = new int[0x2000 * Math.Max(ramBanks, 1)];

		for (int i = 0; i < _ram.Length; i++)
		{
			_ram[i] = 0xff;
		}

		_clock = new RealTimeClock(Clock.SystemClock);
		_battery = battery;

		long[] clockData = new long[12];

		battery.LoadRamWithClock(_ram, clockData);
		_clock.Deserialize(clockData);
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
			ramWriteEnabled = (value & 0b1010) != 0;
			if (!ramWriteEnabled)
			{
				_battery.SaveRamWithClock(_ram, _clock.Serialize());
			}
		}
		else if (address >= 0x2000 && address < 0x4000)
		{
			int bank = value & 0b01111111;
			SelectRomBank(bank);
		}
		else if (address >= 0x4000 && address < 0x6000)
		{
			selectedRamBank = value;
		}
		else if (address >= 0x6000 && address < 0x8000)
		{
			if (value == 0x01 && latchClockReg == 0x00)
			{
				if (clockLatched)
				{
					_clock.Unlatch();
					clockLatched = false;
				}
				else
				{
					_clock.Latch();
					clockLatched = true;
				}
			}

			latchClockReg = value;
		}
		else if (address >= 0xa000 && address < 0xc000 && ramWriteEnabled && selectedRamBank < 4)
		{
			int ramAddress = GetRamAddress(address);
			if (ramAddress < _ram.Length)
			{
				_ram[ramAddress] = value;
			}
		}
		else if (address >= 0xa000 && address < 0xc000 && ramWriteEnabled && selectedRamBank >= 4)
		{
			SetTimer(value);
		}
	}

	private void SelectRomBank(int bank)
	{
		if (bank == 0)
		{
			bank = 1;
		}

		selectedRomBank = bank;
	}

	public int GetByte(int address)
	{
		if (address >= 0x0000 && address < 0x4000)
		{
			return GetRomByte(0, address);
		}
		else if (address >= 0x4000 && address < 0x8000)
		{
			return GetRomByte(selectedRomBank, address - 0x4000);
		}
		else if (address >= 0xa000 && address < 0xc000 && selectedRamBank < 4)
		{
			int ramAddress = GetRamAddress(address);
			if (ramAddress < _ram.Length)
			{
				return _ram[ramAddress];
			}
			else
			{
				return 0xff;
			}
		}
		else if (address >= 0xa000 && address < 0xc000 && selectedRamBank >= 4)
		{
			return GetTimer();
		}
		else
		{
			throw new ArgumentException("Invalid address");
		}
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
		return selectedRamBank * 0x2000 + (address - 0xa000);
	}

	private int GetTimer()
	{
		switch (selectedRamBank)
		{
			case 0x08:
				return _clock.GetSeconds();

			case 0x09:
				return _clock.GetMinutes();

			case 0x0a:
				return _clock.GetHours();

			case 0x0b:
				return _clock.GetDayCounter() & 0xff;

			case 0x0c:
				int result = (_clock.GetDayCounter() & 0x100) >> 8;
				result |= _clock.IsHalt() ? (1 << 6) : 0;
				result |= _clock.IsCounterOverflow() ? (1 << 7) : 0;
				return result;
		}

		return 0xff;
	}

	private void SetTimer(int value)
	{
		int dayCounter = _clock.GetDayCounter();

		switch (selectedRamBank)
		{
			case 0x08:
				_clock.SetSeconds(value);
				break;

			case 0x09:
				_clock.SetMinutes(value);
				break;

			case 0x0a:
				_clock.SetHours(value);
				break;

			case 0x0b:
				_clock.SetDayCounter((dayCounter & 0x100) | (value & 0xff));
				break;

			case 0x0c:
				_clock.SetDayCounter((dayCounter & 0xff) | ((value & 1) << 8));
				_clock.SetHalt((value & (1 << 6)) != 0);

				if ((value & (1 << 7)) == 0)
				{
					_clock.ClearCounterOverflow();
				}

				break;
		}
	}
}
