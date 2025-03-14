using coreboy.cpu;
using coreboy.gpu.phase;
using coreboy.memory;

namespace coreboy.gpu;

public class Gpu : IAddressSpace
{
	public enum Mode
	{
		HBlank,
		VBlank,
		OamSearch,
		PixelTransfer
	}

	private readonly Ram _vRam0;
	private readonly Ram? _vRam1;
	private readonly Ram _oamRam;
	private readonly IDisplay _display;
	private readonly InterruptManager _intManager;
	private readonly Dma _dma;
	private readonly Lcdc _lcdc;
	private readonly bool _gbc;
	private readonly ColorPalette _bgPalette;
	private readonly ColorPalette _oamPalette;
	private readonly HBlankPhase _hBlankPhase;
	private readonly OamSearch _oamSearchPhase;
	private readonly PixelTransfer _pixelTransferPhase;
	private readonly VBlankPhase _vBlankPhase;
	private readonly MemoryRegisters _memRegs;

	private bool lcdEnabled = true;
	private int lcdEnabledDelay;
	private int ticksInLine;
	private Mode mode;
	private IGpuPhase phase;

	public Gpu(
		IDisplay display, InterruptManager intManager, Dma dma, Ram oamRam, bool gbc)
	{
		_memRegs = new MemoryRegisters([.. GpuRegister.Values()]);
		_lcdc = new Lcdc();
		_intManager = intManager;
		_gbc = gbc;
		_vRam0 = new Ram(0x8000, 0x2000);
		_vRam1 = gbc ? new Ram(0x8000, 0x2000) : null;
		_oamRam = oamRam;
		_dma = dma;

		_bgPalette = new ColorPalette(0xff68);
		_oamPalette = new ColorPalette(0xff6a);
		_oamPalette.FillWithFf();

		_oamSearchPhase = new OamSearch(oamRam, _lcdc, _memRegs);
		_pixelTransferPhase = new PixelTransfer(
			_vRam0,
			_vRam1,
			oamRam,
			display,
			_lcdc,
			_memRegs,
			gbc,
			_bgPalette,
			_oamPalette);
		_hBlankPhase = new HBlankPhase();
		_vBlankPhase = new VBlankPhase();

		mode = Mode.OamSearch;
		phase = _oamSearchPhase.Start();

		_display = display;
	}

	private IAddressSpace? GetAddressSpace(int address)
	{
		if (_vRam0.Accepts(address))
		{
			return GetVideoRam();
		}

		if (_oamRam.Accepts(address) && !_dma.IsOamBlocked())
		{
			return _oamRam;
		}

		if (_lcdc.Accepts(address))
		{
			return _lcdc;
		}

		if (_memRegs.Accepts(address))
		{
			return _memRegs;
		}

		if (_gbc && _bgPalette.Accepts(address))
		{
			return _bgPalette;
		}

		if (_gbc && _oamPalette.Accepts(address))
		{
			return _oamPalette;
		}

		return null;
	}

	private Ram GetVideoRam()
	{
		if (_gbc && _vRam1 != null && (_memRegs.Get(GpuRegister.Vbk) & 1) == 1)
		{
			return _vRam1;
		}

		return _vRam0;
	}

	public bool Accepts(int address)
	{
		return GetAddressSpace(address) != null;
	}

	public void SetByte(int address, int value)
	{
		if (address == GpuRegister.Stat.Address)
		{
			SetStat(value);
			return;
		}

		IAddressSpace? space = GetAddressSpace(address);
		if (space == _lcdc)
		{
			SetLcdc(value);
			return;
		}

		space?.SetByte(address, value);
	}

	public int GetByte(int address)
	{
		if (address == GpuRegister.Stat.Address)
		{
			return GetStat();
		}

		IAddressSpace? space = GetAddressSpace(address);
		if (space == null)
		{
			return 0xff;
		}

		if (address == GpuRegister.Vbk.Address)
		{
			return _gbc ? 0xfe : 0xff;
		}

		return space.GetByte(address);
	}

