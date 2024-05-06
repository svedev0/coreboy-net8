namespace coreboy.memory;

public class VoidAddressSpace : IAddressSpace
{
	public bool Accepts(int address) => true;

	public void SetByte(int address, int value)
	{
		if (address < 0 || address > 0xffff)
		{
			throw new ArgumentException("Invalid address");
		}
	}

	public int GetByte(int address)
	{
		if (address < 0 || address > 0xffff)
		{
			throw new ArgumentException("Invalid address");
		}

		return 0xff;
	}
}
