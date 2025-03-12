using coreboy.controller;
using coreboy.cpu;
using coreboy.gpu;
using coreboy.gui;
using coreboy.memory;
using coreboy.memory.cart;
using coreboy.serial;
using coreboy.sound;
using Timer = coreboy.timer.Timer;

namespace coreboy;

public class Gameboy : IRunnable
{
	private const int baseTicksPerSec = 4_195_000;
	public static int TicksPerSec { get; private set; } = baseTicksPerSec;

	public static void SetSpeedMultiplier(int multiplier)
	{
		TicksPerSec = baseTicksPerSec * multiplier;
	}

	public Mmu Mmu { get; }
	public Cpu Cpu { get; }
	public SpeedMode SpeedMode { get; }

	public bool Pause { get; set; }

	private readonly IDisplay _display;
	private readonly Gpu gpu;
	private readonly Timer timer;
	private readonly Dma dma;
	private readonly Hdma hdma;
	private readonly Sound sound;
	private readonly SerialPort serialPort;

	private readonly bool gbcMode;

	public Gameboy(
		GameboyOptions options,
		Cartridge rom,
		IDisplay display,
		IController controller,
		ISoundOutput soundOutput,
		ISerialEndpoint serialEndpoint)
	{
		_display = display;
		gbcMode = rom.Gbc;
		SpeedMode = new SpeedMode();

		InterruptManager interruptManager = new(gbcMode);

		timer = new Timer(interruptManager, SpeedMode);
		Mmu = new Mmu();

		Ram oamRam = new(0xfe00, 0x00a0);

		dma = new Dma(Mmu, oamRam, SpeedMode);
		gpu = new Gpu(display, interruptManager, dma, oamRam, gbcMode);
		hdma = new Hdma(Mmu);
		sound = new Sound(soundOutput, gbcMode);
		serialPort = new SerialPort(interruptManager, serialEndpoint, SpeedMode);

		Mmu.AddAddressSpace(rom);
		Mmu.AddAddressSpace(gpu);
		Mmu.AddAddressSpace(new Joypad(interruptManager, controller));
		Mmu.AddAddressSpace(interruptManager);
		Mmu.AddAddressSpace(serialPort);
		Mmu.AddAddressSpace(timer);
		Mmu.AddAddressSpace(dma);
		Mmu.AddAddressSpace(sound);
		Mmu.AddAddressSpace(new Ram(0xc000, 0x1000));

		if (gbcMode)
		{
			Mmu.AddAddressSpace(SpeedMode);
			Mmu.AddAddressSpace(hdma);
			Mmu.AddAddressSpace(new GbcRam());
			Mmu.AddAddressSpace(new UndocumentedGbcRegisters());
		}
		else
		{
			Mmu.AddAddressSpace(new Ram(0xd000, 0x1000));
		}

		Mmu.AddAddressSpace(new Ram(0xff80, 0x7f));
		Mmu.AddAddressSpace(new ShadowAddressSpace(Mmu, 0xe000, 0xc000, 0x1e00));

		Cpu = new Cpu(Mmu, interruptManager, gpu, display, SpeedMode);

		interruptManager.DisableInterrupts(false);

		if (!options.UseBootstrap)
		{
			InitiliseRegisters();
		}
	}

	private void InitiliseRegisters()
	{
		Registers registers = Cpu.Registers;
		registers.SetAf(0x01b0);

		if (gbcMode)
		{
			registers.A = 0x11;
		}

		registers.SetBc(0x0013);
		registers.SetDe(0x00d8);
		registers.SetHl(0x014d);
		registers.SP = 0xfffe;
		registers.PC = 0x0100;
	}

	public void Run(CancellationToken token)
	{
		bool lcdRefreshRequested = false;
		bool lcdDisabled = false;

		while (!token.IsCancellationRequested)
		{
			if (Pause)
			{
				Task.Delay(1000, token).Wait(token);
				continue;
			}

			Gpu.Mode? newMode = Tick();

			if (newMode.HasValue)
			{
				hdma.OnGpuUpdate(newMode.Value);
			}

			if (!lcdDisabled && !gpu.IsLcdEnabled())
			{
				lcdDisabled = true;
				_display.RequestRefresh();
				hdma.OnLcdSwitch(false);
			}
			else if (newMode == Gpu.Mode.VBlank)
			{
				lcdRefreshRequested = true;
				_display.RequestRefresh();
			}

			if (lcdDisabled && gpu.IsLcdEnabled())
			{
				lcdDisabled = false;
				_display.WaitForRefresh();
				hdma.OnLcdSwitch(true);
			}
			else if (lcdRefreshRequested && newMode == Gpu.Mode.OamSearch)
			{
				lcdRefreshRequested = false;
				_display.WaitForRefresh();
			}
		}
	}

	public Gpu.Mode? Tick()
	{
		timer.Tick();

		if (hdma.IsTransferInProgress())
		{
			hdma.Tick();
		}
		else
		{
			Cpu.Tick();
		}

		dma.Tick();
		sound.Tick();
		serialPort.Tick();
		return gpu.Tick();
	}
}