	public Mode? Tick()
	{
		if (!lcdEnabled && lcdEnabledDelay != -1)
		{
			lcdEnabledDelay--;
			if (lcdEnabledDelay == 0)
			{
				_display.Enabled = true;
				lcdEnabled = true;
			}
		}

		if (!lcdEnabled)
		{
			return null;
		}

		Mode oldMode = mode;
		ticksInLine++;

		if (phase.Tick())
		{
			// switch line 153 to 0
			if (ticksInLine == 4 &&
				mode == Mode.VBlank &&
				_memRegs.Get(GpuRegister.Ly) == 153)
			{
				_memRegs.Put(GpuRegister.Ly, 0);
				RequestLycEqualsLyInterrupt();
			}
		}
		else
		{
			switch (oldMode)
			{
				case Mode.OamSearch:
					mode = Mode.PixelTransfer;
					phase = _pixelTransferPhase.Start(_oamSearchPhase.GetSprites());
					break;

				case Mode.PixelTransfer:
					mode = Mode.HBlank;
					phase = _hBlankPhase.Start(ticksInLine);
					RequestLcdcInterrupt(3);
					break;

				case Mode.HBlank:
					ticksInLine = 0;

					if (_memRegs.PreIncrement(GpuRegister.Ly) == 144)
					{
						mode = Mode.VBlank;
						phase = _vBlankPhase.Start();
						_intManager.RequestInterrupt(InterruptManager.InterruptType.VBlank);
						RequestLcdcInterrupt(4);
					}
					else
					{
						mode = Mode.OamSearch;
						phase = _oamSearchPhase.Start();
					}

					RequestLcdcInterrupt(5);
					RequestLycEqualsLyInterrupt();
					break;

				case Mode.VBlank:
					ticksInLine = 0;

					if (_memRegs.PreIncrement(GpuRegister.Ly) == 1)
					{
						mode = Mode.OamSearch;
						_memRegs.Put(GpuRegister.Ly, 0);
						phase = _oamSearchPhase.Start();
						RequestLcdcInterrupt(5);
					}
					else
					{
						phase = _vBlankPhase.Start();
					}

					RequestLycEqualsLyInterrupt();
					break;
			}
		}

		if (oldMode == mode)
		{
			return null;
		}

		return mode;
	}

	public int GetTicksInLine()
	{
		return ticksInLine;
	}

	private void RequestLcdcInterrupt(int statBit)
	{
		if ((_memRegs.Get(GpuRegister.Stat) & (1 << statBit)) != 0)
		{
			_intManager.RequestInterrupt(InterruptManager.InterruptType.Lcdc);
		}
	}

	private void RequestLycEqualsLyInterrupt()
	{
		if (_memRegs.Get(GpuRegister.Lyc) == _memRegs.Get(GpuRegister.Ly))
		{
			RequestLcdcInterrupt(6);
		}
	}

	private int GetStat()
	{
		if (_memRegs.Get(GpuRegister.Lyc) == _memRegs.Get(GpuRegister.Ly))
		{
			return _memRegs.Get(GpuRegister.Stat) | (int)mode | (1 << 2) | 0x80;
		}
		else
		{
			return _memRegs.Get(GpuRegister.Stat) | (int)mode | (0) | 0x80;
		}
	}

	private void SetStat(int value)
	{
		_memRegs.Put(GpuRegister.Stat, value & 0b11111000);
	}

	private void SetLcdc(int value)
	{
		_lcdc.Set(value);

		if ((value & (1 << 7)) == 0)
		{
			DisableLcd();
		}
		else
		{
			EnableLcd();
		}
	}

	private void DisableLcd()
	{
		_memRegs.Put(GpuRegister.Ly, 0);
		ticksInLine = 0;
		phase = _hBlankPhase.Start(250);
		mode = Mode.HBlank;
		lcdEnabled = false;
		lcdEnabledDelay = -1;
		_display.Enabled = false;
	}

	private void EnableLcd()
	{
		lcdEnabledDelay = 244;
	}

	public bool IsLcdEnabled()
	{
		return lcdEnabled;
	}

	public Lcdc GetLcdc()
	{
		return _lcdc;
	}
}
