using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using coreboy.sound;

namespace coreboy.gui;

/// <summary>
/// Based on wcabus/gb-net6:
/// https://github.com/wcabus/gb-net6/blob/8ef708b6b6aa78665e23989cb9715c2f1789d419/GB.WinForms/OsSpecific/SoundOutput.cs
/// </summary>
public class WinSound : ISoundOutput
{
	private const int bufferSize = 1024;
	private const int sampleRate = 22050;

	private readonly byte[] _buffer = new byte[bufferSize];
	private int _i = 0;
	private int _tick;
	private AudioPlaybackEngine? _engine;

	public WinSound()
	{
	}

	public void Start()
	{
		_engine = new AudioPlaybackEngine(sampleRate, 2);
	}

	public void Stop()
	{
		_engine?.Dispose();
		_engine = null;
	}

	public void Play(int left, int right)
	{
		if (_tick++ != 0)
		{
			_tick %= Gameboy.TicksPerSec / sampleRate;
			return;
		}

		left = (int)(left * 0.25);
		right = (int)(right * 0.25);

		left = left < 0 ? 0 : left > 255 ? 255 : left;
		right = right < 0 ? 0 : right > 255 ? 255 : right;

		_buffer[_i++] = (byte)left;
		_buffer[_i++] = (byte)right;
		if (_i > bufferSize / 2)
		{
			_engine?.PlaySound(_buffer, 0, _i);
			_i = 0;
		}

		// Wait until engine is done playing current audio data
		while (_engine?.GetQueuedAudioLength() > bufferSize)
		{
			// Task.Delay(1) waits longer than 1 ms on Windows. Thread.Yield() is better.
			Thread.Yield();
		}
	}
}

public class AudioPlaybackEngine
{
	private readonly int _sampleRate;
	private readonly int _channelCount;
	private readonly WasapiOut _outputDevice;
	private readonly MixingSampleProvider _mixer;
	private readonly BufferedWaveProvider _bufferedWave;

	public AudioPlaybackEngine(int sampleRate, int channelCount)
	{
		_sampleRate = sampleRate;
		_channelCount = channelCount;

		WaveFormat mixerFormat = WaveFormat.CreateIeeeFloatWaveFormat(
			sampleRate, channelCount);

		_outputDevice = new WasapiOut();
		_mixer = new MixingSampleProvider(mixerFormat)
		{
			ReadFully = true
		};

		WaveFormat bufferedFormat = WaveFormat.CreateCustomFormat(
			tag: WaveFormatEncoding.Pcm,
			sampleRate: _sampleRate,
			channels: _channelCount,
			averageBytesPerSecond: _sampleRate,
			blockAlign: 8,
			bitsPerSample: 8);

		_bufferedWave = new BufferedWaveProvider(bufferedFormat)
		{
			ReadFully = true,
			DiscardOnBufferOverflow = true
		};

		AddMixerInput(_bufferedWave.ToSampleProvider());
		_outputDevice.Init(_mixer);
		_outputDevice.Play();
	}

	public int GetQueuedAudioLength()
	{
		return _bufferedWave.BufferedBytes;
	}

	public void PlaySound(byte[] buffer, int offset, int count)
	{
		_bufferedWave.AddSamples(buffer, offset, count);
	}

	private void AddMixerInput(ISampleProvider input)
	{
		_mixer.AddMixerInput(ConvertToRightChannelCount(input));
	}

	private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
	{
		if (input.WaveFormat.Channels == _channelCount)
		{
			return input;
		}

		if (input.WaveFormat.Channels == 1 && _channelCount == 2)
		{
			return new MonoToStereoSampleProvider(input);
		}

		throw new NotImplementedException("Unimplemented channel count");
	}

	public void Dispose()
	{
		_outputDevice.Stop();
		_outputDevice.Dispose();
	}
}
