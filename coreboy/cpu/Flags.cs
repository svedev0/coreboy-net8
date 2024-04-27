using System.Text;
using static coreboy.cpu.BitUtils;

namespace coreboy.cpu;

public class Flags
{
	public int FlagsByte { get; private set; }

	private static readonly int Z_POS = 7;
	private static readonly int N_POS = 6;
	private static readonly int H_POS = 5;
	private static readonly int C_POS = 4;

	public bool IsZ()
	{
		return GetBit(FlagsByte, Z_POS);
	}

	public bool IsN()
	{
		return GetBit(FlagsByte, N_POS);
	}

	public bool IsH()
	{
		return GetBit(FlagsByte, H_POS);
	}

	public bool IsC()
	{
		return GetBit(FlagsByte, C_POS);
	}

	public void SetZ(bool z)
	{
		FlagsByte = SetBit(FlagsByte, Z_POS, z);
	}

	public void SetN(bool n)
	{
		FlagsByte = SetBit(FlagsByte, N_POS, n);
	}

	public void SetH(bool h)
	{
		FlagsByte = SetBit(FlagsByte, H_POS, h);
	}

	public void SetC(bool c)
	{
		FlagsByte = SetBit(FlagsByte, C_POS, c);
	}

	public void SetFlagsByte(int flags)
	{
		FlagsByte = flags & 0xf0;
	}

	public override string ToString()
	{
		StringBuilder result = new();

		result.Append(IsZ() ? 'Z' : '-');
		result.Append(IsN() ? 'N' : '-');
		result.Append(IsH() ? 'H' : '-');
		result.Append(IsC() ? 'C' : '-');
		result.Append("----");
		return result.ToString();
	}
}
