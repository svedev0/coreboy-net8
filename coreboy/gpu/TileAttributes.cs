namespace coreboy.gpu;

public class TileAttributes
{
	public static TileAttributes Empty { get; }
	private static readonly TileAttributes[] Attributes;
	private readonly int _value;

	static TileAttributes()
	{
		Attributes = new TileAttributes[256];

		for (int i = 0; i < 256; i++)
		{
			Attributes[i] = new TileAttributes(i);
		}

		Empty = Attributes[0];
	}

	private TileAttributes(int value)
	{
		_value = value;
	}

	public static TileAttributes ValueOf(int value)
	{
		return Attributes[value];
	}

	public bool IsPriority()
	{
		return (_value & (1 << 7)) != 0;
	}

	public bool IsYFlip()
	{
		return (_value & (1 << 6)) != 0;
	}

	public bool IsXFlip()
	{
		return (_value & (1 << 5)) != 0;
	}

	public GpuRegister GetDmgPalette()
	{
		if ((_value & (1 << 4)) == 0)
		{
			return GpuRegister.Obp0;
		}
		else
		{
			return GpuRegister.Obp1;
		}
	}

	public int GetBank()
	{
		return (_value & (1 << 3)) == 0 ? 0 : 1;
	}

	public int GetColorPaletteIndex()
	{
		return _value & 0x07;
	}
}
