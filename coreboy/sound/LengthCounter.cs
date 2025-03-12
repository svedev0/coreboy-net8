namespace coreboy.sound;

public class LengthCounter(int fullLength)
{
	private long _index;
	private int _divider => Gameboy.TicksPerSec / 256;
	private readonly int _fullLength = fullLength;

	public bool Enabled { get; private set; }
	public int Length { get; private set; }

	public void Start()
	{
		_index = 8192;
	}

	public void Tick()
	{
		_index++;

		if (_index == _divider)
		{
			_index = 0;

			if (Enabled && Length > 0)
			{
				Length--;
			}
		}
	}

	public void SetLength(int len)
	{
		if (len == 0)
		{
			Length = _fullLength;
		}
		else
		{
			Length = len;
		}
	}

	public void SetNr4(int value)
	{
		bool enable = (value & (1 << 6)) != 0;
		bool trigger = (value & (1 << 7)) != 0;

		if (Enabled)
		{
			if (Length == 0 && trigger)
			{
				if (enable && _index < _divider / 2)
				{
					SetLength(_fullLength - 1);
				}
				else
				{
					SetLength(_fullLength);
				}
			}
		}
		else if (enable)
		{
			if (Length > 0 && _index < _divider / 2)
			{
				Length--;
			}

			if (Length == 0 && trigger && _index < _divider / 2)
			{
				SetLength(_fullLength - 1);
			}
		}
		else
		{
			if (Length == 0 && trigger)
			{
				SetLength(_fullLength);
			}
		}

		Enabled = enable;
	}

	public override string ToString()
	{
		if (Enabled)
		{
			return $"LengthCounter[l={Length},f={_fullLength},c={_index},enabled]";
		}

		return $"LengthCounter[l={Length},f={_fullLength},c={_index},disabled]";
	}

	public void Reset()
	{
		Enabled = true;
		_index = 0;
		Length = 0;
	}
}
