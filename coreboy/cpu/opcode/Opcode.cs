using coreboy.cpu.op;

namespace coreboy.cpu.opcode;

public class Opcode
{
	public int Value { get; }
	public string Label { get; }
	public List<Op> Ops { get; }
	public int Length { get; }

	public Opcode(OpcodeBuilder builder)
	{
		Value = builder.GetOpcode();
		Label = builder.GetLabel();
		Ops = builder.GetOps();

		if (Ops.Count <= 0)
		{
			Length = 0;
		}
		else
		{
			Length = Ops.Max(o => o.OperandLength());
		}
	}

	public override string ToString()
	{
		return $"{Value:X2} {Label}";
	}
}
