using NAudio.Wave;
using coreboy.sound;

namespace coreboy.gui;

public class AudioSystemSoundOutput : ISoundOutput
{
	private const int sampleRate = 22050;
	private const int bufferSize = 1024;

	private static readonly WaveFormat _format = new(sampleRate, 8, 2);

	private BufferedWaveProvider? bufferedWaveProvider;
	private WaveOutEvent? waveOut;

	private byte[]? buffer;
	private int bufferIndex;
	private int tick;

	public void Start()
	{
		if (waveOut != null)
		{
			return;
		}

		try
		{
			waveOut = new WaveOutEvent();
			bufferedWaveProvider = new BufferedWaveProvider(_format)
			{
				BufferLength = bufferSize * 4,
				DiscardOnBufferOverflow = true
			};
			waveOut.Init(bufferedWaveProvider);
		}
		catch
		{
			throw new InvalidOperationException("Failed to start sound output");
		}

		waveOut.Play();
		buffer = new byte[bufferSize];
	}

	public void Stop()
	{
		if (waveOut == null)
		{
			return;
		}

		waveOut.Stop();
		waveOut.Dispose();
		waveOut = null;
	}

	public void Play(int left, int right)
	{
		tick++;

		if (tick != 0)
		{
			tick %= Gameboy.TicksPerSec / sampleRate;
			return;
		}

		ValidateRange(left, 0, 255);
		ValidateRange(right, 0, 255);

		if (buffer is null)
		{
			return;
		}

		buffer[bufferIndex++] = (byte)left;
		buffer[bufferIndex++] = (byte)right;

		if (bufferIndex >= bufferSize / 2)
		{
			bufferedWaveProvider?.AddSamples(buffer, 0, bufferIndex);
			bufferIndex = 0;
		}
	}

	private static void ValidateRange(int value, int min, int max)
	{
		if (value < min || value > max)
		{
			throw new ArgumentException("Value is out of range");
		}
	}
}
