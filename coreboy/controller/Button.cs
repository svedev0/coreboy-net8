namespace coreboy.controller;

public class Button(int mask, int line)
{
	public static readonly Button Right = new(0x01, 0x10);
	public static readonly Button Left = new(0x02, 0x10);
	public static readonly Button Up = new(0x04, 0x10);
	public static readonly Button Down = new(0x08, 0x10);
	public static readonly Button A = new(0x01, 0x20);
	public static readonly Button B = new(0x02, 0x20);
	public static readonly Button Select = new(0x04, 0x20);
	public static readonly Button Start = new(0x08, 0x20);

	public int Mask { get; } = mask;
	public int Line { get; } = line;
}
