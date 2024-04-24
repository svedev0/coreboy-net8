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
    public static readonly int TicksPerSec = 4_194_304;

    public Mmu Mmu { get; }
    public Cpu Cpu { get; }
    public SpeedMode SpeedMode { get; }

    public bool Pause { get; set; }

    private readonly Gpu _gpu;
    private readonly Timer _timer;
    private readonly Dma _dma;
    private readonly Hdma _hdma;
    private readonly IDisplay _display;
    private readonly Sound _sound;
    private readonly SerialPort _serialPort;

    private readonly bool _gbc;

    public Gameboy(
        GameboyOptions options, 
        Cartridge rom, 
        IDisplay display, 
        IController controller,
        ISoundOutput soundOutput,
        SerialEndpoint serialEndpoint)
    {
        InterruptManager interruptManager = new(_gbc);
        Ram oamRam = new(0xfe00, 0x00a0);

        Mmu = new();
        SpeedMode = new();

        _timer = new(interruptManager, SpeedMode);
        _dma = new(Mmu, oamRam, SpeedMode);
        _gpu = new(display, interruptManager, _dma, oamRam, _gbc);
        _hdma = new(Mmu);
        _display = display;
        _sound = new(soundOutput, _gbc);
        _serialPort = new(interruptManager, serialEndpoint, SpeedMode);
        _gbc = rom.Gbc;

        Mmu.AddAddressSpace(rom);
        Mmu.AddAddressSpace(_gpu);
        Mmu.AddAddressSpace(new Joypad(interruptManager, controller));
        Mmu.AddAddressSpace(interruptManager);
        Mmu.AddAddressSpace(_serialPort);
        Mmu.AddAddressSpace(_timer);
        Mmu.AddAddressSpace(_dma);
        Mmu.AddAddressSpace(_sound);

        Mmu.AddAddressSpace(new Ram(0xc000, 0x1000));
        
        if (_gbc)
        {
            Mmu.AddAddressSpace(SpeedMode);
            Mmu.AddAddressSpace(_hdma);
            Mmu.AddAddressSpace(new GbcRam());
            Mmu.AddAddressSpace(new UndocumentedGbcRegisters());
        }
        else
        {
            Mmu.AddAddressSpace(new Ram(0xd000, 0x1000));
        }

        Mmu.AddAddressSpace(new Ram(0xff80, 0x7f));
        Mmu.AddAddressSpace(new ShadowAddressSpace(Mmu, 0xe000, 0xc000, 0x1e00));

        Cpu = new(Mmu, interruptManager, _gpu, display, SpeedMode);

        interruptManager.DisableInterrupts(false);
        
        if (!options.UseBootstrap)
        {
            InitialiseRegisters();
        }
    }

    private void InitialiseRegisters()
    {
        var registers = Cpu.Registers;

        registers.SetAf(0x01b0);
        if (_gbc)
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
        var lcdRefreshRequested = false;
        var lcdDisabled = false;
        
        while (!token.IsCancellationRequested)
        {
            if (Pause)
            {
                Task.Delay(1000, token).Wait(token);
                continue;
            }

            Gpu.Mode? newGpuMode = Tick();

            if (newGpuMode.HasValue)
            {
                _hdma.OnGpuUpdate(newGpuMode.Value);
            }

            if (!lcdDisabled && !_gpu.IsLcdEnabled())
            {
                lcdDisabled = true;
                _display.RequestRefresh();
                _hdma.OnLcdSwitch(false);
            }
            else if (newGpuMode == Gpu.Mode.VBlank)
            {
                lcdRefreshRequested = true;
                _display.RequestRefresh();
            }

            if (lcdDisabled && _gpu.IsLcdEnabled())
            {
                lcdDisabled = false;
                _display.WaitForRefresh();
                _hdma.OnLcdSwitch(true);
            }
            else if (lcdRefreshRequested && newGpuMode == Gpu.Mode.OamSearch)
            {
                lcdRefreshRequested = false;
                _display.WaitForRefresh();
            }
        }
    }

    public Gpu.Mode? Tick()
    {
        _timer.Tick();

        if (_hdma.IsTransferInProgress())
        {
            _hdma.Tick();
        }
        else
        {
            Cpu.Tick();
        }

        _dma.Tick();
        _sound.Tick();
        _serialPort.Tick();
        return _gpu.Tick();
    }
}
