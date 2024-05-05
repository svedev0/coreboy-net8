namespace coreboy.sound;

public class SoundMode1(bool gbc) : SoundModeBase(0xff10, 64, gbc)
{
	private readonly FrequencySweep _frequencySweep = new();
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
		_frequencySweep.Start();
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
		env = UpdateSweep() && env;
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

	protected override void SetNr0(int value)
	{
		base.SetNr0(value);
		_frequencySweep.SetNr10(value);
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

	protected override void SetNr3(int value)
	{
		base.SetNr3(value);
		_frequencySweep.SetNr13(value);
	}

	protected override void SetNr4(int value)
	{
		base.SetNr4(value);
		_frequencySweep.SetNr14(value);
	}

	protected override int GetNr3()
	{
		return _frequencySweep.GetNr13();
	}

	protected override int GetNr4()
	{
		return (base.GetNr4() & 0b11111000) |
			(_frequencySweep.GetNr14() & 0b00000111);
	}

	private int GetDuty()
	{
		return (GetNr1() >> 6) switch
		{
			0 => 0b00000001,
			1 => 0b10000001,
			2 => 0b10000111,
			3 => 0b01111110,
			_ => throw new InvalidOperationException("Illegal state exception"),
		};
	}

	private void ResetFreqDivider()
	{
		freqDivider = GetFrequency() * 4;
	}

	protected bool UpdateSweep()
	{
		_frequencySweep.Tick();

		if (ChannelEnabled && !_frequencySweep.IsEnabled())
		{
			ChannelEnabled = false;
		}

		return ChannelEnabled;
	}
}
