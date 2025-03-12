namespace coreboy.memory.cart.rtc;

public class RealTimeClock(IClock clock)
{
	private readonly IClock _clock = clock;

	private long offsetSec;
	private long clockStart = clock.CurrentTimeMillis();
	private bool halt;
	private long latchStart;
	private int haltSeconds;
	private int haltMinutes;
	private int haltHours;
	private int haltDays;

	public void Latch()
	{
		latchStart = _clock.CurrentTimeMillis();
	}

	public void Unlatch()
	{
		latchStart = 0;
	}

	public int GetSeconds()
	{
		return (int)(ClockTimeInSec() % 60);
	}

	public int GetMinutes()
	{
		return (int)(ClockTimeInSec() % (60 * 60) / 60);
	}

	public int GetHours()
	{
		return (int)(ClockTimeInSec() % (60 * 60 * 24) / (60 * 60));
	}

	public int GetDayCounter()
	{
		return (int)(ClockTimeInSec() % (60 * 60 * 24 * 512) / (60 * 60 * 24));
	}

	public bool IsHalt()
	{
		return halt;
	}

	public bool IsCounterOverflow()
	{
		return ClockTimeInSec() >= 60 * 60 * 24 * 512;
	}

	public void SetSeconds(int seconds)
	{
		if (!halt)
		{
			return;
		}

		haltSeconds = seconds;
	}

	public void SetMinutes(int minutes)
	{
		if (!halt)
		{
			return;
		}

		haltMinutes = minutes;
	}

	public void SetHours(int hours)
	{
		if (!halt)
		{
			return;
		}

		haltHours = hours;
	}

	public void SetDayCounter(int dayCounter)
	{
		if (!halt)
		{
			return;
		}

		haltDays = dayCounter;
	}

	public void SetHalt(bool halt)
	{
		if (halt && !this.halt)
		{
			Latch();
			haltSeconds = GetSeconds();
			haltMinutes = GetMinutes();
			haltHours = GetHours();
			haltDays = GetDayCounter();
			Unlatch();
		}
		else if (!halt && this.halt)
		{
			offsetSec =
				haltSeconds +
				haltMinutes * 60 +
				haltHours * 60 * 60 +
				haltDays * 60 * 60 * 24;
			clockStart = _clock.CurrentTimeMillis();
		}

		this.halt = halt;
	}

	public void ClearCounterOverflow()
	{
		while (IsCounterOverflow())
		{
			offsetSec -= 60 * 60 * 24 * 512;
		}
	}

	private long ClockTimeInSec()
	{
		long now;

		if (latchStart == 0)
		{
			now = _clock.CurrentTimeMillis();
		}
		else
		{
			now = latchStart;
		}

		return (now - clockStart) / 1000 + offsetSec;
	}

	public void Deserialize(long[] clockData)
	{
		long seconds = clockData[0];
		long minutes = clockData[1];
		long hours = clockData[2];
		long days = clockData[3];
		long daysHigh = clockData[4];
		long timestamp = clockData[10];

		clockStart = timestamp * 1000;
		offsetSec =
			seconds +
			minutes * 60 +
			hours * 60 * 60 +
			days * 24 * 60 * 60 +
			daysHigh * 256 * 24 * 60 * 60;
	}

	public long[] Serialize()
	{
		long[] clockData = new long[11];

		Latch();
		clockData[0] = clockData[5] = GetSeconds();
		clockData[1] = clockData[6] = GetMinutes();
		clockData[2] = clockData[7] = GetHours();
		clockData[3] = clockData[8] = GetDayCounter() % 256;
		clockData[4] = clockData[9] = GetDayCounter() / 256;
		clockData[10] = latchStart / 1000;
		Unlatch();

		return clockData;
	}
}
