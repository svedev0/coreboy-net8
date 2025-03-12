using coreboy.memory;

namespace coreboy.gpu.phase;

public class OamSearch(IAddressSpace oemRam, Lcdc lcdc, MemoryRegisters registers) : IGpuPhase
{
	private enum State
	{
		ReadingY,
		ReadingX
	}

	public sealed class SpritePosition(int x, int y, int address)
	{
		private readonly int _x = x;
		private readonly int _y = y;
		private readonly int _address = address;

		public int GetX()
		{
			return _x;
		}

		public int GetY()
		{
			return _y;
		}

		public int GetAddress()
		{
			return _address;
		}
	}

	private readonly IAddressSpace _oemRam = oemRam;
	private readonly MemoryRegisters _registers = registers;
	private readonly SpritePosition?[] _sprites = new SpritePosition[10];
	private readonly Lcdc _lcdc = lcdc;
	private int spritePosIndex;
	private State state;
	private int spriteY;
	private int spriteX;
	private int index;

	public OamSearch Start()
	{
		spritePosIndex = 0;
		state = State.ReadingY;
		spriteY = 0;
		spriteX = 0;
		index = 0;

		for (int j = 0; j < _sprites.Length; j++)
		{
			_sprites[j] = null;
		}

		return this;
	}

	public bool Tick()
	{
		int spriteAddress = 0xfe00 + 4 * index;

		switch (state)
		{
			case State.ReadingY:
				spriteY = _oemRam.GetByte(spriteAddress);
				state = State.ReadingX;
				break;

			case State.ReadingX:
				spriteX = _oemRam.GetByte(spriteAddress + 1);

				bool between = Between(
					spriteY,
					_registers.Get(GpuRegister.Ly) + 16,
					spriteY + _lcdc.GetSpriteHeight());

				if (spritePosIndex < _sprites.Length && between)
				{
					_sprites[spritePosIndex++] = new SpritePosition(
						spriteX, spriteY, spriteAddress);
				}

				index++;
				state = State.ReadingY;
				break;
		}

		return index < 40;
	}

	public SpritePosition?[] GetSprites()
	{
		return _sprites;
	}

	private static bool Between(int from, int x, int to)
	{
		return from <= x && x < to;
	}
}
