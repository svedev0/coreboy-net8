#nullable disable

using coreboy.memory;

namespace coreboy.gpu.phase;

public class PixelTransfer : IGpuPhase
{
	private readonly IPixelFifo _fifo;
	private readonly Fetcher _fetcher;
	private readonly MemoryRegisters _memRegs;
	private readonly Lcdc _lcdc;
	private readonly bool _gbc;
	private OamSearch.SpritePosition[] _sprites;
	private int _droppedPixels;
	private int _x;
	private bool _window;

	public PixelTransfer(
		IAddressSpace vRam0,
		IAddressSpace vRam1,
		IAddressSpace oemRam,
		IDisplay display,
		Lcdc lcdc,
		MemoryRegisters memRegs,
		bool gbc,
		ColorPalette bgPalette,
		ColorPalette oamPalette)
	{
		_memRegs = memRegs;
		_lcdc = lcdc;
		_gbc = gbc;

		if (gbc)
		{
			_fifo = new ColorPixelFifo(lcdc, display, bgPalette, oamPalette);
		}
		else
		{
			_fifo = new DmgPixelFifo(display, memRegs);
		}

		_fetcher = new Fetcher(_fifo, vRam0, vRam1, oemRam, lcdc, memRegs, gbc);
	}

	public PixelTransfer Start(OamSearch.SpritePosition[] sprites)
	{
		_sprites = sprites;
		_droppedPixels = 0;
		_x = 0;
		_window = false;

		_fetcher.Init();

		if (_gbc || _lcdc.IsBgAndWindowDisplay())
		{
			StartFetchingBackground();
		}
		else
		{
			_fetcher.FetchingDisabled();
		}

		return this;
	}

	public bool Tick()
	{
		_fetcher.Tick();

		if (_lcdc.IsBgAndWindowDisplay() || _gbc)
		{
			if (_fifo.GetLength() <= 8)
			{
				return true;
			}

			if (_droppedPixels < _memRegs.Get(GpuRegister.Scx) % 8)
			{
				_fifo.DropPixel();
				_droppedPixels++;
				return true;
			}

			if (!_window && _lcdc.IsWindowDisplay() &&
				_memRegs.Get(GpuRegister.Ly) >= _memRegs.Get(GpuRegister.Wy) &&
				_x == _memRegs.Get(GpuRegister.Wx) - 7)
			{
				_window = true;
				StartFetchingWindow();
				return true;
			}
		}

		if (_lcdc.IsObjDisplay())
		{
			if (_fetcher.SpriteInProgress())
			{
				return true;
			}

			bool spriteAdded = false;

			for (int i = 0; i < _sprites.Length; i++)
			{
				var spritePos = _sprites[i];

				if (spritePos == null)
				{
					continue;
				}

				if (_x == 0 && spritePos.GetX() < 8)
				{
					_fetcher.AddSprite(spritePos, 8 - spritePos.GetX(), i);
					spriteAdded = true;
					_sprites[i] = null;
				}
				else if (spritePos.GetX() - 8 == _x)
				{
					_fetcher.AddSprite(spritePos, 0, i);
					spriteAdded = true;
					_sprites[i] = null;
				}

				if (spriteAdded)
				{
					return true;
				}
			}
		}

		_fifo.PutPixelToScreen();
		_x++;

		if (_x == 160)
		{
			return false;
		}

		return true;
	}

	private void StartFetchingBackground()
	{
		int bgX = _memRegs.Get(GpuRegister.Scx) / 0x08;
		int bgY = (_memRegs.Get(GpuRegister.Scy) + _memRegs.Get(GpuRegister.Ly)) % 0x100;

		_fetcher.StartFetching(
			_lcdc.GetBgTileMapDisplay() + bgY / 0x08 * 0x20,
			_lcdc.GetBgWindowTileData(),
			bgX,
			_lcdc.IsBgWindowTileDataSigned(),
			bgY % 0x08);
	}

	private void StartFetchingWindow()
	{
		int winX = (_x - _memRegs.Get(GpuRegister.Wx) + 7) / 0x08;
		int winY = _memRegs.Get(GpuRegister.Ly) - _memRegs.Get(GpuRegister.Wy);

		_fetcher.StartFetching(
			_lcdc.GetWindowTileMapDisplay() + winY / 0x08 * 0x20,
			_lcdc.GetBgWindowTileData(),
			winX,
			_lcdc.IsBgWindowTileDataSigned(),
			winY % 0x08);
	}
}
