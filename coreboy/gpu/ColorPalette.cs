using System.Text;

namespace coreboy.gpu;

public class ColorPalette : IAddressSpace
{
	private readonly int _indexAddress;
	private readonly int _dataAddress;
	private int _index;
	private bool _autoIncrement;

	private readonly List<List<int>> _palettes;

	public ColorPalette(int offset)
	{
		_palettes = [];

		for (int x = 0; x < 8; x++)
		{
			List<int> row = new(4);

			for (int y = 0; y < 4; y++)
			{
				row.Add(0);
			}

			_palettes.Add(row);
		}

		_indexAddress = offset;
		_dataAddress = offset + 1;
	}

	public bool Accepts(int address)
	{
		return address == _indexAddress || address == _dataAddress;
	}

	public void SetByte(int address, int value)
	{
		if (address == _indexAddress)
		{
			_index = value & 0x3f;
			_autoIncrement = (value & (1 << 7)) != 0;
		}
		else if (address == _dataAddress)
		{
			int color = _palettes[_index / 8][_index % 8 / 2];

			if (_index % 2 == 0)
			{
				color = (color & 0xff00) | value;
			}
			else
			{
				color = (color & 0x00ff) | (value << 8);
			}
			_palettes[_index / 8][_index % 8 / 2] = color;

			if (_autoIncrement)
			{
				_index = (_index + 1) & 0x3f;
			}
		}
		else
		{
			throw new InvalidOperationException();
		}
	}

	public int GetByte(int address)
	{
		if (address == _indexAddress)
		{
			return _index | (_autoIncrement ? 0x80 : 0x00) | 0x40;
		}

		if (address != _dataAddress)
		{
			throw new ArgumentException("Invalid memory address");
		}

		int color = _palettes[_index / 8][_index % 8 / 2];

		if (_index % 2 == 0)
		{
			return color & 0xff;
		}

		return (color >> 8) & 0xff;
	}

	public int[] GetPalette(int index)
	{
		return [.. _palettes[index]];
	}

	public override string ToString()
	{
		StringBuilder sb = new();

		for (int i = 0; i < 8; i++)
		{
			sb.Append(i).Append(": ");

			int[] palette = GetPalette(i);

			foreach (int color in palette)
			{
				sb.Append($"{color:X4}").Append(' ');
			}

			sb[^1] = '\n';
		}

		return sb.ToString();
	}

	public void FillWithFf()
	{
		for (int i = 0; i < 8; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				_palettes[i][j] = 0x7fff;
			}
		}
	}
}
