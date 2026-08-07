using EventBooking.ConsoleApp.Ui;
using EventBooking.Domain.Abstractions;
using EventBooking.Domain.Users;

namespace EventBooking.ConsoleApp.Screens;

/// <summary>
/// The loop. It knows how to show the screens the signed-in user is allowed to open, and how to
/// survive anything they throw; it knows nothing whatsoever about booking.
/// </summary>
public sealed class MainMenu(
    IEnumerable<IScreen> screens,
    Session session,
    IUserRepository users,
    IClock clock,
    IUserInterface ui)
{
    private readonly IReadOnlyList<IScreen> _screens = [.. screens];

    public void Run()
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
                return;
            }

            if (choice is null)
            {
                continue;
            }

            if (choice == 0)
            {
                ui.Blank();
                ui.Muted("Goodbye.");
                return;
            }

            if (choice == available.Count + 1)
            {
                SwitchUser();
                continue;
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

        // Screens deal with expected refusals themselves. This is the net for everything else, so
        // that one unexpected failure does not take the whole session down with it.
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
        ui.Muted("Demo data is loaded. The menu shows only what your role may do - switch user to see the other side.");
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

        ui.Write($"  {Format.Number(available.Count + 1),2}. Switch user");
        ui.Write($"  {Format.Number(0),2}. Exit");
        ui.Blank();
    }

    private string DescribeRole() => session.CurrentUser switch
    {
        Customer customer => $"customer, {customer.Tier} tier",
        Organizer organizer => $"organiser at {organizer.OrganizationName}",
        _ => session.CurrentRole.ToString(),
    };

    private void SwitchUser()
    {
        ui.Section("Switch user");
        ui.Muted("  The menu changes with the role you pick.");

        var everyone = users.GetAll().OrderBy(user => user.Role).ThenBy(user => user.FullName).ToList();
        var chosen = ui.Choose("Sign in as", everyone, Describe);

        if (chosen is not null)
        {
            session.SignIn(chosen);
            ui.Success($"Now signed in as {chosen.FullName}.");
        }

        ui.Pause();
        ui.Clear();
    }

    private static string Describe(User user) => user switch
    {
        Customer customer =>
            $"{customer.FullName,-16} customer   {customer.Tier,-8} "
            + $"{Format.Number(customer.CompletedBookings)} completed booking(s)",
        Organizer organizer => $"{organizer.FullName,-16} organiser  {organizer.OrganizationName}",
        _ => user.FullName,
    };
}
