#nullable disable

using coreboy.cpu.opcode;

using InvalidOpE = System.InvalidOperationException;

namespace coreboy.cpu;

public class Opcodes
{
	public List<Opcode> Commands { get; }
	public List<Opcode> ExtCommands { get; }

	public Opcodes()
	{
		var opcodes = new OpcodeBuilder[0x100];
		var extOpcodes = new OpcodeBuilder[0x100];

		RegCmd(opcodes, 0x00, "NOP");

		var regKvps01 = OpcodesForValues(0x01, 0x10, "BC", "DE", "HL", "SP");
		foreach (var kvp in regKvps01)
		{
			RegLoad(opcodes, kvp.Key, kvp.Value, "d16");
		}

		var regKvps02 = OpcodesForValues(0x02, 0x10, "(BC)", "(DE)");
		foreach (var kvp in regKvps02)
		{
			RegLoad(opcodes, kvp.Key, kvp.Value, "A");
		}

		var regKvps03 = OpcodesForValues(0x03, 0x10, "BC", "DE", "HL", "SP");
		foreach (var kvp in regKvps03)
		{
			RegCmd(opcodes, kvp, "INC {}")
				.Load(kvp.Value)
				.Alu("INC")
				.Store(kvp.Value);
		}

		var regKvps04 = OpcodesForValues(
			0x04, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A");
		foreach (var kvp in regKvps04)
		{
			RegCmd(opcodes, kvp, "INC {}")
				.Load(kvp.Value)
				.Alu("INC")
				.Store(kvp.Value);
		}

		var regKvps05 = OpcodesForValues(
			0x05, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A");
		foreach (var kvp in regKvps05)
		{
			RegCmd(opcodes, kvp, "DEC {}")
				.Load(kvp.Value)
				.Alu("DEC")
				.Store(kvp.Value);
		}

		var regKvps06 = OpcodesForValues(
			0x06, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A");
		foreach (var kvp in regKvps06)
		{
			RegLoad(opcodes, kvp.Key, kvp.Value, "d8");
		}

		var regKvps07 = OpcodesForValues(0x07, 0x08, "RLC", "RRC", "RL", "RR");
		foreach (var kvp in regKvps07)
		{
			RegCmd(opcodes, kvp, kvp.Value + "A")
				.Load("A")
				.Alu(kvp.Value)
				.ClearZ()
				.Store("A");
		}

		RegLoad(opcodes, 0x08, "(a16)", "SP");

		var regKvps09 = OpcodesForValues(0x09, 0x10, "BC", "DE", "HL", "SP");
		foreach (var kvp in regKvps09)
		{
			RegCmd(opcodes, kvp, "ADD HL,{}")
				.Load("HL")
				.Alu("ADD", kvp.Value)
				.Store("HL");
		}

		var regKvps0a = OpcodesForValues(0x0a, 0x10, "(BC)", "(DE)");
		foreach (var kvp in regKvps0a)
		{
			RegLoad(opcodes, kvp.Key, "A", kvp.Value);
		}

		var regKvps0b = OpcodesForValues(0x0b, 0x10, "BC", "DE", "HL", "SP");
		foreach (var kvp in regKvps0b)
		{
			RegCmd(opcodes, kvp, "DEC {}")
				.Load(kvp.Value)
				.Alu("DEC")
				.Store(kvp.Value);
		}

		RegCmd(opcodes, 0x10, "STOP");

		RegCmd(opcodes, 0x18, "JR r8")
			.Load("PC")
			.Alu("ADD", "r8")
			.Store("PC");

		var regKvps20 = OpcodesForValues(0x20, 0x08, "NZ", "Z", "NC", "C");
		foreach (var kvp in regKvps20)
		{
			RegCmd(opcodes, kvp, "JR {},r8")
				.Load("PC")
				.ProceedIf(kvp.Value)
				.Alu("ADD", "r8")
				.Store("PC");
		}

		RegCmd(opcodes, 0x22, "LD (HL+),A")
			.CopyByte("(HL)", "A")
			.AluHL("INC");

		RegCmd(opcodes, 0x2a, "LD A,(HL+)")
			.CopyByte("A", "(HL)")
			.AluHL("INC");

		RegCmd(opcodes, 0x27, "DAA")
			.Load("A")
			.Alu("DAA")
			.Store("A");

		RegCmd(opcodes, 0x2f, "CPL")
			.Load("A")
			.Alu("CPL")
			.Store("A");

		RegCmd(opcodes, 0x32, "LD (HL-),A")
			.CopyByte("(HL)", "A")
			.AluHL("DEC");

		RegCmd(opcodes, 0x3a, "LD A,(HL-)")
			.CopyByte("A", "(HL)")
			.AluHL("DEC");

		RegCmd(opcodes, 0x37, "SCF")
			.Load("A")
			.Alu("SCF")
			.Store("A");

		RegCmd(opcodes, 0x3f, "CCF")
			.Load("A")
			.Alu("CCF")
			.Store("A");

		var regKvps40 = OpcodesForValues(
			0x40, 0x08, "B", "C", "D", "E", "H", "L", "(HL)", "A");
		foreach (var kvp in regKvps40)
		{
			var regKvps = OpcodesForValues(
				kvp.Key, 0x01, "B", "C", "D", "E", "H", "L", "(HL)", "A");
			foreach (var kvp2 in regKvps)
			{
				if (kvp2.Key == 0x76)
				{
					continue;
				}

				RegLoad(opcodes, kvp2.Key, kvp.Value, kvp2.Value);
			}
		}

		RegCmd(opcodes, 0x76, "HALT");

		var regKvps80 = OpcodesForValues(
			0x80, 0x08, "ADD", "ADC", "SUB", "SBC", "AND", "XOR", "OR", "CP");
		foreach (var kvp in regKvps80)
		{
			var regKvps = OpcodesForValues(
				kvp.Key, 0x01, "B", "C", "D", "E", "H", "L", "(HL)", "A");
			foreach (var kvp2 in regKvps)
			{
				RegCmd(opcodes, kvp2, kvp.Value + " {}")
					.Load("A")
					.Alu(kvp.Value, kvp2.Value)
					.Store("A");
			}
		}

		var regKvpsC0 = OpcodesForValues(0xc0, 0x08, "NZ", "Z", "NC", "C");
		foreach (var kvp in regKvpsC0)
		{
			RegCmd(opcodes, kvp, "RET {}")
				.ExtraCycle()
				.ProceedIf(kvp.Value)
				.Pop()
				.ForceFinish()
				.Store("PC");
		}

		var regKvpsC1 = OpcodesForValues(0xc1, 0x10, "BC", "DE", "HL", "AF");
		foreach (var kvp in regKvpsC1)
		{
			RegCmd(opcodes, kvp, "POP {}")
				.Pop()
				.Store(kvp.Value);
		}

		var regKvpsC2 = OpcodesForValues(0xc2, 0x08, "NZ", "Z", "NC", "C");
		foreach (var kvp in regKvpsC2)
		{
			RegCmd(opcodes, kvp, "JP {},a16")
				.Load("a16")
				.ProceedIf(kvp.Value)
				.Store("PC")
				.ExtraCycle();
		}

		RegCmd(opcodes, 0xc3, "JP a16")
			.Load("a16")
			.Store("PC")
			.ExtraCycle();

		var regKvpsC4 = OpcodesForValues(0xc4, 0x08, "NZ", "Z", "NC", "C");
		foreach (var kvp in regKvpsC4)
		{
			RegCmd(opcodes, kvp, "CALL {},a16")
				.ProceedIf(kvp.Value)
				.ExtraCycle()
				.Load("PC")
				.Push()
				.Load("a16")
				.Store("PC");
		}

		var regKvpsC5 = OpcodesForValues(0xc5, 0x10, "BC", "DE", "HL", "AF");
		foreach (var kvp in regKvpsC5)
		{
			RegCmd(opcodes, kvp, "PUSH {}")
				.ExtraCycle()
				.Load(kvp.Value)
				.Push();
		}

		var regKvpsC6 = OpcodesForValues(
			0xc6, 0x08, "ADD", "ADC", "SUB", "SBC", "AND", "XOR", "OR", "CP");
		foreach (var kvp in regKvpsC6)
		{
			RegCmd(opcodes, kvp, kvp.Value + " d8")
				.Load("A")
				.Alu(kvp.Value, "d8")
				.Store("A");
		}

		for (int i = 0xc7, j = 0x00; i <= 0xf7; i += 0x10, j += 0x10)
		{
			RegCmd(opcodes, i, $"RST {j:X2}H")
				.Load("PC")
				.Push()
				.ForceFinish()
				.LoadWord(j)
				.Store("PC");
		}

		RegCmd(opcodes, 0xc9, "RET")
			.Pop()
			.ForceFinish()
			.Store("PC");

		RegCmd(opcodes, 0xcd, "CALL a16")
			.Load("PC")
			.ExtraCycle()
			.Push()
			.Load("a16")
			.Store("PC");

		for (int i = 0xcf, j = 0x08; i <= 0xff; i += 0x10, j += 0x10)
		{
			RegCmd(opcodes, i, $"RST {j:X2}H")
				.Load("PC")
				.Push()
				.ForceFinish()
				.LoadWord(j)
				.Store("PC");
		}

		RegCmd(opcodes, 0xd9, "RETI")
			.Pop()
			.ForceFinish()
			.Store("PC")
			.SwitchInterrupts(true, false);

		RegLoad(opcodes, 0xe2, "(C)", "A");

		RegLoad(opcodes, 0xf2, "A", "(C)");

		RegCmd(opcodes, 0xe9, "JP (HL)")
			.Load("HL")
			.Store("PC");

		RegCmd(opcodes, 0xe0, "LDH (a8),A")
			.CopyByte("(a8)", "A");

		RegCmd(opcodes, 0xf0, "LDH A,(a8)")
			.CopyByte("A", "(a8)");

		RegCmd(opcodes, 0xe8, "ADD SP,r8")
			.Load("SP")
			.Alu("ADD_SP", "r8")
			.ExtraCycle()
			.Store("SP");

		RegCmd(opcodes, 0xf8, "LD HL,SP+r8")
			.Load("SP")
			.Alu("ADD_SP", "r8")
			.Store("HL");

		RegLoad(opcodes, 0xea, "(a16)", "A");

		RegLoad(opcodes, 0xfa, "A", "(a16)");

		RegCmd(opcodes, 0xf3, "DI")
			.SwitchInterrupts(false, true);

		RegCmd(opcodes, 0xfb, "EI")
			.SwitchInterrupts(true, true);

		RegLoad(opcodes, 0xf9, "SP", "HL")
			.ExtraCycle();

		var regKvps00 = OpcodesForValues(
			0x00, 0x08, "RLC", "RRC", "RL", "RR", "SLA", "SRA", "SWAP", "SRL");
		foreach (var kvp in regKvps00)
		{
			var regKvps = OpcodesForValues(
				kvp.Key, 0x01, "B", "C", "D", "E", "H", "L", "(HL)", "A");
			foreach (var kvp2 in regKvps)
			{
				RegCmd(extOpcodes, kvp2, kvp.Value + " {}")
					.Load(kvp2.Value)
					.Alu(kvp.Value)
					.Store(kvp2.Value);
			}
		}

		var regKvps4040 = OpcodesForValues(0x40, 0x40, "BIT", "RES", "SET");
		foreach (var kvp in regKvps4040)
		{
			for (var b = 0; b < 0x08; b++)
			{
				int bit = kvp.Key + b * 0x08;
				var regKvps = OpcodesForValues(
					bit, 0x01, "B", "C", "D", "E", "H", "L", "(HL)", "A");
				foreach (var kvp2 in regKvps)
				{
					if ("BIT".Equals(kvp.Value) &&
						"(HL)".Equals(kvp2.Value))
					{
						RegCmd(extOpcodes, kvp2, $"BIT {b},(HL)")
							.BitHL(b);
					}
					else
					{
						RegCmd(extOpcodes, kvp2, $"{kvp.Value} {b},{kvp2.Value}")
							.Load(kvp2.Value)
							.Alu(kvp.Value, b)
							.Store(kvp2.Value);
					}
				}
			}
		}

		List<Opcode> commands = new(0x100);
		List<Opcode> extCommands = new(0x100);

		commands.AddRange(opcodes.Select(b => b?.Build()));
		extCommands.AddRange(extOpcodes.Select(b => b?.Build()));

		Commands = commands;
		ExtCommands = extCommands;
	}

	private static OpcodeBuilder RegLoad(
		IList<OpcodeBuilder> commands, int opcode, string target, string source)
	{
		return RegCmd(commands, opcode, $"LD {target},{source}").CopyByte(target, source);
	}

	private static OpcodeBuilder RegCmd(
		IList<OpcodeBuilder> commands, KeyValuePair<int, string> opcode, string label)
	{
		return RegCmd(commands, opcode.Key, label.Replace("{}", opcode.Value));
	}

	private static OpcodeBuilder RegCmd(
		IList<OpcodeBuilder> commands, int opcode, string label)
	{
		if (commands[opcode] != null)
		{
			throw new InvalidOpE(
				$"Opcode {opcode:X} already exists: {commands[opcode]}");
		}

		var builder = new OpcodeBuilder(opcode, label);
		commands[opcode] = builder;
		return builder;
	}

	private static Dictionary<int, string> OpcodesForValues(
		int start, int step, params string[] values)
	{
		Dictionary<int, string> map = [];
		int i = start;

		foreach (var e in values)
		{
			map.Add(i, e);
			i += step;
		}

		return map;
	}
}
