namespace coreboy.cpu;

public class SpeedMode : IAddressSpace
{
	private bool currentSpeed;
	private bool prepareSpeedSwitch;

	public bool Accepts(int address)
	{
		return address == 0xff4d;
	}

	public void SetByte(int address, int value)
	{
		prepareSpeedSwitch = (value & 0x01) != 0;
	}

	public int GetByte(int address)
	{
		if (currentSpeed)
		{
			return (1 << 7) | (prepareSpeedSwitch ? (1 << 0) : 0) | 0b01111110;
		}

		return (0) | (prepareSpeedSwitch ? (1 << 0) : 0) | 0b01111110;
	}

	public bool OnStop()
	{
		if (!prepareSpeedSwitch)
		{
			return false;
		}

		currentSpeed = !currentSpeed;
		prepareSpeedSwitch = false;
		return true;
	}

	public int GetSpeedMode()
	{
		return currentSpeed ? 2 : 1;
	}
}
