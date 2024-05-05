namespace coreboy.sound;

public abstract class SoundModeBase(int offset, int length, bool gbc) : IAddressSpace
{
	protected readonly int Offset = offset;
	protected readonly bool Gbc = gbc;
	protected LengthCounter Length = new(length);
	protected bool DacEnabled;
	protected bool ChannelEnabled;

	protected int Nr0, Nr1, Nr2, Nr3, Nr4;

	protected virtual int GetNr0() => Nr0;
	protected virtual int GetNr1() => Nr1;
	protected virtual int GetNr2() => Nr2;
	protected virtual int GetNr3() => Nr3;
	protected virtual int GetNr4() => Nr4;

	protected virtual void SetNr0(int value) => Nr0 = value;
	protected virtual void SetNr1(int value) => Nr1 = value;
	protected virtual void SetNr2(int value) => Nr2 = value;
	protected virtual void SetNr3(int value) => Nr3 = value;

	public abstract int Tick();
	protected abstract void Trigger();

	public bool IsEnabled()
	{
		return ChannelEnabled && DacEnabled;
	}

	public virtual bool Accepts(int address)
	{
		return address >= Offset && address < Offset + 5;
	}

	public virtual void SetByte(int address, int value)
	{
		int offset = address - Offset;

		switch (offset)
		{
			case 0:
				SetNr0(value);
				break;
			case 1:
				SetNr1(value);
				break;
			case 2:
				SetNr2(value);
				break;
			case 3:
				SetNr3(value);
				break;
			case 4:
				SetNr4(value);
				break;
		}
	}

	public virtual int GetByte(int address)
	{
		int offset = address - Offset;

		return offset switch
		{
			0 => GetNr0(),
			1 => GetNr1(),
			2 => GetNr2(),
			3 => GetNr3(),
			4 => GetNr4(),
			_ => throw new ArgumentException("Illegal address for sound mode")
		};
	}

	protected virtual void SetNr4(int value)
	{
		Nr4 = value;
		Length.SetNr4(value);

		if ((value & (1 << 7)) != 0)
		{
			ChannelEnabled = DacEnabled;
			Trigger();
		}
	}

	protected virtual int GetFrequency()
	{
		return 2048 - (GetNr3() | ((GetNr4() & 0b111) << 8));
	}

	public abstract void Start();

	public void Stop() => ChannelEnabled = false;

	protected bool UpdateLength()
	{
		Length.Tick();

		if (!Length.Enabled)
		{
			return ChannelEnabled;
		}

		if (ChannelEnabled && Length.Length == 0)
		{
			ChannelEnabled = false;
		}

		return ChannelEnabled;
	}
}
