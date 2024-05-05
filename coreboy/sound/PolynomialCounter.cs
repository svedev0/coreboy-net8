namespace coreboy.sound;

public class PolynomialCounter
{
	private int _index;
	private int _shiftedDivisor;

	public void SetNr43(int value)
	{
		int clockShift = value >> 4;

		int divisor = (value & 0b111) switch
		{
			0 => 8,
			1 => 16,
			2 => 32,
			3 => 48,
			4 => 64,
			5 => 80,
			6 => 96,
			7 => 112,
			_ => throw new InvalidOperationException()
		};

		_shiftedDivisor = divisor << clockShift;
		_index = 1;
	}

	public bool Tick()
	{
		_index--;

		if (_index == 0)
		{
			_index = _shiftedDivisor;
			return true;
		}
		else
		{
			return false;
		}
	}
}
