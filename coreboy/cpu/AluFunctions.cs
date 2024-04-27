using coreboy.cpu.op;

using IntRegistryFunc = System.Func<coreboy.cpu.Flags, int, int>;
using BiIntRegistryFunc = System.Func<coreboy.cpu.Flags, int, int, int>;
using AluFuncsMap = System.Collections.Generic.Dictionary<
	coreboy.cpu.AluFunctions.FunctionKey,
	System.Func<coreboy.cpu.Flags,
	int,
	int>>;
using AluBiFuncsMap = System.Collections.Generic.Dictionary<
	coreboy.cpu.AluFunctions.FunctionKey,
	System.Func<coreboy.cpu.Flags,
	int,
	int,
	int>>;

namespace coreboy.cpu;

public class AluFunctions
{
	private readonly AluFuncsMap _functions = [];
	private readonly AluBiFuncsMap _biFunctions = [];

	public IntRegistryFunc GetFunction(string name, DataType argumentType)
	{
		return _functions[new FunctionKey(name, argumentType)];
	}

	public BiIntRegistryFunc GetFunction(
		string name, DataType arg1Type, DataType arg2Type)
	{
		return _biFunctions[new FunctionKey(name, arg1Type, arg2Type)];
	}

	private void AddFunc(
		string name, DataType dataType, IntRegistryFunc function)
	{
		_functions[new FunctionKey(name, dataType)] = function;
	}

	private void AddFunc(
		string name, DataType dataType1, DataType dataType2, BiIntRegistryFunc function)
	{
		_biFunctions[new FunctionKey(name, dataType1, dataType2)] = function;
	}

