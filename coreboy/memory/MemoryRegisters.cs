namespace coreboy.memory;

public class MemoryRegisters : IAddressSpace
{
	private readonly Dictionary<int, IRegister> _registers;
	private readonly Dictionary<int, int> _values = [];
	private readonly RegisterType[] _allowsWrite = [RegisterType.W, RegisterType.RW];
	private readonly RegisterType[] _allowsRead = [RegisterType.R, RegisterType.RW];

	public MemoryRegisters(params IRegister[] registers)
	{
		Dictionary<int, IRegister> map = [];

		foreach (var reg in registers)
		{
			if (map.ContainsKey(reg.Address))
			{
				throw new ArgumentException("Two registers with the same address");
			}

			map.Add(reg.Address, reg);
			_values.Add(reg.Address, 0);
		}

		_registers = map;
	}

	private MemoryRegisters(MemoryRegisters original)
	{
		_registers = original._registers;
		_values = new(original._values);
	}

	public int Get(IRegister reg)
	{
		if (_registers.ContainsKey(reg.Address))
		{
			return _values[reg.Address];
		}
		else
		{
			throw new ArgumentException("Not valid register: " + reg);
		}
	}

	public void Put(IRegister reg, int value)
	{
		if (_registers.ContainsKey(reg.Address))
		{
			_values[reg.Address] = value;
		}
		else
		{
			throw new ArgumentException("Not valid register: " + reg);
		}
	}

	public MemoryRegisters Freeze() => new(this);

	public int PreIncrement(IRegister reg)
	{
		if (!_registers.ContainsKey(reg.Address))
		{
			throw new ArgumentException("Not valid register: " + reg);
		}

		var value = _values[reg.Address] + 1;
		_values[reg.Address] = value;
		return value;
	}

	public bool Accepts(int address)
	{
		return _registers.ContainsKey(address);
	}

	public void SetByte(int address, int value)
	{
		var regType = _registers[address].Type;

		if (_allowsWrite.Contains(regType))
		{
			_values[address] = value;
		}
	}

	public int GetByte(int address)
	{
		var regType = _registers[address].Type;
		
		if (_allowsRead.Contains(regType))
		{
			return _values[address];
		}
		else
		{
			return 0xff;
		}
	}
}
