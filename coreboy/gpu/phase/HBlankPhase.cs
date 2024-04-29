namespace coreboy.gpu.phase;

public class HBlankPhase : IGpuPhase
{
	private int ticks;

	public HBlankPhase Start(int ticksInLine)
	{
		ticks = ticksInLine;
		return this;
	}

	public bool Tick()
	{
		ticks++;
		return ticks < 456;
	}
}
