using coreboy.cpu;

namespace coreboy.serial;

public class SerialPort(
	InterruptManager interruptManager,
	ISerialEndpoint serialEndpoint,
	SpeedMode speedMode) : IAddressSpace
{
	private readonly ISerialEndpoint _serialEndpoint = serialEndpoint;
	private readonly InterruptManager _interruptManager = interruptManager;
	private readonly SpeedMode _speedMode = speedMode;
	private int serialByte;
	private int serialControl;
	private bool transferInProgress;
	private int divider;

	public void Tick()
	{
		if (!transferInProgress)
		{
			return;
		}

		if (divider++ >= Gameboy.TicksPerSec / 8192 / _speedMode.GetSpeedMode())
		{
			transferInProgress = false;

			try
			{
				serialByte = _serialEndpoint.Transfer(serialByte);
			}
			catch (IOException e)
			{
				Console.WriteLine($"Can't transfer byte {e}");
				serialByte = 0;
			}

			_interruptManager.RequestInterrupt(InterruptManager.InterruptType.Serial);
		}
	}

	public bool Accepts(int address)
	{
		return address == 0xff01 || address == 0xff02;
	}

	public void SetByte(int address, int value)
	{
		if (address == 0xff01)
		{
			serialByte = value;
		}
		else if (address == 0xff02)
		{
			serialControl = value;

			if ((serialControl & (1 << 7)) != 0)
			{
				StartTransfer();
			}
		}
	}

	public int GetByte(int address)
	{
		return address switch
		{
			0xff01 => serialByte,
			0xff02 => serialControl | 0b01111110,
			_ => throw new ArgumentException($"Invalid address: {address}"),
		};
	}

	private void StartTransfer()
	{
		transferInProgress = true;
		divider = 0;
	}
}
