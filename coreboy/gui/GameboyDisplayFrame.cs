using System.Runtime.CompilerServices;

namespace coreboy.gui;

public class GameboyDisplayFrame(int[] pixels)
{
	public const int DisplayWidth = 160;
	public const int DisplayHeight = 144;

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
		RgbaPixel[,] pixels = new RgbaPixel[DisplayWidth, DisplayHeight];

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
			pixels[x, y] = new RgbaPixel(r, g, b, 255);
			x++;
		}

		return BmpEncoder.EncodeBmp(pixels);
	}
}

public static class GameboyDisplayFrameHelperExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (int R, int G, int B) ToRgb(this int pixel)
	{
		int b = pixel & 255;
		int g = (pixel >> 8) & 255;
		int r = (pixel >> 16) & 255;
		return (r, g, b);
	}
}
