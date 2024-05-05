namespace coreboy.gpu;

public class ColorPixelFifo(
	Lcdc lcdc,
	IDisplay display,
	ColorPalette bgPalette,
	ColorPalette oamPalette) : IPixelFifo
{
	private readonly IntQueue _pixels = new(16);
	private readonly IntQueue _palettes = new(16);
	private readonly IntQueue _priorities = new(16);
	private readonly Lcdc _lcdc = lcdc;
	private readonly IDisplay _display = display;
	private readonly ColorPalette _bgPalette = bgPalette;
	private readonly ColorPalette _oamPalette = oamPalette;

	public int GetLength()
	{
		return _pixels.Size();
	}

	public void PutPixelToScreen()
	{
		_display.PutColorPixel(DequeuePixel());
	}

	private int DequeuePixel()
	{
		return GetColor(
			_priorities.Dequeue(), _palettes.Dequeue(), _pixels.Dequeue());
	}

	public void DropPixel() => DequeuePixel();

	public void Enqueue8Pixels(int[] pixelRow, TileAttributes tileAttributes)
	{
		foreach (var p in pixelRow)
		{
			_pixels.Enqueue(p);
			_palettes.Enqueue(tileAttributes.GetColorPaletteIndex());
			_priorities.Enqueue(tileAttributes.IsPriority() ? 100 : -1);
		}
	}

	/*
	lcdc.0

	when 0 => sprites are always displayed on top of the bg

	bg tile attribute.7

	when 0 => use oam priority bit
	when 1 => bg priority

	sprite attribute.7

	when 0 => sprite above bg
	when 1 => sprite above bg color 0
	*/

	public void SetOverlay(
		int[] pixelRow, int offset, TileAttributes spriteAttr, int oamIndex)
	{
		for (int j = offset; j < pixelRow.Length; j++)
		{
			int pixel = pixelRow[j];
			int index = j - offset;

			if (pixel == 0)
			{
				continue;
			}

			int oldPriority = _priorities.Get(index);
			bool put = false;

			if ((oldPriority == -1 || oldPriority == 100) &&
				!_lcdc.IsBgAndWindowDisplay())
			{
				// this one takes precedence
				put = true;
			}
			else if (oldPriority == 100)
			{
				// bg with priority
				put = _pixels.Get(index) == 0;
			}
			else if (oldPriority == -1 && !spriteAttr.IsPriority())
			{
				// bg without priority
				put = true;
			}
			else if (oldPriority == -1 &&
				spriteAttr.IsPriority() &&
				_pixels.Get(index) == 0)
			{
				// bg without priority
				put = true;
			}
			else if (oldPriority >= 0 && oldPriority < 10)
			{
				// other sprite
				put = oldPriority > oamIndex;
			}

			if (put)
			{
				_pixels.Set(index, pixel);
				_palettes.Set(index, spriteAttr.GetColorPaletteIndex());
				_priorities.Set(index, oamIndex);
			}
		}
	}

	public void Clear()
	{
		_pixels.Clear();
		_palettes.Clear();
		_priorities.Clear();
	}

	private int GetColor(int priority, int palette, int color)
	{
		if (priority >= 0 && priority < 10)
		{
			return _oamPalette.GetPalette(palette)[color];
		}
		else
		{
			return _bgPalette.GetPalette(palette)[color];
		}
	}
}
