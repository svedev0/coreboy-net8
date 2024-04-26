using System.Collections.Concurrent;
using coreboy.cpu;

namespace coreboy.controller;

public class Joypad : IAddressSpace
{
	private readonly ConcurrentDictionary<Button, Button> buttons = new();
	private int buttonmask;

	public Joypad(InterruptManager interruptManager, IController controller)
	{
		JoyPadButtonListener listener = new(interruptManager, buttons);
		controller.SetButtonListener(listener);
	}

	public bool Accepts(int address)
	{
		return address == 0xff00;
	}

	public void SetByte(int address, int value)
	{
		buttonmask = value & 0b00110000;
	}

	public int GetByte(int address)
	{
		int result = buttonmask | 0b11001111;

		foreach (Button b in buttons.Keys)
		{
			if ((b.Line & buttonmask) == 0)
			{
				result &= 0xff & ~b.Mask;
			}
		}

		return result;
	}
}
