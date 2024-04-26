using System.Collections.Concurrent;
using coreboy.cpu;

namespace coreboy.controller;

public class JoyPadButtonListener(
	InterruptManager interruptManager,
	ConcurrentDictionary<Button, Button> buttons) : IButtonListener
{
	private readonly InterruptManager _interruptManager = interruptManager;
	private readonly ConcurrentDictionary<Button, Button> _buttons = buttons;

	public void OnButtonPress(Button button)
	{
		if (button != null)
		{
			_interruptManager.RequestInterrupt(InterruptManager.InterruptType.P1013);
			_buttons.TryAdd(button, button);
		}
	}

	public void OnButtonRelease(Button button)
	{
		if (button != null)
		{
			_buttons.TryRemove(button, out _);
		}
	}
}
