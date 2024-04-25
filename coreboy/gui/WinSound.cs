using coreboy.sound;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace coreboy.gui
{

	public class WinSound : ISoundOutput
	{
		private int _tick;
		private readonly int _divider;
		private AudioPlaybackEngine? _engine;

		private const int SampleRate = 22050;

		public WinSound()
		{
			_divider = Gameboy.TicksPerSec / SampleRate;
		}

		public void Start()
		{
			try
			{
				_engine = new AudioPlaybackEngine(SampleRate, 2);
			}
			catch
			{
				// ignored
			}
		}

		public void Stop()
		{
			_engine?.Dispose();
		}

		public void Play(int left, int right)
		{
			if (_tick++ != 0)
			{
				_tick %= _divider;
				return;
			}

			// Beep((uint)left, 5);*/
		}
	}

	public class AudioPlaybackEngine : IDisposable
	{
		private readonly IWavePlayer _outputDevice;
		private readonly MixingSampleProvider _mixer;

		public AudioPlaybackEngine(int sampleRate = 44100, int channelCount = 2)
		{
			_outputDevice = new WaveOutEvent();
			_mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount))
			{
				ReadFully = true
			};

			_outputDevice.Init(_mixer);
			_outputDevice.Play();
		}

		public void PlaySound(string fileName)
		{
			var input = new AudioFileReader(fileName);
			AddMixerInput(new AutoDisposeFileReader(input));
		}

		public void PlaySound(CachedSound sound)
		{
			AddMixerInput(new CachedSoundSampleProvider(sound));
		}

		private void AddMixerInput(ISampleProvider input)
		{
			_mixer.AddMixerInput(ConvertToRightChannelCount(input));
		}

		private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
		{
			if (input.WaveFormat.Channels == _mixer.WaveFormat.Channels)
			{
				return input;
			}
			if (input.WaveFormat.Channels == 1 && _mixer.WaveFormat.Channels == 2)
			{
				return new MonoToStereoSampleProvider(input);
			}
			throw new NotImplementedException("Not yet implemented this channel count conversion");
		}

		public void Dispose()
		{
			_outputDevice.Dispose();
		}
	}

	public class AutoDisposeFileReader(AudioFileReader reader) : ISampleProvider
	{
		private readonly AudioFileReader _reader = reader;
		private bool _isDisposed;

		public int Read(float[] buffer, int offset, int count)
		{
			if (_isDisposed)
				return 0;

			var read = _reader.Read(buffer, offset, count);
			if (read == 0)
			{
				_reader.Dispose();
				_isDisposed = true;
			}
			return read;
		}

		public WaveFormat WaveFormat { get; private set; } = reader.WaveFormat;
	}
	public class CachedSoundSampleProvider(CachedSound cachedSound) : ISampleProvider
	{
		private readonly CachedSound _cachedSound = cachedSound;
		private long _position;

		public int Read(float[] buffer, int offset, int count)
		{
			var availableSamples = _cachedSound.AudioData.Length - _position;
			var samplesToCopy = Math.Min(availableSamples, count);
			Array.Copy(_cachedSound.AudioData, _position, buffer, offset, samplesToCopy);
			_position += samplesToCopy;
			return (int)samplesToCopy;
		}

		public WaveFormat WaveFormat { get { return _cachedSound.WaveFormat; } }
	}

	public class CachedSound
	{
		public float[] AudioData { get; private set; }
		public WaveFormat WaveFormat { get; private set; }
		public CachedSound(string audioFileName)
		{
			using var audioFileReader = new AudioFileReader(audioFileName);
			// TODO: could add resampling in here if required
			WaveFormat = audioFileReader.WaveFormat;
			var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
			var readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
			int samplesRead;
			while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
			{
				wholeFile.AddRange(readBuffer.Take(samplesRead));
			}
			AudioData = [.. wholeFile];
		}
	}
}
