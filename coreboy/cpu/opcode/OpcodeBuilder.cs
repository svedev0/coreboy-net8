using coreboy.cpu.op;
using coreboy.gpu;
using static coreboy.cpu.BitUtils;

using IntRegistryFunc = System.Func<coreboy.cpu.Flags, int, int>;
using BiIntRegistryFunc = System.Func<coreboy.cpu.Flags, int, int, int>;
using InvalidOpE = System.InvalidOperationException;

namespace coreboy.cpu.opcode;

public class OpcodeBuilder(int opcode, string label)
{
	private static readonly AluFunctions aluFunctions;

	public static readonly List<IntRegistryFunc> OemBug;

	static OpcodeBuilder()
	{
		aluFunctions = new AluFunctions();
		OemBug = [
			aluFunctions.GetFunction("INC", DataType.D16),
			aluFunctions.GetFunction("DEC", DataType.D16)
		];
	}

	private readonly int _opcode = opcode;
	private readonly string _label = label;
	private readonly List<Op> _ops = [];
	private DataType lastDataType;

	public int GetOpcode() => _opcode;
	public string GetLabel() => _label;
	public List<Op> GetOps() => _ops;

	public OpcodeBuilder CopyByte(string target, string source)
	{
		Load(source);
		Store(target);
		return this;
	}

	private class LoadOp(Argument arg) : Op
	{
		private readonly Argument _arg = arg;

		public override bool ReadsMemory() => _arg.IsMemory;
		public override int OperandLength() => _arg.OperandLength;

		public override int Execute(
			Registers registers, IAddressSpace addressSpace, int[] args, int context)
		{
			return _arg.Read(registers, addressSpace, args);
		}

		public override string ToString()
		{
			if (_arg.DataType == DataType.D16)
			{
				return $"{_arg.Label} → [__]";
			}

			return $"{_arg.Label} → [_]";
		}
	}

	public OpcodeBuilder Load(string source)
	{
		var arg = Argument.Parse(source);
		lastDataType = arg.DataType;
		_ops.Add(new LoadOp(arg));
		return this;
	}

	private class LoadWordOp(int value) : Op
	{
		private readonly int _value = value;

		public override int Execute(
			Registers registers, IAddressSpace addressSpace, int[] args, int context)
		{
			return _value;
		}

		public override string ToString()
		{
			return $"0x{_value:X2} → [__]";
		}
	}

	public OpcodeBuilder LoadWord(int value)
	{
		lastDataType = DataType.D16;
		_ops.Add(new LoadWordOp(value));
		return this;
	}

	private class StoreA16Op1(Argument arg) : Op
	{
		private readonly Argument _arg = arg;

		public override bool WritesMemory() => _arg.IsMemory;
		public override int OperandLength() => _arg.OperandLength;

		public override int Execute(
			Registers registers, IAddressSpace addressSpace, int[] args, int context)
		{
			addressSpace.SetByte(ToWord(args), context & 0x00ff);
			return context;
		}

		public override string ToString()
		{
			return $"[ _] → {_arg.Label}";
		}
	}

	private class StoreA16Op2(Argument arg) : Op
	{
		private readonly Argument _arg = arg;

		public override bool WritesMemory() => _arg.IsMemory;
		public override int OperandLength() => _arg.OperandLength;

		public override int Execute(
			Registers registers, IAddressSpace addressSpace, int[] args, int context)
		{
			addressSpace.SetByte(
				(ToWord(args) + 1) & 0xffff,
				(context & 0xff00) >> 8);
			return context;
		}

		public override string ToString()
		{
			return $"[_ ] → {_arg.Label}";
		}
	}

	private class StoreLastDataType(Argument arg) : Op
	{
		private readonly Argument _arg = arg;

		public override bool WritesMemory() => _arg.IsMemory;
		public override int OperandLength() => _arg.OperandLength;

		public override int Execute(
			Registers registers, IAddressSpace addressSpace, int[] args, int context)
		{
			_arg.Write(registers, addressSpace, args, context);
			return context;
		}

