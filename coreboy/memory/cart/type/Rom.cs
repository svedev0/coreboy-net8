namespace coreboy.memory.cart.type;

public class Rom(int[] rom, CartridgeType type, int romBanks, int ramBanks) : IAddressSpace
{
	private readonly int[] _rom = rom;
	private readonly CartridgeType _type = type;
	private readonly int _romBanks = romBanks;
	private readonly int _ramBanks = ramBanks;

	public bool Accepts(int address)
	{
		return address >= 0x0000 &&
			address < 0x8000 || address >= 0xa000 &&
			address < 0xc000;
	}

	public void SetByte(int address, int value)
	{
	}

	public int GetByte(int address)
	{
		if (address >= 0x0000 && address < 0x8000)
		{
			return _rom[address];
		}

		return 0;
	}
}
