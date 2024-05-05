using coreboy.memory;

namespace coreboy.gpu;

public class GpuRegister(int address, RegisterType type) : IRegister
{
	public static readonly GpuRegister Stat = new(0xff41, RegisterType.RW);
	public static readonly GpuRegister Scy = new(0xff42, RegisterType.RW);
	public static readonly GpuRegister Scx = new(0xff43, RegisterType.RW);
	public static readonly GpuRegister Ly = new(0xff44, RegisterType.R);
	public static readonly GpuRegister Lyc = new(0xff45, RegisterType.RW);
	public static readonly GpuRegister Bgp = new(0xff47, RegisterType.RW);
	public static readonly GpuRegister Obp0 = new(0xff48, RegisterType.RW);
	public static readonly GpuRegister Obp1 = new(0xff49, RegisterType.RW);
	public static readonly GpuRegister Wy = new(0xff4a, RegisterType.RW);
	public static readonly GpuRegister Wx = new(0xff4b, RegisterType.RW);
	public static readonly GpuRegister Vbk = new(0xff4f, RegisterType.W);

	public int Address { get; } = address;
	public RegisterType Type { get; } = type;

	public static IEnumerable<IRegister> Values()
	{
		yield return Stat;
		yield return Scy;
		yield return Scx;
		yield return Ly;
		yield return Lyc;
		yield return Bgp;
		yield return Obp0;
		yield return Obp1;
		yield return Wy;
		yield return Wx;
		yield return Vbk;
	}
}