		public override string ToString()
		{
			if (_arg.DataType == DataType.D16)
			{
				return $"[__] → {_arg.Label}";
			}

			return $"[_] → {_arg.Label}";
		}
	}

	public OpcodeBuilder Store(string target)
	{
		var arg = Argument.Parse(target);

		if (lastDataType == DataType.D16 && arg.Label == "(a16)")
		{
			_ops.Add(new StoreA16Op1(arg));
			_ops.Add(new StoreA16Op2(arg));

		}
		else if (lastDataType == arg.DataType)
		{
			_ops.Add(new StoreLastDataType(arg));
		}
		else
		{
			throw new InvalidOpE($"Can't write {lastDataType} to {target}");
		}

		return this;
	}

	private class ProceedIfOp(string condition) : Op
	{
		private readonly string _condition = condition;

		public override bool Proceed(Registers registers)
		{
			return _condition switch
			{
				"NZ" => !registers.Flags.IsZ(),
				"Z" => registers.Flags.IsZ(),
				"NC" => !registers.Flags.IsC(),
				"C" => registers.Flags.IsC(),
				_ => false
			};
		}

		public override string ToString()
		{
			return $"? {_condition}:";
		}
	}

	public OpcodeBuilder ProceedIf(string condition)
	{
		_ops.Add(new ProceedIfOp(condition));
		return this;
	}

	private class PushOp1(IntRegistryFunc func) : Op
	{
		private readonly IntRegistryFunc _func = func;

		public override bool WritesMemory() => true;

		public override int Execute(
			Registers registers, IAddressSpace addressSpace, int[] args, int context)
		{
			registers.SP = _func(registers.Flags, registers.SP);
			addressSpace.SetByte(registers.SP, (context & 0xff00) >> 8);
			return context;
		}

		public override SpriteBug.CorruptionType? CausesOemBug(
			Registers registers, int context)
		{
			if (InOamArea(registers.SP))
			{
				return SpriteBug.CorruptionType.PUSH_1;
			}

			return null;
		}

		public override string ToString()
		{
			return "[_ ] → (SP--)";
		}
	}

	private class PushOp2(IntRegistryFunc func) : Op
	{
		private readonly IntRegistryFunc _func = func;

		public override bool WritesMemory() => true;

		public override int Execute(
			Registers registers, IAddressSpace addressSpace, int[] args, int context)
		{
			registers.SP = _func(registers.Flags, registers.SP);
			addressSpace.SetByte(registers.SP, context & 0x00ff);
			return context;
		}

		public override SpriteBug.CorruptionType? CausesOemBug(Registers registers, int context)
		{
			if (InOamArea(registers.SP))
			{
				return SpriteBug.CorruptionType.PUSH_2;
			}

			return null;
		}

		public override string ToString()
		{
			return "[ _] → (SP--)";
		}
	}

	public OpcodeBuilder Push()
	{
		var dec = aluFunctions.GetFunction("DEC", DataType.D16);
		_ops.Add(new PushOp1(dec));
		_ops.Add(new PushOp2(dec));
		return this;
	}

	private class PopOp1(IntRegistryFunc func) : Op
	{
		private readonly IntRegistryFunc _func = func;

		public override bool ReadsMemory() => true;

		public override int Execute(
			Registers registers, IAddressSpace addressSpace, int[] args, int context)
		{
			var lsb = addressSpace.GetByte(registers.SP);
			registers.SP = _func(registers.Flags, registers.SP);
			return lsb;
		}

		public override SpriteBug.CorruptionType? CausesOemBug(
			Registers registers, int context)
		{
			if (InOamArea(registers.SP))
			{
				return SpriteBug.CorruptionType.POP_1;
			}

			return null;
		}

		public override string ToString()
		{
			return "(SP++) → [ _]";
		}
	}

	private class PopOp2(IntRegistryFunc func) : Op
	{
		private readonly IntRegistryFunc _func = func;

