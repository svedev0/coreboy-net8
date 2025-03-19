namespace coreboy.serial;

public class NullSerialEndpoint : ISerialEndpoint
{
	public int Transfer(int outgoing)
	{
		return 0;
	}
}
