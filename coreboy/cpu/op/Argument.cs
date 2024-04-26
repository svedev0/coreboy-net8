#nullable disable

using InvalidOpE = System.InvalidOperationException;

namespace coreboy.cpu.op;

public class Argument(
	string label, int operandLength, bool isMemory, DataType dataType)
{
	public string Label { get; } = label;
	public int OperandLength { get; } = operandLength;
	public bool IsMemory { get; } = isMemory;
	public DataType DataType { get; } = dataType;
	public static List<Argument> Values { get; }

	static Argument()
	{
		Values = [
			new Argument("A").Handle(
				(reg, addr, args) => reg.A,
				(reg, addr, i1, value) => reg.A = value),

			new Argument("B").Handle(
				(reg, addr, args) => reg.B,
				(reg, addr, i1, value) => reg.B = value),

			new Argument("C").Handle(
				(reg, addr, args) => reg.C,
				(reg, addr, i1, value) => reg.C = value),

			new Argument("D").Handle(
				(reg, addr, args) => reg.D,
				(reg, addr, i1, value) => reg.D = value),

			new Argument("E").Handle(
				(reg, addr, args) => reg.E,
				(reg, addr, i1, value) => reg.E = value),

			new Argument("H").Handle(
				(reg, addr, args) => reg.H,
				(reg, addr, i1, value) => reg.H = value),

			new Argument("L").Handle(
				(reg, addr, args) => reg.L,
				(reg, addr, i1, value) => reg.L = value),

			new Argument("AF", 0, false, DataType.D16).Handle(
				(reg, addr, args) => reg.AF,
				(reg, addr, i1, value) => reg.SetAf(value)),

			new Argument("BC", 0, false, DataType.D16).Handle(
				(reg, addr, args) => reg.BC,
				(reg, addr, i1, value) => reg.SetBc(value)),

			new Argument("DE", 0, false, DataType.D16).Handle(
				(reg, addr, args) => reg.DE,
				(reg, addr, i1, value) => reg.SetDe(value)),

			new Argument("HL", 0, false, DataType.D16).Handle(
				(reg, addr, args) => reg.HL,
				(reg, addr, i1, value) => reg.SetHl(value)),

			new Argument("SP", 0, false, DataType.D16).Handle(
				(reg, addr, args) => reg.SP,
				(reg, addr, i1, value) => reg.SP = value),

			new Argument("PC", 0, false, DataType.D16).Handle(
				(reg, addr, args) => reg.PC,
				(reg, addr, i1, value) => reg.PC = value),

			new Argument("d8", 1, false, DataType.D8).Handle(
				(reg, addr, args) => args[0],
				(reg, addr, i1, value) => throw new InvalidOpE("Unsupported")),

			new Argument("d16", 2, false, DataType.D16).Handle(
				(reg, addr, args) => BitUtils.ToWord(args),
				(reg, addr, i1, value) => throw new InvalidOpE("Unsupported")),

			new Argument("r8", 1, false, DataType.R8).Handle(
				(reg, addr, args) => BitUtils.ToSigned(args[0]),
				(reg, addr, i1, value) => throw new InvalidOpE("Unsupported")),

			new Argument("a16", 2, false, DataType.D16).Handle(
				(reg, addr, args) => BitUtils.ToWord(args),
				(reg, addr, i1, value) => throw new InvalidOpE("Unsupported")),
				
            // _BC
            new Argument("(BC)", 0, true, DataType.D8).Handle(
				(reg, addr, args) => addr.GetByte(reg.BC),
				(reg, addr, i1, value) => addr.SetByte(reg.BC, value)),

            // _DE
            new Argument("(DE)", 0, true, DataType.D8).Handle(
				(reg, addr, args) => addr.GetByte(reg.DE),
				(reg, addr, i1, value) => addr.SetByte(reg.DE, value)),

            // _HL
            new Argument("(HL)", 0, true, DataType.D8).Handle(
				(reg, addr, args) => addr.GetByte(reg.HL),
				(reg, addr, i1, value) => addr.SetByte(reg.HL, value)),

            // _a8
            new Argument("(a8)", 1, true, DataType.D8).Handle(
				(reg, addr, args) => addr.GetByte(0xff00 | args[0]),
				(reg, addr, i1, value) => addr.SetByte(0xff00 | i1[0], value)),

            // _a16
            new Argument("(a16)", 2, true, DataType.D8).Handle(
				(reg, addr, args) => addr.GetByte(BitUtils.ToWord(args)),
				(reg, addr, i1, value) => addr.SetByte(BitUtils.ToWord(i1), value)),

            // _C
            new Argument("(C)", 0, true, DataType.D8).Handle(
				(reg, addr, args) => addr.GetByte(0xff00 | reg.C),
				(reg, addr, i1, value) => addr.SetByte(0xff00 | reg.C, value))
		];
	}

	private Func<Registers, IAddressSpace, int[], int> _readFunc;
	private Action<Registers, IAddressSpace, int[], int> _writeAction;

	public Argument(string label) : this(label, 0, false, DataType.D8)
	{
	}

	public Argument Handle(
		Func<Registers, IAddressSpace, int[], int> readFunc,
		Action<Registers, IAddressSpace, int[], int> writeAction)
	{
		_readFunc = readFunc;
		_writeAction = writeAction;
		return this;
	}

	public int Read(Registers registers, IAddressSpace addressSpace, int[] args)
	{
		return _readFunc(registers, addressSpace, args);
	}

	public void Write(
		Registers registers, IAddressSpace addressSpace, int[] args, int value)
	{
		_writeAction(registers, addressSpace, args, value);
	}

	public static Argument Parse(string argument)
	{
		foreach (var arg in Values)
		{
			if (arg.Label.Equals(argument))
			{
				return arg;
			}
		}

		throw new ArgumentException($"Unknown argument: {argument}");
	}
}
