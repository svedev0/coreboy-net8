namespace coreboy.serial;

public interface ISerialEndpoint
{
	int Transfer(int outgoing);
}