	public AluFunctions()
	{
		AddFunc("INC", DataType.D8, (flags, arg) =>
		{
			int result = (arg + 1) & 0xff;

			flags.SetZ(result == 0);
			flags.SetN(false);
			flags.SetH((arg & 0x0f) == 0x0f);
			return result;
		});

		AddFunc("INC", DataType.D16, (flags, arg) =>
		{
			return (arg + 1) & 0xffff;
		});

		AddFunc("DEC", DataType.D8, (flags, arg) =>
		{
			int result = (arg - 1) & 0xff;

			flags.SetZ(result == 0);
			flags.SetN(true);
			flags.SetH((arg & 0x0f) == 0x0);
			return result;
		});

		AddFunc("DEC", DataType.D16, (flags, arg) =>
		{
			return (arg - 1) & 0xffff;
		});

		AddFunc("ADD", DataType.D16, DataType.D16, (flags, arg1, arg2) =>
		{
			flags.SetN(false);
			flags.SetH((arg1 & 0x0fff) + (arg2 & 0x0fff) > 0x0fff);
			flags.SetC(arg1 + arg2 > 0xffff);
			return (arg1 + arg2) & 0xffff;
		});

		AddFunc("ADD", DataType.D16, DataType.R8, (flags, arg1, arg2) =>
		{
			return (arg1 + arg2) & 0xffff;
		});

		AddFunc("ADD_SP", DataType.D16, DataType.R8, (flags, arg1, arg2) =>
		{
			flags.SetZ(false);
			flags.SetN(false);

			int result = arg1 + arg2;

			flags.SetC((((arg1 & 0xff) + (arg2 & 0xff)) & 0x100) != 0);
			flags.SetH((((arg1 & 0x0f) + (arg2 & 0x0f)) & 0x10) != 0);
			return result & 0xffff;
		});

		AddFunc("DAA", DataType.D8, (flags, arg) =>
		{
			int result = arg;

			if (flags.IsN())
			{
				if (flags.IsH())
				{
					result = (result - 6) & 0xff;
				}

				if (flags.IsC())
				{
					result = (result - 0x60) & 0xff;
				}
			}
			else
			{
				if (flags.IsH() || (result & 0xf) > 9)
				{
					result += 0x06;
				}

				if (flags.IsC() || result > 0x9f)
				{
					result += 0x60;
				}
			}

			flags.SetH(false);

			if (result > 0xff)
			{
				flags.SetC(true);
			}

			result &= 0xff;
			flags.SetZ(result == 0);
			return result;
		});

		AddFunc("CPL", DataType.D8, (flags, arg) =>
		{
			flags.SetN(true);
			flags.SetH(true);
			return (~arg) & 0xff;
		});

		AddFunc("SCF", DataType.D8, (flags, arg) =>
		{
			flags.SetN(false);
			flags.SetH(false);
			flags.SetC(true);
			return arg;
		});

		AddFunc("CCF", DataType.D8, (flags, arg) =>
		{
			flags.SetN(false);
			flags.SetH(false);
			flags.SetC(!flags.IsC());
			return arg;
		});

		AddFunc("ADD", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
		{
			flags.SetZ(((byte1 + byte2) & 0xff) == 0);
			flags.SetN(false);
			flags.SetH((byte1 & 0x0f) + (byte2 & 0x0f) > 0x0f);
			flags.SetC(byte1 + byte2 > 0xff);
			return (byte1 + byte2) & 0xff;
		});

		AddFunc("ADC", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
		{
			int carry = flags.IsC() ? 1 : 0;

			flags.SetZ(((byte1 + byte2 + carry) & 0xff) == 0);
			flags.SetN(false);
			flags.SetH((byte1 & 0x0f) + (byte2 & 0x0f) + carry > 0x0f);
			flags.SetC(byte1 + byte2 + carry > 0xff);
			return (byte1 + byte2 + carry) & 0xff;
		});

		AddFunc("SUB", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
		{
			flags.SetZ(((byte1 - byte2) & 0xff) == 0);
			flags.SetN(true);
			flags.SetH((0x0f & byte2) > (0x0f & byte1));
			flags.SetC(byte2 > byte1);
			return (byte1 - byte2) & 0xff;
		});

		AddFunc("SBC", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
		{
			int carry = flags.IsC() ? 1 : 0;
			int result = byte1 - byte2 - carry;

			flags.SetZ((result & 0xff) == 0);
			flags.SetN(true);
			flags.SetH(((byte1 ^ byte2 ^ (result & 0xff)) & (1 << 4)) != 0);
			flags.SetC(result < 0);
			return result & 0xff;
		});

		AddFunc("AND", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
		{
			int result = byte1 & byte2;

			flags.SetZ(result == 0);
			flags.SetN(false);
			flags.SetH(true);
			flags.SetC(false);
			return result;
		});

		AddFunc("OR", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
		{
			int result = byte1 | byte2;

			flags.SetZ(result == 0);
			flags.SetN(false);
			flags.SetH(false);
			flags.SetC(false);
			return result;
		});

		AddFunc("XOR", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
		{
			int result = (byte1 ^ byte2) & 0xff;

			flags.SetZ(result == 0);
			flags.SetN(false);
			flags.SetH(false);
			flags.SetC(false);
			return result;
		});

		AddFunc("CP", DataType.D8, DataType.D8, (flags, byte1, byte2) =>
		{
			flags.SetZ(((byte1 - byte2) & 0xff) == 0);
			flags.SetN(true);
			flags.SetH((0x0f & byte2) > (0x0f & byte1));
			flags.SetC(byte2 > byte1);
			return byte1;
		});

		AddFunc("RLC", DataType.D8, (flags, arg) =>
		{
			int result = (arg << 1) & 0xff;

			if ((arg & (1 << 7)) != 0)
			{
				result |= 1;
				flags.SetC(true);
			}
			else
			{
				flags.SetC(false);
			}

			flags.SetZ(result == 0);
			flags.SetN(false);
			flags.SetH(false);
			return result;
		});

		AddFunc("RRC", DataType.D8, (flags, arg) =>
		{
			int result = arg >> 1;

			if ((arg & 1) == 1)
			{
				result |= 1 << 7;
				flags.SetC(true);
			}
			else
			{
				flags.SetC(false);
			}

			flags.SetZ(result == 0);
			flags.SetN(false);
			flags.SetH(false);
			return result;
		});

		AddFunc("RL", DataType.D8, (flags, arg) =>
		{
			int result = (arg << 1) & 0xff;
			result |= flags.IsC() ? 1 : 0;

			flags.SetC((arg & (1 << 7)) != 0);
			flags.SetZ(result == 0);
			flags.SetN(false);
			flags.SetH(false);
			return result;
		});

		AddFunc("RR", DataType.D8, (flags, arg) =>
		{
			int result = arg >> 1;
			result |= flags.IsC() ? (1 << 7) : 0;

			flags.SetC((arg & 1) != 0);
			flags.SetZ(result == 0);
			flags.SetN(false);
			flags.SetH(false);
			return result;
		});

		AddFunc("SLA", DataType.D8, (flags, arg) =>
		{
			int result = (arg << 1) & 0xff;

			flags.SetC((arg & (1 << 7)) != 0);
			flags.SetZ(result == 0);
			flags.SetN(false);
			flags.SetH(false);
			return result;
		});

		AddFunc("SRA", DataType.D8, (flags, arg) =>
		{
			int result = (arg >> 1) | (arg & (1 << 7));

			flags.SetC((arg & 1) != 0);
			flags.SetZ(result == 0);
			flags.SetN(false);
			flags.SetH(false);
			return result;
		});

		AddFunc("SWAP", DataType.D8, (flags, arg) =>
		{
			int upper = arg & 0xf0;
			int lower = arg & 0x0f;
			int result = (lower << 4) | (upper >> 4);

			flags.SetZ(result == 0);
			flags.SetN(false);
			flags.SetH(false);
			flags.SetC(false);
			return result;
		});

		AddFunc("SRL", DataType.D8, (flags, arg) =>
		{
			int result = arg >> 1;

			flags.SetC((arg & 1) != 0);
			flags.SetZ(result == 0);
			flags.SetN(false);
			flags.SetH(false);
			return result;
		});

		AddFunc("BIT", DataType.D8, DataType.D8, (flags, arg1, arg2) =>
		{
			int bit = arg2;

			flags.SetN(false);
			flags.SetH(true);

			if (bit < 8)
			{
				flags.SetZ(!BitUtils.GetBit(arg1, arg2));
			}

			return arg1;
		});

		AddFunc("RES", DataType.D8, DataType.D8, (flags, arg1, arg2) =>
		{
			return BitUtils.ClearBit(arg1, arg2);
		});

		AddFunc("SET", DataType.D8, DataType.D8, (flags, arg1, arg2) =>
		{
			return BitUtils.SetBit(arg1, arg2);
		});
	}

	public class FunctionKey
	{
		private readonly string _name;
		private readonly DataType _type1;
		private readonly DataType _type2;

		public FunctionKey(string name, DataType type1, DataType type2)
		{
			_name = name;
			_type1 = type1;
			_type2 = type2;
		}

		public FunctionKey(string name, DataType type)
		{
			_name = name;
			_type1 = type;
			_type2 = DataType.Unset;
		}

		protected bool Equals(FunctionKey other)
		{
			return _name == other._name &&
				_type1 == other._type1 &&
				_type2 == other._type2;
		}

		public override bool Equals(object? obj)
		{
			if (obj is null)
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((FunctionKey)obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(_name, _type1, _type2);
		}
	}
}
