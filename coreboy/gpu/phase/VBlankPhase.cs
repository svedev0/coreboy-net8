namespace coreboy.gpu.phase;

public class VBlankPhase : IGpuPhase
{
	private int ticks;

	public VBlankPhase Start()
	{
		ticks = 0;
		return this;
	}

	public bool Tick()
	{
		return ticks++ < 456;
	}
}
