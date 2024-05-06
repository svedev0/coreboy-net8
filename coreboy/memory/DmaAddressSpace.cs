namespace coreboy.memory;

public class DmaAddressSpace(IAddressSpace addressSpace) : IAddressSpace
{
	private readonly IAddressSpace _addressSpace = addressSpace;

	public bool Accepts(int address)
	{
		return true;
	}

	public void SetByte(int address, int value)
	{
		throw new NotImplementedException("Unsupported");
	}

	public int GetByte(int address)
	{
		if (address < 0xe000)
		{
			return _addressSpace.GetByte(address);
		}
		else
		{
			return _addressSpace.GetByte(address - 0x2000);
		}
	}
}