		public override bool ReadsMemory() => true;

		public override int Execute(
			Registers registers, IAddressSpace addressSpace, int[] args, int context)
		{
			var msb = addressSpace.GetByte(registers.SP);
			registers.SP = _func(registers.Flags, registers.SP);
			return context | (msb << 8);
		}

		public override SpriteBug.CorruptionType? CausesOemBug(
			Registers registers, int context)
		{
			if (InOamArea(registers.SP))
			{
				return SpriteBug.CorruptionType.POP_2;
			}

			return null;
		}

		public override string ToString()
		{
			return "(SP++) → [_ ]";
		}
	}

	public OpcodeBuilder Pop()
	{
		var inc = aluFunctions.GetFunction("INC", DataType.D16);
		lastDataType = DataType.D16;
		_ops.Add(new PopOp1(inc));
		_ops.Add(new PopOp2(inc));
		return this;
	}

	private class AluOp1(
		BiIntRegistryFunc func,
		Argument arg,
		string operation,
		DataType lastDataType) : Op
	{
		private readonly BiIntRegistryFunc _func = func;
		private readonly Argument _arg = arg;
		private readonly string _operation = operation;
		private readonly DataType _lastDataType = lastDataType;

		public override bool ReadsMemory() => _arg.IsMemory;
		public override int OperandLength() => _arg.OperandLength;

		public override int Execute(
			Registers registers, IAddressSpace addressSpace, int[] args, int v1)
		{
			int v2 = _arg.Read(registers, addressSpace, args);
			return _func(registers.Flags, v1, v2);
		}

		public override string ToString()
		{
			if (_lastDataType == DataType.D16)
			{
				return $"{_operation}([__],{_arg}) → [__]";
			}

			return $"{_operation}([_],{_arg}) → [_]";
		}
	}

	public OpcodeBuilder Alu(string operation, string argument)
	{
		var arg = Argument.Parse(argument);
		var func = aluFunctions.GetFunction(operation, lastDataType, arg.DataType);
		_ops.Add(new AluOp1(func, arg, operation, lastDataType));

		if (lastDataType == DataType.D16)
		{
			ExtraCycle();
		}

		return this;
	}

	private class AluOp2(BiIntRegistryFunc func, string operation, int d8Value) : Op
	{
		private readonly BiIntRegistryFunc _func = func;
		private readonly string _operation = operation;
		private readonly int _d8Value = d8Value;

		public override int Execute(
			Registers registers, IAddressSpace addressSpace, int[] args, int v1)
		{
			return _func(registers.Flags, v1, _d8Value);
		}

		public override string ToString()
		{
			return $"{_operation}({_d8Value:D},[_]) → [_]";
		}
	}

	public OpcodeBuilder Alu(string operation, int d8Value)
	{
		var func = aluFunctions.GetFunction(operation, lastDataType, DataType.D8);
		_ops.Add(new AluOp2(func, operation, d8Value));

		if (lastDataType == DataType.D16)
		{
			ExtraCycle();
		}

		return this;
	}

	private class AluOp3(
		IntRegistryFunc func,
		string operation,
		DataType lastDataType) : Op
	{
		private readonly IntRegistryFunc _func = func;
		private readonly string _operation = operation;
		private readonly DataType _lastDataType = lastDataType;

		public override int Execute(
			Registers registers, IAddressSpace addressSpace, int[] args, int value)
		{
			return _func(registers.Flags, value);
		}

		public override SpriteBug.CorruptionType? CausesOemBug(
			Registers registers, int context)
		{
			if (OpcodeBuilder.CausesOemBug(_func, context))
			{
				return SpriteBug.CorruptionType.INC_DEC;
			}

			return null;
		}

		public override string ToString()
		{
			if (_lastDataType == DataType.D16)
			{
				return $"{_operation}([__]) → [__]";
			}

			return $"{_operation}([_]) → [_]";
		}
	}

