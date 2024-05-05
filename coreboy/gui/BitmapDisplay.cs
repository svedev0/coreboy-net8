#nullable disable

using coreboy.gpu;

namespace coreboy.gui;

public class BitmapDisplay : IDisplay
{
	public static readonly int DisplayWidth = 160;
	public static readonly int DisplayHeight = 144;
	public static readonly int[] Colors = [0xe6f8da, 0x99c886, 0x437969, 0x051f2a];

	public bool Enabled { get; set; }
	public event FrameProducedEventHandler OnFrameProduced;

	private readonly int[] _rgb;
	private bool _doRefresh;
	private int _index;

	private readonly object _lockObject = new();

	public BitmapDisplay()
	{
		_rgb = new int[DisplayWidth * DisplayHeight];
	}

	public void PutDmgPixel(int color)
	{
		_rgb[_index++] = Colors[color];
		_index %= _rgb.Length;
	}

	public void PutColorPixel(int gbcRgb)
	{
		_rgb[_index++] = TranslateGbcRgb(gbcRgb);
	}

	public static int TranslateGbcRgb(int gbcRgb)
	{
		var r = (gbcRgb >> 0) & 0x1f;
		var g = (gbcRgb >> 5) & 0x1f;
		var b = (gbcRgb >> 10) & 0x1f;
		var result = (r * 8) << 16;
		result |= (g * 8) << 8;
		result |= (b * 8) << 0;
		return result;
	}

	public void RequestRefresh()
	{
		SetRefreshFlag(true);
	}

	public void WaitForRefresh()
	{
		while (_doRefresh)
		{
			Task.Delay(1).Wait();
		}
	}

	public void Run(CancellationToken token)
	{
		SetRefreshFlag(false);

		Enabled = true;

		while (!token.IsCancellationRequested)
		{
			if (!_doRefresh)
			{
				Task.Delay(1, token).Wait(token);
				continue;
			}

			RefreshScreen();
			SetRefreshFlag(false);
		}
	}

	private void RefreshScreen()
	{
		GameboyDisplayFrame frame = new(_rgb);
		byte[] bytes = frame.ToBitmap();

		OnFrameProduced?.Invoke(this, bytes);
		_index = 0;
	}

	private void SetRefreshFlag(bool flag)
	{
		lock (_lockObject)
		{
			_doRefresh = flag;
		}
	}
}
