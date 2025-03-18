namespace coreboy.gui;

public class BmpEncoder
{
	private const int bmpHeaderSize = 14; // BMP file header size (bytes)
	private const int dibHeaderSize = 40; // BITMAPINFOHEADER size (bytes)
	private const int dataOffset = bmpHeaderSize + dibHeaderSize;

	private static byte[] data = [];

	// See BMP file format specification for more details:
	// https://www.ece.ualberta.ca/~elliott/ee552/studentAppNotes/2003_w/misc/bmp_file_format/bmp_file_format.htm
	public static byte[] EncodeBmp(RgbaPixel[,] pixels)
	{
		ArgumentNullException.ThrowIfNull(pixels);

		int width = pixels.GetLength(0);
		int height = pixels.GetLength(1);
		short bitsPerPixel = 32;
		int resolutionPpm = 2835; // 2835 PPM ≈ 72 DPI

		int dataSize = width * height * (bitsPerPixel / sizeof(byte));
		int fileSize = dataOffset + dataSize;

		data = new byte[fileSize];

		// Header (BMP file header)
		CopyBytes("BM", 0);        // Signature
		CopyBytes(fileSize, 2);    // File size (little-endian)
		CopyBytes(0, 6);           // Reserved field (4 bytes)
		CopyBytes(dataOffset, 10); // Data offset

		// Info header (DIB header)
		CopyBytes(dibHeaderSize, 14); // Info header size
		CopyBytes(width, 18);         // Bitmap width
		CopyBytes(-height, 22);       // Bitmap height (negative indicates top-down bitmap)
		CopyBytes(1, 26);             // Planes (always 1)
		CopyBytes(bitsPerPixel, 28);  // Bits per pixel
		CopyBytes(0, 30);             // Compression (0 = BI_RGB, no compression)
		CopyBytes(dataSize, 34);      // Pixel data size
		CopyBytes(resolutionPpm, 38); // Horizontal resolution
		CopyBytes(resolutionPpm, 42); // Vertical resolution
		CopyBytes(0, 46);             // Number of colors used (0 == default)
		CopyBytes(0, 50);             // Important colors (0 == all are important)

		// Pixel data
		int offset = dataOffset;
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				RgbaPixel pixel = pixels[x, y];

				// Pixel data is in BGRA format for a 32-bit bitmap
				data[offset++] = pixel.B;
				data[offset++] = pixel.G;
				data[offset++] = pixel.R;
				data[offset++] = pixel.A;
			}
		}

		return data;
	}

	private static void CopyBytes(int value, int index)
	{
		BitConverter.GetBytes(value).CopyTo(data, index);
	}

	private static void CopyBytes(short value, int index)
	{
		BitConverter.GetBytes(value).CopyTo(data, index);
	}

	private static void CopyBytes(string value, int index)
	{
		for (int i = 0; i < value.Length; i++)
		{
			CopyBytes((short)value[i], index + i);
		}
	}
}
