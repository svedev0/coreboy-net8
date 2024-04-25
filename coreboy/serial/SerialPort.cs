using coreboy.cpu;

namespace coreboy.serial;

public class SerialPort(
	InterruptManager interruptManager,
	ISerialEndpoint serialEndpoint,
	SpeedMode speedMode) : IAddressSpace
{
	private readonly ISerialEndpoint _serialEndpoint = serialEndpoint;
	private readonly InterruptManager _intManager = interruptManager;
	private readonly SpeedMode _speedMode = speedMode;
	private int _sb;
	private int _sc;
	private bool _transferInProgress;
	private int _divider;

	public void Tick()
	{
		if (!_transferInProgress)
		{
			return;
		}

		if (_divider++ < Gameboy.TicksPerSec / 8192 / _speedMode.GetSpeedMode())
		{
			return;
		}

		_transferInProgress = false;

		try
		{
			_sb = _serialEndpoint.Transfer(_sb);
		}
		catch (IOException e)
		{
			Console.WriteLine($"Cannot transfer byte {e}");
			_sb = 0;
		}

		_intManager.RequestInterrupt(InterruptManager.InterruptType.Serial);
	}

	public bool Accepts(int address)
	{
		return address == 0xff01 || address == 0xff02;
	}

	public void SetByte(int address, int value)
	{
		if (address == 0xff01)
		{
			_sb = value;
			return;
		}

		if (address == 0xff02)
		{
			_sc = value;
		}

		if ((_sc & (1 << 7)) != 0)
		{
			StartTransfer();
		}
	}

	public int GetByte(int address)
	{
		return address switch
		{
			0xff01 => _sb,
			0xff02 => _sc | 0b01111110,
			_ => throw new ArgumentException("Illegal address for serial port")
		};
	}

	private void StartTransfer()
	{
		_transferInProgress = true;
		_divider = 0;
	}
}
