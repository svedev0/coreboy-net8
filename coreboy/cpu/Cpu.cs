#nullable disable

using coreboy.cpu.op;
using coreboy.cpu.opcode;
using coreboy.gpu;

using InvalidOpE = System.InvalidOperationException;

namespace coreboy.cpu;

public enum State
{
	OPCODE,
	EXT_OPCODE,
	OPERAND,
	RUNNING,
	IRQ_READ_IF,
	IRQ_READ_IE,
	IRQ_PUSH_1,
	IRQ_PUSH_2,
	IRQ_JUMP,
	STOPPED,
	HALTED
}

public class Cpu(
	IAddressSpace addressSpace,
	InterruptManager interruptManager,
	Gpu gpu,
	IDisplay display,
	SpeedMode speedMode)
{
	public Registers Registers { get; set; } = new Registers();
	public Opcode CurrentOpcode { get; private set; }
	public State State { get; private set; } = State.OPCODE;

	private readonly IAddressSpace _addressSpace = addressSpace;
	private readonly InterruptManager _interruptManager = interruptManager;
	private readonly Gpu _gpu = gpu;
	private readonly IDisplay _display = display;
	private readonly SpeedMode _speedMode = speedMode;

	private int opcode1;
	private int opcode2;
	private readonly int[] operand = new int[2];
	private List<Op> ops;
	private int operandIndex;
	private int opIndex;

	private int opContext;
	private int interruptFlag;
	private int interruptEnabled;

	private InterruptManager.InterruptType requestedInt;

	private int clockCycle;
	private bool haltBugMode;
	private readonly Opcodes opcodes = new();

	public void Tick()
	{
		clockCycle++;
		int speed = _speedMode.GetSpeedMode();

		if (clockCycle >= (4 / speed))
		{
			clockCycle = 0;
		}
		else
		{
			return;
		}

		if (State == State.OPCODE || State == State.HALTED || State == State.STOPPED)
		{
			if (_interruptManager.IsIme() && _interruptManager.IsInterruptRequested())
			{
				if (State == State.STOPPED)
				{
					_display.Enabled = true;
				}

				State = State.IRQ_READ_IF;
			}
		}

		switch (State)
		{
			case State.IRQ_READ_IF:
			case State.IRQ_READ_IE:
			case State.IRQ_PUSH_1:
			case State.IRQ_PUSH_2:
			case State.IRQ_JUMP:
				HandleInterrupt();
				return;

			case State.HALTED when _interruptManager.IsInterruptRequested():
				State = State.OPCODE;
				break;
		}

		if (State == State.HALTED || State == State.STOPPED)
		{
			return;
		}

		bool accessedMemory = false;

		while (true)
		{
			int pc = Registers.PC;

			switch (State)
			{
				case State.OPCODE:
					ClearState();
					opcode1 = _addressSpace.GetByte(pc);
					accessedMemory = true;

					if (opcode1 == 0xcb)
					{
						State = State.EXT_OPCODE;
					}
					else if (opcode1 == 0x10)
					{
						CurrentOpcode = opcodes.Commands[opcode1];
						State = State.EXT_OPCODE;
					}
					else
					{
						State = State.OPERAND;
						CurrentOpcode = opcodes.Commands[opcode1];
						if (CurrentOpcode == null)
						{
							throw new InvalidOpE(
								$"No command for 0x{opcode1:X2}");
						}
					}

					if (!haltBugMode)
					{
						Registers.IncrementPc();
					}
					else
					{
						haltBugMode = false;
					}
					break;

				case State.EXT_OPCODE:
					if (accessedMemory)
					{
						return;
					}

					accessedMemory = true;
					opcode2 = _addressSpace.GetByte(pc);
					CurrentOpcode ??= opcodes.ExtCommands[opcode2];

					if (CurrentOpcode == null)
					{
						throw new InvalidOpE(
							$"No command for {opcode2:X}cb 0x{opcode2:X2}");
					}

					State = State.OPERAND;
					Registers.IncrementPc();
					break;

				case State.OPERAND:
					while (operandIndex < CurrentOpcode.Length)
					{
						if (accessedMemory)
						{
							return;
						}

						accessedMemory = true;
						operand[operandIndex++] = _addressSpace.GetByte(pc);
						Registers.IncrementPc();
					}

					ops = [.. CurrentOpcode.Ops];
					State = State.RUNNING;
					break;

				case State.RUNNING:
					if (opcode1 == 0x10)
					{
						if (_speedMode.OnStop())
						{
							State = State.OPCODE;
						}
						else
						{
							State = State.STOPPED;
							_display.Enabled = false;
						}

						return;
					}
					else if (opcode1 == 0x76)
					{
						if (_interruptManager.IsHaltBug())
						{
							State = State.OPCODE;
							haltBugMode = true;
							return;
						}
						else
						{
							State = State.HALTED;
							return;
						}
					}

					if (opIndex < ops.Count)
					{
						var op = ops[opIndex];
						bool opAccessesMemory = op.ReadsMemory() || op.WritesMemory();

						if (accessedMemory && opAccessesMemory)
						{
							return;
						}

						opIndex++;

						var corruptionType = op.CausesOemBug(Registers, opContext);

						if (corruptionType != null)
						{
							HandleSpriteBug(corruptionType.Value);
						}

						opContext = op.Execute(
							Registers, _addressSpace, operand, opContext);
						op.SwitchInterrupts(_interruptManager);

						if (!op.Proceed(Registers))
						{
							opIndex = ops.Count;
							break;
						}

						if (op.ForceFinishCycle())
						{
							return;
						}

						if (opAccessesMemory)
						{
							accessedMemory = true;
						}
					}

					if (opIndex >= ops.Count)
					{
						State = State.OPCODE;
						operandIndex = 0;
						_interruptManager.OnInstructionFinished();
						return;
					}
					break;

				case State.HALTED:
				case State.STOPPED:
					return;
			}
		}
	}

	private void HandleInterrupt()
	{
		switch (State)
		{
			case State.IRQ_READ_IF:
				interruptFlag = _addressSpace.GetByte(0xff0f);
				State = State.IRQ_READ_IE;
				break;

			case State.IRQ_READ_IE:
				interruptEnabled = _addressSpace.GetByte(0xffff);
				requestedInt = null;

				foreach (var irq in InterruptManager.InterruptType.Values())
				{
					if ((interruptFlag & interruptEnabled & (1 << irq.Ordinal)) != 0)
					{
						requestedInt = irq;
						break;
					}
				}

				if (requestedInt == null)
				{
					State = State.OPCODE;
				}
				else
				{
					State = State.IRQ_PUSH_1;
					_interruptManager.ClearInterrupt(requestedInt);
					_interruptManager.DisableInterrupts(false);
				}
				break;

			case State.IRQ_PUSH_1:
				Registers.DecrementSp();
				_addressSpace.SetByte(Registers.SP, (Registers.PC & 0xff00) >> 8);
				State = State.IRQ_PUSH_2;
				break;

			case State.IRQ_PUSH_2:
				Registers.DecrementSp();
				_addressSpace.SetByte(Registers.SP, Registers.PC & 0x00ff);
				State = State.IRQ_JUMP;
				break;

			case State.IRQ_JUMP:
				Registers.PC = requestedInt.Handler;
				requestedInt = null;
				State = State.OPCODE;
				break;
		}
	}

	private void HandleSpriteBug(SpriteBug.CorruptionType type)
	{
		if (!_gpu.GetLcdc().IsLcdEnabled())
		{
			return;
		}

		int stat = _addressSpace.GetByte(GpuRegister.Stat.Address);

		if ((stat & 0b11) == (int)Gpu.Mode.OamSearch &&
			_gpu.GetTicksInLine() < 79)
		{
			SpriteBug.CorruptOam(_addressSpace, type, _gpu.GetTicksInLine());
		}
	}

	public void ClearState()
	{
		opcode1 = 0;
		opcode2 = 0;
		CurrentOpcode = null;
		ops = null;

		operand[0] = 0x00;
		operand[1] = 0x00;
		operandIndex = 0;

		opIndex = 0;
		opContext = 0;

		interruptFlag = 0;
		interruptEnabled = 0;
		requestedInt = null;
	}
}
