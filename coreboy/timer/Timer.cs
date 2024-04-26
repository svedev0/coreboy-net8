using coreboy.cpu;

namespace coreboy.timer;

public class Timer(InterruptManager intManager, SpeedMode speedMode) : IAddressSpace
{
	private readonly SpeedMode _speedMode = speedMode;
	private readonly InterruptManager _intManager = intManager;
	private static readonly int[] FreqToBit = [9, 3, 5, 7];

	private int div;
	private int tac;
	private int tma;
	private int tima;
	private bool previousBit;
	private bool overflow;
	private int ticksSinceOverflow;

	public void Tick()
	{
		UpdateDiv((div + 1) & 0xffff);

		if (!overflow)
		{
			return;
		}

		ticksSinceOverflow++;

		if (ticksSinceOverflow == 4)
		{
			_intManager.RequestInterrupt(InterruptManager.InterruptType.Timer);
		}

		if (ticksSinceOverflow == 5)
		{
			tima = tma;
		}

		if (ticksSinceOverflow == 6)
		{
			tima = tma;
			overflow = false;
			ticksSinceOverflow = 0;
		}
	}

	private void IncTima()
	{
		tima++;
		tima %= 0x100;

		if (tima == 0)
		{
			overflow = true;
			ticksSinceOverflow = 0;
		}
	}

	private void UpdateDiv(int newDiv)
	{
		div = newDiv;

		int bitPos = FreqToBit[tac & 0b11];
		bitPos <<= _speedMode.GetSpeedMode() - 1;

		bool bit = (div & (1 << bitPos)) != 0;
		bit &= (tac & (1 << 2)) != 0;

		if (!bit && previousBit)
		{
			IncTima();
		}

		previousBit = bit;
	}

	public bool Accepts(int address)
	{
		return address >= 0xff04 && address <= 0xff07;
	}

	public void SetByte(int address, int value)
	{
		switch (address)
		{
			case 0xff04:
				UpdateDiv(0);
				break;

			case 0xff05:
				if (ticksSinceOverflow < 5)
				{
					tima = value;
					overflow = false;
					ticksSinceOverflow = 0;
				}
				break;

			case 0xff06:
				tma = value;
				break;

			case 0xff07:
				tac = value;
				break;
		}
	}

	public int GetByte(int address)
	{
		return address switch
		{
			0xff04 => div >> 8,
			0xff05 => tima,
			0xff06 => tma,
			0xff07 => tac | 0b11111000,
			_ => throw new ArgumentException("Invalid memory address")
		};
	}
}
