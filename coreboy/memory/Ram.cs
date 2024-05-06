namespace coreboy.memory;

public class Ram(int offset, int length) : IAddressSpace
{
	private readonly int[] _space = new int[length];
	private readonly int _length = length;
	private readonly int _offset = offset;

	public bool Accepts(int address)
	{
		return address >= _offset && address < _offset + _length;
	}

	public void SetByte(int address, int value)
	{
		_space[address - _offset] = value;
	}

	public int GetByte(int address)
	{
		int index = address - _offset;

		if (index < 0 || index >= _space.Length)
		{
			throw new IndexOutOfRangeException("Address: " + address);
		}

		return _space[index];
	}
}
