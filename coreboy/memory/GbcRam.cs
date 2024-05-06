namespace coreboy.memory;

public class GbcRam : IAddressSpace
{
	private readonly int[] _ram = new int[7 * 0x1000];

	private int svbk;

	public bool Accepts(int address)
	{
		return address == 0xff70 || (address >= 0xd000 && address < 0xe000);
	}

	public void SetByte(int address, int value)
	{
		if (address == 0xff70)
		{
			svbk = value;
		}
		else
		{
			_ram[Translate(address)] = value;
		}
	}

	public int GetByte(int address)
	{
		return address == 0xff70 ? svbk : _ram[Translate(address)];
	}

	private int Translate(int address)
	{
		int ramBank = svbk & 0x7;

		if (ramBank == 0)
		{
			ramBank = 1;
		}

		int result = address - 0xd000 + (ramBank - 1) * 0x1000;

		if (result < 0 || result >= _ram.Length)
		{
			throw new ArgumentException("Invalid address");
		}

		return result;
	}
}
