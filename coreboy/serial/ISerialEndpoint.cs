namespace coreboy.serial;

public interface ISerialEndpoint
{
	int Transfer(int outgoing);
}


public class NullSerialEndpoint : ISerialEndpoint
{
	public int Transfer(int outgoing)
	{
		return 0;
	}
}
