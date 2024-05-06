namespace coreboy.memory;

public class ShadowAddressSpace(
	IAddressSpace addressSpace,
	int echoStart,
	int targetStart,
	int length) : IAddressSpace
{
	private readonly IAddressSpace _addressSpace = addressSpace;
	private readonly int _echoStart = echoStart;
	private readonly int _targetStart = targetStart;
	private readonly int _length = length;

	public bool Accepts(int address)
	{
		return address >= _echoStart && address < _echoStart + _length;
	}

	public void SetByte(int address, int value)
	{
		_addressSpace.SetByte(Translate(address), value);
	}

	public int GetByte(int address)
	{
		return _addressSpace.GetByte(Translate(address));
	}

	private int Translate(int address)
	{
		return GetRelative(address) + _targetStart;
	}

	private int GetRelative(int address)
	{
		int index = address - _echoStart;

		if (index < 0 || index >= _length)
		{
			throw new ArgumentException("Invalid address");
		}

		return index;
	}
}