using coreboy.gpu;

namespace coreboy.memory;

public class Hdma(IAddressSpace addressSpace) : IAddressSpace
{
	private const int Hdma1 = 0xff51;
	private const int Hdma2 = 0xff52;
	private const int Hdma3 = 0xff53;
	private const int Hdma4 = 0xff54;
	private const int Hdma5 = 0xff55;

	private readonly IAddressSpace _addressSpace = addressSpace;
	private readonly Ram _hdma1234 = new(Hdma1, 4);
	private Gpu.Mode? _gpuMode;

	private bool transferInProgress;
	private bool hBlankTransfer;
	private bool lcdEnabled;
	private int length;
	private int src;
	private int dst;
	private int tick;

	public bool Accepts(int address)
	{
		return address >= Hdma1 && address <= Hdma5;
	}

	public void Tick()
	{
		if (!IsTransferInProgress())
		{
			return;
		}

		if (++tick < 0x20)
		{
			return;
		}

		for (int j = 0; j < 0x10; j++)
		{
			_addressSpace.SetByte(dst + j, _addressSpace.GetByte(src + j));
		}

		src += 0x10;
		dst += 0x10;

		length--;

		if (length == 0)
		{
			transferInProgress = false;
			length = 0x7f;
		}
		else if (hBlankTransfer)
		{
			_gpuMode = null; // wait until next HBlank
		}
	}

	public void SetByte(int address, int value)
	{
		if (_hdma1234.Accepts(address))
		{
			_hdma1234.SetByte(address, value);
		}
		else if (address == Hdma5)
		{
			if (transferInProgress)
			{
				StopTransfer();
			}
			else
			{
				StartTransfer(value);
			}
		}
	}

	public int GetByte(int address)
	{
		if (_hdma1234.Accepts(address))
		{
			return 0xff;
		}

		if (address == Hdma5)
		{
			return (transferInProgress ? 0 : (1 << 7)) | length;
		}

		throw new ArgumentException("Invalid address");
	}

	public void OnGpuUpdate(Gpu.Mode newGpuMode)
	{
		_gpuMode = newGpuMode;
	}

	public void OnLcdSwitch(bool lcdEnabled)
	{
		this.lcdEnabled = lcdEnabled;
	}

	public bool IsTransferInProgress()
	{
		if (!transferInProgress)
		{
			return false;
		}

		if (hBlankTransfer && (_gpuMode == Gpu.Mode.HBlank || !lcdEnabled))
		{
			return true;
		}

		return !hBlankTransfer;
	}

	private void StartTransfer(int reg)
	{
		hBlankTransfer = (reg & (1 << 7)) != 0;
		length = reg & 0x7f;

		src = (_hdma1234.GetByte(Hdma1) << 8) |
			(_hdma1234.GetByte(Hdma2) & 0xf0);
		dst = ((_hdma1234.GetByte(Hdma3) & 0x1f) << 8) |
			(_hdma1234.GetByte(Hdma4) & 0xf0);
		src &= 0xfff0;
		dst = (dst & 0x1fff) | 0x8000;

		transferInProgress = true;
	}

	private void StopTransfer()
	{
		transferInProgress = false;
	}
}
