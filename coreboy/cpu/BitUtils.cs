namespace coreboy.cpu;

public static class BitUtils
{
	public static int GetMsb(int word)
	{
		return word >> 8;
	}

	public static int GetLsb(int word)
	{
		return word & 0xff;
	}

	public static int ToWord(int[] bytes)
	{
		return ToWord(bytes[1], bytes[0]);
	}

	public static int ToWord(int mostSigBit, int leastSigBit)
	{
		return (mostSigBit << 8) | leastSigBit;
	}

	public static bool GetBit(int byteValue, int position)
	{
		return (byteValue & (1 << position)) != 0;
	}

	public static int SetBit(int byteValue, int position, bool value)
	{
		if (value)
		{
			return SetBit(byteValue, position);
		}

		return ClearBit(byteValue, position);
	}

	public static int SetBit(int byteValue, int position)
	{
		return (byteValue | (1 << position)) & 0xff;
	}

	public static int ClearBit(int byteValue, int position)
	{
		return ~(1 << position) & byteValue & 0xff;
	}

	public static int ToSigned(int byteValue)
	{
		return (byteValue & (1 << 7)) == 0 ? byteValue : byteValue - 0x100;
	}
}
