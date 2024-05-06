using coreboy.cpu;

namespace coreboy.memory;

public class Dma(
	IAddressSpace addressSpace,
	IAddressSpace oam,
	SpeedMode speedMode) : IAddressSpace
{
	private readonly DmaAddressSpace _addressSpace = new(addressSpace);
	private readonly IAddressSpace _oam = oam;
	private readonly SpeedMode _speedMode = speedMode;

	private bool transferInProgress;
	private bool restarted;
	private int from;
	private int ticks;
	private int regValue = 0xff;

	public bool Accepts(int address)
	{
		return address == 0xff46;
	}

	public void Tick()
	{
		if (!transferInProgress)
		{
			return;
		}

		if (++ticks < 648 / _speedMode.GetSpeedMode())
		{
			return;
		}

		transferInProgress = false;
		restarted = false;
		ticks = 0;
		
		for (int i = 0; i < 0xa0; i++)
		{
			_oam.SetByte(0xfe00 + i, _addressSpace.GetByte(from + i));
		}
	}

	public void SetByte(int address, int value)
	{
		from = value * 0x100;
		restarted = IsOamBlocked();
		ticks = 0;
		transferInProgress = true;
		regValue = value;
	}

	public int GetByte(int address)
	{
		return regValue;
	}

	public bool IsOamBlocked()
	{
		return restarted || (transferInProgress && ticks >= 5);
	}
}
