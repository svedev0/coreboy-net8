namespace coreboy.gpu;

public class NullDisplay : IDisplay
{
	public bool Enabled { get; set; }
	public event FrameProducedEventHandler OnFrameProduced = delegate { };

	public void PutDmgPixel(int color)
	{
	}

	public void PutColorPixel(int gbcRgb)
	{
	}

	public void RequestRefresh()
	{
	}

	public void WaitForRefresh()
	{
	}

	public void Run(CancellationToken token)
	{
	}
}