	public OpcodeBuilder Alu(string operation)
	{
		var func = aluFunctions.GetFunction(operation, lastDataType);
		_ops.Add(new AluOp3(func, operation, lastDataType));

		if (lastDataType == DataType.D16)
		{
			ExtraCycle();
		}

		return this;
	}

	private class AluHLOp(IntRegistryFunc func) : Op
	{
		private readonly IntRegistryFunc _func = func;

		public override int Execute(
			Registers registers, IAddressSpace addressSpace, int[] args, int value)
		{
			return _func(registers.Flags, value);
		}

		public override SpriteBug.CorruptionType? CausesOemBug(
			Registers registers, int context)
		{
			if (OpcodeBuilder.CausesOemBug(_func, context))
			{
				return SpriteBug.CorruptionType.LD_HL;
			}

			return null;
		}

		public override string ToString()
		{
			return "%s(HL) → [__]";
		}
	}

	public OpcodeBuilder AluHL(string operation)
	{
		Load("HL");
		_ops.Add(new AluHLOp(aluFunctions.GetFunction(operation, DataType.D16)));
		Store("HL");
		return this;
	}

	private class BitHLOp(int bit) : Op
	{
		private readonly int _bit = bit;

		public override bool ReadsMemory() => true;

		public override int Execute(
			Registers registers, IAddressSpace addressSpace, int[] args, int context)
		{
			int value = addressSpace.GetByte(registers.HL);

			var flags = registers.Flags;
			flags.SetN(false);
			flags.SetH(true);

			if (_bit < 8)
			{
				flags.SetZ(!GetBit(value, _bit));
			}

			return context;
		}

		public override string ToString()
		{
			return $"BIT({_bit:D},HL)";
		}
	}

	public OpcodeBuilder BitHL(int bit)
	{
		_ops.Add(new BitHLOp(bit));
		return this;
	}

	private class ClearZOp : Op
	{
		public override int Execute(
			Registers registers, IAddressSpace addressSpace, int[] args, int context)
		{
			registers.Flags.SetZ(false);
			return context;
		}

		public override string ToString()
		{
			return "0 → Z";
		}
	}

	public OpcodeBuilder ClearZ()
	{
		_ops.Add(new ClearZOp());
		return this;
	}

	private class SwitchInterruptsOp(bool enable, bool withDelay) : Op
	{
		private readonly bool _enable = enable;
		private readonly bool _withDelay = withDelay;

		public override void SwitchInterrupts(InterruptManager interruptManager)
		{
			if (_enable)
			{
				interruptManager.EnableInterrupts(_withDelay);
			}
			else
			{
				interruptManager.DisableInterrupts(_withDelay);
			}
		}

		public override string ToString()
		{
			if (_enable)
			{
				return "enable interrupts";
			}
			else
			{
				return "disable interrupts";
			}
		}
	}

	public OpcodeBuilder SwitchInterrupts(bool enable, bool withDelay)
	{
		_ops.Add(new SwitchInterruptsOp(enable, withDelay));
		return this;
	}

	public OpcodeBuilder Op(Op op)
	{
		_ops.Add(op);
		return this;
	}

	private class ExtraCycleOp : Op
	{
		public override bool ReadsMemory() => true;

		public override string ToString() => "wait cycle";
	}

	public OpcodeBuilder ExtraCycle()
	{
		_ops.Add(new ExtraCycleOp());
		return this;
	}

	private class ForceFinishOp : Op
	{
		public override bool ForceFinishCycle() => true;

		public override string ToString() => "finish cycle";
	}

	public OpcodeBuilder ForceFinish()
	{
		_ops.Add(new ForceFinishOp());
		return this;
	}

	public Opcode Build() => new(this);

	public override string ToString() => _label;

	public static bool CausesOemBug(IntRegistryFunc function, int context)
	{
		return OemBug.Contains(function) && InOamArea(context);
	}

	private static bool InOamArea(int address)
	{
		return address >= 0xfe00 && address <= 0xfeff;
	}
}
