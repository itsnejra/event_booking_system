using EventBooking.Domain.Enums;

namespace EventBooking.ConsoleApp.Screens;

/// <summary>
/// One entry of the main menu. Adding a screen means writing a class and registering it - the menu
/// itself never learns what the options are.
/// </summary>
public interface IScreen
{
    string Title { get; }

    /// <summary>
    /// The role allowed to open this screen, or <see langword="null"/> when anyone may.
    /// The menu hides whatever the signed-in user cannot use, so a screen never has to defend itself.
    /// </summary>
    UserRole? RequiredRole { get; }

    void Show();
}
