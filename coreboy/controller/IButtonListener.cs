namespace coreboy.controller;

public interface IButtonListener
{
    void OnButtonPress(Button button);

    void OnButtonRelease(Button button);
}