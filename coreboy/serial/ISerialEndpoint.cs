namespace coreboy.serial
{
	public interface ISerialEndpoint
	{
		int transfer(int outgoing);
	}


	public class NullSerialEndpoint : ISerialEndpoint
	{
		public int transfer(int outgoing)
		{
			return 0;
		}
	}
}