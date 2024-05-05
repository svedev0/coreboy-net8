namespace coreboy.sound;

public class VolumeEnvelope
{
	private int initialVolume;
	private int envelopeDirection;
	private int sweep;
	private int volume;
	private bool finished;
	private int index;

	public void SetNr2(int register)
	{
		initialVolume = register >> 4;

		if ((register & (1 << 3)) == 0)
		{
			envelopeDirection = -1;
		}
		else
		{
			envelopeDirection = 1;
		}

		sweep = register & 0b111;
	}

	public bool IsEnabled()
	{
		return sweep > 0;
	}

	public void Start()
	{
		finished = true;
		index = 8192;
	}

	public void Trigger()
	{
		index = 0;
		volume = initialVolume;
		finished = false;
	}

	public void Tick()
	{
		if (finished)
		{
			return;
		}

		if ((volume == 0 && envelopeDirection == -1) ||
			(volume == 15 && envelopeDirection == 1))
		{
			finished = true;
			return;
		}

		if (++index == sweep * Gameboy.TicksPerSec / 64)
		{
			index = 0;
			volume += envelopeDirection;
		}
	}

	public int GetVolume()
	{
		if (IsEnabled())
		{
			return volume;
		}
		else
		{
			return initialVolume;
		}
	}
}
