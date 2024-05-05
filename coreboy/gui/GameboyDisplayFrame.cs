using System.Runtime.CompilerServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace coreboy.gui;

public class GameboyDisplayFrame(int[] pixels)
{
	public static readonly int DisplayWidth = 160;
	public static readonly int DisplayHeight = 144;

	private readonly int[] _pixels = pixels;

	public IEnumerable<int[]> Rows()
	{
		int offset = 0;

		for (int row = 0; row < DisplayHeight; row++)
		{
			int[] currentRow = new int[DisplayWidth];
			Array.Copy(_pixels, offset, currentRow, 0, 160);
			yield return currentRow;
			offset += 160;
		}
	}

	public byte[] ToBitmap()
	{
		Image<Rgba32> pixels = new(DisplayWidth, DisplayHeight);

		int x = 0;
		int y = 0;

		foreach (int pixel in _pixels)
		{
			if (x == DisplayWidth)
			{
				x = 0;
				y++;
			}

			(int r, int g, int b) = pixel.ToRgb();
			pixels[x, y] = new Rgba32((byte)r, (byte)g, (byte)b, 255);
			x++;
		}

		using MemoryStream memoryStream = new();
		pixels.SaveAsBmp(memoryStream);
		pixels.Dispose();
		return memoryStream.ToArray();
	}
}

public static class GameboyDisplayFrameHelperExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (int, int, int) ToRgb(this int pixel)
	{
		var b = pixel & 255;
		var g = (pixel >> 8) & 255;
		var r = (pixel >> 16) & 255;
		return (r, g, b);
	}
}
