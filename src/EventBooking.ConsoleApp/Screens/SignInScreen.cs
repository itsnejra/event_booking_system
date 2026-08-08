using EventBooking.Application.Authentication;
using EventBooking.ConsoleApp.Ui;
using EventBooking.Domain.Entities;
using EventBooking.Domain.Interfaces;

namespace EventBooking.ConsoleApp.Screens;

/// <summary>
/// The gate in front of the menu. It asks who you are, hands the answer to
/// <see cref="IAuthenticator"/>, and knows nothing about what any of them are allowed to do.
/// </summary>
/// <remarks>
/// Not an <see cref="IScreen"/>: those are the things a signed-in user picks from the menu, and this
/// runs before there is a menu at all.
/// </remarks>
public sealed class SignInScreen(
    IAuthenticator authenticator,
    IUserRepository users,
    IUserInterface ui)
{
    /// <summary>The user who signed in, or <see langword="null"/> if they chose to leave instead.</summary>
    public User? Show()
    {
        ui.Clear();
        ui.Header("Event Booking System");
        ui.Muted("Sign in to continue. The menu you get depends on the role of the account you use.");

        ShowDemoAccounts();

        while (true)
        {
            ui.Blank();
            var typed = ui.Ask("E-mail address (Enter to quit)");

            if (typed is null || ui.InputClosed)
            {
                return null;
            }

            var user = authenticator.Authenticate(typed);
            if (user is not null)
            {
                ui.Success($"Signed in as {user.FullName}.");
                return user;
            }

            // Deliberately the same answer for a malformed address and an unknown one.
            ui.Error("  No account for that address.");
        }
    }

    /// <summary>
    /// A demo has to be usable by someone who has never seen the data, so the addresses are on
    /// screen. A real sign-in screen would obviously show none of this.
    /// </summary>
    private void ShowDemoAccounts()
    {
        ui.Section("Demo accounts");

        foreach (var user in users.GetAll().OrderBy(user => user.Role).ThenBy(user => user.FullName))
        {
            ui.Muted($"  {user.Email,-40} {Describe(user)}");
        }
    }

    private static string Describe(User user) => user switch
    {
        Customer customer => $"customer, {customer.Tier} tier",
        Organizer organizer => $"organiser at {organizer.OrganizationName}",
        _ => user.Role.ToString(),
    };
}
