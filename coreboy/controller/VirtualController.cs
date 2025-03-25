namespace coreboy.controller;

public class VirtualController : IController
{
	private enum ButtonPressType
	{
		Hold,
		Release
	}

	private class ButtonPress(Button button, ButtonPressType type)
	{
		public Button Button { get; init; } = button;
		public ButtonPressType Type { get; init; } = type;
	}

	private delegate void ButtonEventHandler();

	private event ButtonEventHandler? _buttonEvent;
	private readonly Queue<ButtonPress> _buttonQueue;
	private IButtonListener? _listener;

	public VirtualController()
	{
		_buttonQueue = new();
		_buttonEvent += HandleButtonPress;
	}

	~VirtualController()
	{
		_buttonEvent -= HandleButtonPress;
	}

	public void SetButtonListener(IButtonListener listener)
	{
		_listener = listener;
	}

	public void HoldButton(Button button)
	{
		ButtonPress btnPress = new(button, ButtonPressType.Hold);
		_buttonQueue.Enqueue(btnPress);
		_buttonEvent?.Invoke();
	}

	public void ReleaseButton(Button button)
	{
		ButtonPress btnPress = new(button, ButtonPressType.Release);
		_buttonQueue.Enqueue(btnPress);
		_buttonEvent?.Invoke();
	}

	private bool TryDequeueButtonPress(out ButtonPress? btnPress)
	{
		if (_buttonQueue.Count == 0)
		{
			btnPress = null;
			return false;
		}

		if (!_buttonQueue.TryDequeue(out ButtonPress? bp))
		{
			btnPress = null;
			return false;
		}
		else if (bp is null)
		{
			btnPress = null;
			return false;
		}

		btnPress = bp;
		return true;
	}

	private void HandleButtonPress()
	{
		if (!TryDequeueButtonPress(out ButtonPress? btnPress))
		{
			return;
		}
		else if (btnPress is null)
		{
			return;
		}

		switch (btnPress.Type)
		{
			case ButtonPressType.Hold:
				_listener?.OnButtonPress(btnPress.Button);
				break;

			case ButtonPressType.Release:
				_listener?.OnButtonRelease(btnPress.Button);
				break;
		}
	}
}
