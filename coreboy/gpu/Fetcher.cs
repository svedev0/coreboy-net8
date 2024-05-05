#nullable disable

using coreboy.gpu.phase;
using coreboy.memory;

namespace coreboy.gpu;

using static cpu.BitUtils;
using static GpuRegister;

public class Fetcher(
	IPixelFifo fifo,
	IAddressSpace videoRam0,
	IAddressSpace videoRam1,
	IAddressSpace oemRam,
	Lcdc lcdc,
	MemoryRegisters registers,
	bool gbc)
{
	private enum State
	{
		ReadTileId,
		ReadData1,
		ReadData2,
		Push,
		ReadSpriteTileId,
		ReadSpriteFlags,
		ReadSpriteData1,
		ReadSpriteData2,
		PushSprite
	}

	private static readonly int[] EmptyPixelRow = new int[8];

	private static readonly List<State> SpritesInProgress = [
		State.ReadSpriteTileId,
		State.ReadSpriteFlags,
		State.ReadSpriteData1,
		State.ReadSpriteData2,
		State.PushSprite
	];

	private readonly IPixelFifo _fifo = fifo;
	private readonly IAddressSpace _videoRam0 = videoRam0;
	private readonly IAddressSpace _videoRam1 = videoRam1;
	private readonly IAddressSpace _oemRam = oemRam;
	private readonly MemoryRegisters _memRegs = registers;
	private readonly Lcdc _lcdc = lcdc;
	private readonly bool _gbc = gbc;

	private readonly int[] _pixelLine = new int[8];
	private State _state;
	private bool _fetchingDisabled;

	private int _mapAddress;
	private int _xOffset;

	private int _tileDataAddress;
	private bool _tileIdSigned;
	private int _tileLine;
	private int _tileId;
	private TileAttributes _tileAttributes;
	private int _tileData1;
	private int _tileData2;

	private int _spriteTileLine;
	private OamSearch.SpritePosition _sprite;
	private TileAttributes _spriteAttributes;
	private int _spriteOffset;
	private int _spriteOamIndex;

	private int _divider = 2;

	public void Init()
	{
		_state = State.ReadTileId;
		_tileId = 0;
		_tileData1 = 0;
		_tileData2 = 0;
		_divider = 2;
		_fetchingDisabled = false;
	}

	public void StartFetching(
		int mapAddress, int tileDataAddress, int xOffset, bool tileIdSigned, int tileLine)
	{
		_mapAddress = mapAddress;
		_tileDataAddress = tileDataAddress;
		_xOffset = xOffset;
		_tileIdSigned = tileIdSigned;
		_tileLine = tileLine;
		_fifo.Clear();

		_state = State.ReadTileId;
		_tileId = 0;
		_tileData1 = 0;
		_tileData2 = 0;
		_divider = 2;
	}

	public void FetchingDisabled()
	{
		_fetchingDisabled = true;
	}

	public void AddSprite(OamSearch.SpritePosition sprite, int offset, int oamIndex)
	{
		_sprite = sprite;
		_state = State.ReadSpriteTileId;
		_spriteTileLine = _memRegs.Get(Ly) + 16 - sprite.GetY();
		_spriteOffset = offset;
		_spriteOamIndex = oamIndex;
	}

	public void Tick()
	{
		if (_fetchingDisabled && _state == State.ReadTileId)
		{
			if (_fifo.GetLength() <= 8)
			{
				_fifo.Enqueue8Pixels(EmptyPixelRow, _tileAttributes);
			}

			return;
		}

		_divider--;

		if (_divider == 0)
		{
			_divider = 2;
		}
		else
		{
			return;
		}

	stateSwitch:

		switch (_state)
		{
			case State.ReadTileId:
				_tileId = _videoRam0.GetByte(_mapAddress + _xOffset);

				if (_gbc)
				{
					_tileAttributes = TileAttributes.ValueOf(
						_videoRam1.GetByte(_mapAddress + _xOffset));
				}
				else
				{
					_tileAttributes = TileAttributes.Empty;
				}

				_state = State.ReadData1;
				break;

			case State.ReadData1:
				_tileData1 = GetTileData(
					_tileId, _tileLine, 0, _tileDataAddress, _tileIdSigned, _tileAttributes, 8);
				_state = State.ReadData2;
				break;

			case State.ReadData2:
				_tileData2 = GetTileData(
					_tileId, _tileLine, 1, _tileDataAddress, _tileIdSigned, _tileAttributes, 8);
				_state = State.Push;
				goto stateSwitch; // Sorry mum

			case State.Push:
				if (_fifo.GetLength() <= 8)
				{
					_fifo.Enqueue8Pixels(
						Zip(_tileData1, _tileData2, _tileAttributes.IsXFlip()),
						_tileAttributes);
					_xOffset = (_xOffset + 1) % 0x20;
					_state = State.ReadTileId;
				}
				break;

			case State.ReadSpriteTileId:
				_tileId = _oemRam.GetByte(_sprite.GetAddress() + 2);
				_state = State.ReadSpriteFlags;
				break;

			case State.ReadSpriteFlags:
				_spriteAttributes = TileAttributes.ValueOf(
					_oemRam.GetByte(_sprite.GetAddress() + 3));
				_state = State.ReadSpriteData1;
				break;

			case State.ReadSpriteData1:
				if (_lcdc.GetSpriteHeight() == 16)
				{
					_tileId &= 0xfe;
				}

				_tileData1 = GetTileData(
					_tileId,
					_spriteTileLine,
					0,
					0x8000,
					false,
					_spriteAttributes,
					_lcdc.GetSpriteHeight());
				_state = State.ReadSpriteData2;
				break;

			case State.ReadSpriteData2:
				_tileData2 = GetTileData(
					_tileId,
					_spriteTileLine,
					1,
					0x8000,
					false,
					_spriteAttributes,
					_lcdc.GetSpriteHeight());
				_state = State.PushSprite;
				break;

			case State.PushSprite:
				_fifo.SetOverlay(
					Zip(_tileData1, _tileData2, _spriteAttributes.IsXFlip()),
					_spriteOffset,
					_spriteAttributes,
					_spriteOamIndex);
				_state = State.ReadTileId;
				break;
		}
	}

	private int GetTileData(
		int tileId,
		int line,
		int byteNumber,
		int tileDataAddress,
		bool signed,
		TileAttributes attr,
		int tileHeight)
	{
		int effectiveLine;

		if (attr.IsYFlip())
		{
			effectiveLine = tileHeight - 1 - line;
		}
		else
		{
			effectiveLine = line;
		}

		int tileAddress;

		if (signed)
		{
			tileAddress = tileDataAddress + ToSigned(tileId) * 0x10;
		}
		else
		{
			tileAddress = tileDataAddress + tileId * 0x10;
		}

		IAddressSpace vRam;

		if (attr.GetBank() == 0 || !_gbc)
		{
			vRam = _videoRam0;
		}
		else
		{
			vRam = _videoRam1;
		}

		return vRam.GetByte(tileAddress + effectiveLine * 2 + byteNumber);
	}

	public bool SpriteInProgress()
	{
		return SpritesInProgress.Contains(_state);
	}

	public int[] Zip(int data1, int data2, bool reverse)
	{
		return Zip(data1, data2, reverse, _pixelLine);
	}

	public static int[] Zip(int data1, int data2, bool reverse, int[] pixelRow)
	{
		for (int i = 7; i >= 0; i--)
		{
			int mask = 1 << i;
			int pixel;

			if ((data2 & mask) == 0)
			{
				pixel = 2 * 0 + ((data1 & mask) == 0 ? 0 : 1);
			}
			else
			{
				pixel = 2 * 1 + ((data1 & mask) == 0 ? 0 : 1);
			}

			if (reverse)
			{
				pixelRow[i] = pixel;
			}
			else
			{
				pixelRow[7 - i] = pixel;
			}
		}

		return pixelRow;
	}
}
