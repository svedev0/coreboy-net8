namespace coreboy.memory;

public class Mmu : IAddressSpace
{
	private static readonly IAddressSpace Void = new VoidAddressSpace();
	private readonly List<IAddressSpace> _spaces = [];

	public void AddAddressSpace(IAddressSpace space)
	{
		_spaces.Add(space);
	}

	public bool Accepts(int address)
	{
		return true;
	}

	public void SetByte(int address, int value)
	{
		GetSpace(address).SetByte(address, value);
	}

	public int GetByte(int address)
	{
		return GetSpace(address).GetByte(address);
	}

	private IAddressSpace GetSpace(int address)
	{
		foreach (var s in _spaces)
		{
			if (s.Accepts(address))
			{
				return s;
			}
		}

		return Void;
	}
}
