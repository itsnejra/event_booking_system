using EventBooking.ConsoleApp.Ui;
using EventBooking.Domain.Entities;
using EventBooking.Domain.Interfaces;

namespace EventBooking.ConsoleApp.Screens;

/// <summary>
/// The loop. It knows how to show the screens the signed-in user is allowed to open, and how to
/// survive anything they throw; it knows nothing whatsoever about booking.
/// </summary>
public sealed class MainMenu(
    IEnumerable<IScreen> screens,
    Session session,
    IClock clock,
    IUserInterface ui)
{
    private readonly IReadOnlyList<IScreen> _screens = [.. screens];

    /// <summary>
    /// Runs until the signed-in user leaves. <see langword="true"/> means they signed out and want
    /// the sign-in screen back; <see langword="false"/> means they are done with the application.
    /// </summary>
    public bool Run()
    {
        ui.Clear();
        ShowWelcome();

        while (true)
        {
            var available = AvailableScreens();
            ShowMenu(available);

            var choice = ui.AskInteger("Choose", 0, available.Count + 1);

            if (ui.InputClosed)
            {
                return false;
            }

            if (choice is null)
            {
                continue;
            }

            if (choice == 0)
            {
                ui.Blank();
                ui.Muted("Goodbye.");
                return false;
            }

            if (choice == available.Count + 1)
            {
                session.SignOut();
                return true;
            }

            RunScreen(available[choice.Value - 1]);
        }
    }

    /// <summary>The whole of the role filtering, in one line.</summary>
    private IReadOnlyList<IScreen> AvailableScreens() =>
        [.. _screens.Where(screen => screen.RequiredRole is null || screen.RequiredRole == session.CurrentRole)];

    private void RunScreen(IScreen screen)
    {
        ui.Clear();
        ui.Header(screen.Title);

        // Screens deal with expected refusals themselves.
        try
        {
            screen.Show();
        }
        catch (Exception exception)
        {
            ui.Blank();
            ui.Error($"Something went wrong: {exception.Message}");
            ui.Muted(exception.GetType().Name);
        }

        ui.Pause();
        ui.Clear();
    }

    private void ShowWelcome()
    {
        ui.Header("Event Booking System");
        ui.Muted("The menu shows only what your role may do - sign out and back in as somebody else to see the other side.");
        ui.Muted("Use 'Simulation' to move the clock and watch the time based rules fire.");
    }

    private void ShowMenu(IReadOnlyList<IScreen> available)
    {
        ui.Section($"{session.CurrentUser.FullName} - {DescribeRole()} | {Format.Moment(clock.UtcNow)}");
        ui.Blank();

        for (var index = 0; index < available.Count; index++)
        {
            ui.Write($"  {Format.Number(index + 1),2}. {available[index].Title}");
        }

        ui.Write($"  {Format.Number(available.Count + 1),2}. Sign out");
        ui.Write($"  {Format.Number(0),2}. Exit");
        ui.Blank();
    }

    private string DescribeRole() => session.CurrentUser switch
    {
        Customer customer => $"customer, {customer.Tier} tier",
        Organizer organizer => $"organiser at {organizer.OrganizationName}",
        _ => session.CurrentRole.ToString(),
    };

}
