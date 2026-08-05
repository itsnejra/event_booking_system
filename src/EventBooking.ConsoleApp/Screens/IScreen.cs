namespace EventBooking.ConsoleApp.Screens;

/// <summary>
/// One entry of the main menu. Adding a screen means writing a class and registering it - the menu
/// itself never learns what the options are.
/// </summary>
public interface IScreen
{
    string Title { get; }

    void Show();
}
