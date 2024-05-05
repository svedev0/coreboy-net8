namespace coreboy.gpu;

public class Lcdc : IAddressSpace
{
	private int _value = 0x91;

	public bool IsBgAndWindowDisplay()
	{
		return (_value & 0x01) != 0;
	}

	public bool IsObjDisplay()
	{
		return (_value & 0x02) != 0;
	}

	public int GetSpriteHeight()
	{
		return (_value & 0x04) == 0 ? 8 : 16;
	}

	public int GetBgTileMapDisplay()
	{
		return (_value & 0x08) == 0 ? 0x9800 : 0x9c00;
	}

	public int GetBgWindowTileData()
	{
		return (_value & 0x10) == 0 ? 0x9000 : 0x8000;
	}

	public bool IsBgWindowTileDataSigned()
	{
		return (_value & 0x10) == 0;
	}

	public bool IsWindowDisplay()
	{
		return (_value & 0x20) != 0;
	}

	public int GetWindowTileMapDisplay()
	{
		return (_value & 0x40) == 0 ? 0x9800 : 0x9c00;
	}

	public bool IsLcdEnabled()
	{
		return (_value & 0x80) != 0;
	}

	public bool Accepts(int address)
	{
		return address == 0xff40;
	}

	public void SetByte(int address, int val)
	{
		_value = val;
	}

	public int GetByte(int address)
	{
		return _value;
	}

	public void Set(int val)
	{
		_value = val;
	}

	public int Get()
	{
		return _value;
	}
}
