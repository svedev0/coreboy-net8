namespace coreboy.sound;

public class SoundMode2(bool gbc) : SoundModeBase(0xff15, 64, gbc)
{
	private readonly VolumeEnvelope _volumeEnvelope = new();
	private int freqDivider;
	private int lastOutput;
	private int index;

	public override void Start()
	{
		index = 0;

		if (Gbc)
		{
			Length.Reset();
		}

		Length.Start();
		_volumeEnvelope.Start();
	}

	protected override void Trigger()
	{
		index = 0;
		freqDivider = 1;
		_volumeEnvelope.Trigger();
	}

	public override int Tick()
	{
		_volumeEnvelope.Tick();

		bool env = UpdateLength();
		env = DacEnabled && env;

		if (!env)
		{
			return 0;
		}

		freqDivider--;

		if (freqDivider == 0)
		{
			ResetFreqDivider();
			lastOutput = (GetDuty() & (1 << index)) >> index;
			index = (index + 1) % 8;
		}

		return lastOutput * _volumeEnvelope.GetVolume();
	}

	protected override void SetNr1(int value)
	{
		base.SetNr1(value);
		Length.SetLength(64 - (value & 0b00111111));
	}

	protected override void SetNr2(int value)
	{
		base.SetNr2(value);
		_volumeEnvelope.SetNr2(value);
		DacEnabled = (value & 0b11111000) != 0;
		ChannelEnabled &= DacEnabled;
	}

	private int GetDuty()
	{
		int i = GetNr1() >> 6;

		return i switch
		{
			0 => 0b00000001,
			1 => 0b10000001,
			2 => 0b10000111,
			3 => 0b01111110,
			_ => throw new InvalidOperationException("Illegal operation")
		};
	}

	private void ResetFreqDivider()
	{
		freqDivider = GetFrequency() * 4;
	}
}
