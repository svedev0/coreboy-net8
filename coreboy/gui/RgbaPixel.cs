namespace coreboy.gui;

public struct RgbaPixel
{
	public byte R;
	public byte G;
	public byte B;
	public byte A;

	public RgbaPixel(byte r, byte g, byte b, byte a)
	{
		R = r;
		G = g;
		B = b;
		A = a;
	}

	public RgbaPixel(int r, int g, int b, int a)
	{
		R = (byte)r;
		G = (byte)g;
		B = (byte)b;
		A = (byte)a;
	}
}
