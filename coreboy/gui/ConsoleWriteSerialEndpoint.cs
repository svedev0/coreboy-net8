using coreboy.serial;

namespace coreboy.gui;

public class ConsoleWriteSerialEndpoint : ISerialEndpoint
{
	public int Transfer(int b)
	{
		Console.Write((char)b);
		return 0;
	}
}
