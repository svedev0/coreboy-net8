namespace coreboy.memory;

public class Ram : IAddressSpace
{
	private readonly int[] _space;
	private readonly int _length;
	private readonly int _offset;

	// Ignore IntelliSense. This should not be made into a primary constructor
	// because that removes the readonly modifier and changes the semantics.
	public Ram(int offset, int length)
	{
		_space = new int[length];
		_length = length;
		_offset = offset;
	}

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
