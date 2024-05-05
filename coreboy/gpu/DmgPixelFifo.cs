using coreboy.memory;

namespace coreboy.gpu;

public class DmgPixelFifo(IDisplay display, MemoryRegisters registers) : IPixelFifo
{
	public IntQueue Pixels { get; } = new(16);
	private readonly IntQueue _palettes = new(16);
	private readonly IntQueue _pixelType = new(16);

	private readonly IDisplay _display = display;
	private readonly MemoryRegisters _registers = registers;

	public int GetLength()
	{
		return Pixels.Size();
	}

	public void PutPixelToScreen()
	{
		_display.PutDmgPixel(DequeuePixel());
	}

	public void DropPixel() => DequeuePixel();

	public int DequeuePixel()
	{
		_pixelType.Dequeue();
		return GetColor(_palettes.Dequeue(), Pixels.Dequeue());
	}

	public void Enqueue8Pixels(int[] pixelRow, TileAttributes tileAttributes)
	{
		foreach (var pixel in pixelRow)
		{
			Pixels.Enqueue(pixel);
			_palettes.Enqueue(_registers.Get(GpuRegister.Bgp));
			_pixelType.Enqueue(0);
		}
	}

	public void SetOverlay(
		int[] pixelRow, int offset, TileAttributes flags, int oamIndex)
	{
		bool priority = flags.IsPriority();
		int overlayPalette = _registers.Get(flags.GetDmgPalette());

		for (int j = offset; j < pixelRow.Length; j++)
		{
			var pixel = pixelRow[j];
			var index = j - offset;

			if (_pixelType.Get(index) == 1)
			{
				continue;
			}

			if (priority && Pixels.Get(index) == 0 || !priority && pixel != 0)
			{
				Pixels.Set(index, pixel);
				_palettes.Set(index, overlayPalette);
				_pixelType.Set(index, 1);
			}
		}
	}

	private static int GetColor(int palette, int colorIndex)
	{
		return 0b11 & (palette >> (colorIndex * 2));
	}

	public void Clear()
	{
		Pixels.Clear();
		_palettes.Clear();
		_pixelType.Clear();
	}
}
