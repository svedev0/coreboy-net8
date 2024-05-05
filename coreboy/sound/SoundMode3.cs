using coreboy.memory;

namespace coreboy.sound;

public class SoundMode3 : SoundModeBase
{
	private static readonly int[] dmgWave = [
		0x84, 0x40, 0x43, 0xaa, 0x2d, 0x78, 0x92, 0x3c,
		0x60, 0x59, 0x59, 0xb0, 0x34, 0xb8, 0x2e, 0xda
	];

	private static readonly int[] gbcWave = [
		0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff,
		0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff
	];

	private readonly Ram _waveRam = new(0xff30, 0x10);

	private int freqDivider;
	private int lastOutput;
	private int index;
	private int ticksSinceRead = 65536;
	private int lastReadAddress;
	private int buffer;
	private bool triggered;

	public SoundMode3(bool gbc) : base(0xff1a, 256, gbc)
	{
		foreach (var v in gbc ? gbcWave : dmgWave)
		{
			_waveRam.SetByte(0xff30, v);
		}
	}

	public override bool Accepts(int address)
	{
		return _waveRam.Accepts(address) || base.Accepts(address);
	}

	public override int GetByte(int address)
	{
		if (!_waveRam.Accepts(address))
		{
			return base.GetByte(address);
		}

		if (!IsEnabled())
		{
			return _waveRam.GetByte(address);
		}

		if (_waveRam.Accepts(lastReadAddress) && (Gbc || ticksSinceRead < 2))
		{
			return _waveRam.GetByte(lastReadAddress);
		}

		return 0xff;
	}

	public override void SetByte(int address, int value)
	{
		if (!_waveRam.Accepts(address))
		{
			base.SetByte(address, value);
			return;
		}

		if (!IsEnabled())
		{
			_waveRam.SetByte(address, value);
		}
		else if (_waveRam.Accepts(lastReadAddress) && (Gbc || ticksSinceRead < 2))
		{
			_waveRam.SetByte(lastReadAddress, value);
		}
	}

	protected override void SetNr0(int value)
	{
		base.SetNr0(value);
		DacEnabled = (value & (1 << 7)) != 0;
		ChannelEnabled &= DacEnabled;
	}

	protected override void SetNr1(int value)
	{
		base.SetNr1(value);
		Length.SetLength(256 - value);
	}

	protected override void SetNr4(int value)
	{
		if (!Gbc && (value & (1 << 7)) != 0)
		{
			if (IsEnabled() && freqDivider == 2)
			{
				int pos = index / 2;

				if (pos < 4)
				{
					_waveRam.SetByte(0xff30, _waveRam.GetByte(0xff30 + pos));
				}
				else
				{
					pos &= ~3;

					for (var j = 0; j < 4; j++)
					{
						_waveRam.SetByte(
							0xff30 + j,
							_waveRam.GetByte(0xff30 + ((pos + j) % 0x10)));
					}
				}
			}
		}

		base.SetNr4(value);
	}

	public override void Start()
	{
		index = 0;
		buffer = 0;

		if (Gbc)
		{
			Length.Reset();
		}

		Length.Start();
	}

	protected override void Trigger()
	{
		index = 0;
		freqDivider = 6;
		triggered = !Gbc;

		if (Gbc)
		{
			GetWaveEntry();
		}
	}

	public override int Tick()
	{
		ticksSinceRead++;

		if (!UpdateLength())
		{
			return 0;
		}

		if (!DacEnabled)
		{
			return 0;
		}

		if ((GetNr0() & (1 << 7)) == 0)
		{
			return 0;
		}

		freqDivider--;

		if (freqDivider == 0)
		{
			ResetFreqDivider();

			if (triggered)
			{
				lastOutput = (buffer >> 4) & 0x0f;
				triggered = false;
			}
			else
			{
				lastOutput = GetWaveEntry();
			}

			index = (index + 1) % 32;
		}

		return lastOutput;
	}

	private int GetVolume()
	{
		return (GetNr2() >> 5) & 0b11;
	}

	private int GetWaveEntry()
	{
		ticksSinceRead = 0;
		lastReadAddress = 0xff30 + index / 2;
		buffer = _waveRam.GetByte(lastReadAddress);

		int b = buffer;

		if (index % 2 == 0)
		{
			b = (b >> 4) & 0x0f;
		}
		else
		{
			b &= 0x0f;
		}

		return GetVolume() switch
		{
			0 => 0,
			1 => b,
			2 => b >> 1,
			3 => b >> 2,
			_ => throw new InvalidOperationException("Illegal state")
		};
	}

	private void ResetFreqDivider()
	{
		freqDivider = GetFrequency() * 2;
	}
}
