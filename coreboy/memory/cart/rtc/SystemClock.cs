namespace coreboy.memory.cart.rtc;

public class SystemClock : IClock
{
	public long CurrentTimeMillis()
	{
		return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	}
}
