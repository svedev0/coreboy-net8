namespace coreboy.cpu;

public class InterruptManager(bool gbc) : IAddressSpace
{
	private bool ime;
	private readonly bool _gbc = gbc;
	private int interruptFlag = 0xe1;
	private int interruptEnabled;
	private int pendingEnableInterrupts = -1;
	private int pendingDisableInterrupts = -1;

	public void EnableInterrupts(bool withDelay)
	{
		pendingDisableInterrupts = -1;

		if (withDelay)
		{
			if (pendingEnableInterrupts == -1)
			{
				pendingEnableInterrupts = 1;
			}
		}
		else
		{
			pendingEnableInterrupts = -1;
			ime = true;
		}
	}

	public void DisableInterrupts(bool withDelay)
	{
		pendingEnableInterrupts = -1;

		if (withDelay && _gbc)
		{
			if (pendingDisableInterrupts == -1)
			{
				pendingDisableInterrupts = 1;
			}
		}
		else
		{
			pendingDisableInterrupts = -1;
			ime = false;
		}
	}

	public void RequestInterrupt(InterruptType type)
	{
		interruptFlag |= 1 << type.Ordinal;
	}

	public void ClearInterrupt(InterruptType type)
	{
		interruptFlag &= ~(1 << type.Ordinal);
	}

	public void OnInstructionFinished()
	{
		if (pendingEnableInterrupts != -1)
		{
			if (pendingEnableInterrupts-- == 0)
			{
				EnableInterrupts(false);
			}
		}

		if (pendingDisableInterrupts != -1)
		{
			if (pendingDisableInterrupts-- == 0)
			{
				DisableInterrupts(false);
			}
		}
	}

	public bool IsIme()
	{
		return ime;
	}

	public bool IsInterruptRequested()
	{
		return (interruptFlag & interruptEnabled) != 0;
	}

	public bool IsHaltBug()
	{
		return (interruptFlag & interruptEnabled & 0x1f) != 0 && !ime;
	}

	public bool Accepts(int address)
	{
		return address == 0xff0f || address == 0xffff;
	}

	public void SetByte(int address, int value)
	{
		switch (address)
		{
			case 0xff0f:
				interruptFlag = value | 0xe0;
				break;

			case 0xffff:
				interruptEnabled = value;
				break;
		}
	}

	public int GetByte(int address)
	{
		return address switch
		{
			0xff0f => interruptFlag,
			0xffff => interruptEnabled,
			_ => 0xff,
		};
	}

	public class InterruptType
	{
		public static readonly InterruptType VBlank = new(0x0040, 0);
		public static readonly InterruptType Lcdc = new(0x0048, 1);
		public static readonly InterruptType Timer = new(0x0050, 2);
		public static readonly InterruptType Serial = new(0x0058, 3);
		public static readonly InterruptType P1013 = new(0x0060, 4);

		public int Ordinal { get; }
		public int Handler { get; }

		private InterruptType(int handler, int ordinal)
		{
			Ordinal = ordinal;
			Handler = handler;
		}

		public static IEnumerable<InterruptType> Values()
		{
			yield return VBlank;
			yield return Lcdc;
			yield return Timer;
			yield return Serial;
			yield return P1013;
		}
	}
}
