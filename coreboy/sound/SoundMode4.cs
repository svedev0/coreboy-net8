namespace coreboy.sound;

public class SoundMode4(bool gbc) : SoundModeBase(0xff1f, 64, gbc)
{
	private readonly VolumeEnvelope _volumeEnvelope = new();
	private readonly PolynomialCounter _polynomialCounter = new();
	private readonly Lfsr _lfsr = new();
	private int lastResult;

	public override void Start()
	{
		if (Gbc)
		{
			Length.Reset();
		}

		Length.Start();
		_lfsr.Start();
		_volumeEnvelope.Start();
	}

	protected override void Trigger()
	{
		_lfsr.Reset();
		_volumeEnvelope.Trigger();
	}

	public override int Tick()
	{
		_volumeEnvelope.Tick();

		if (!UpdateLength())
		{
			return 0;
		}

		if (!DacEnabled)
		{
			return 0;
		}

		if (_polynomialCounter.Tick())
		{
			lastResult = _lfsr.NextBit((Nr3 & (1 << 3)) != 0);
		}

		return lastResult * _volumeEnvelope.GetVolume();
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
		_polynomialCounter.SetNr43(value);
	}
}
