using EventBooking.ConsoleApp.Ui;
using EventBooking.Domain.Abstractions;
using EventBooking.Domain.Users;

namespace EventBooking.ConsoleApp.Screens;

/// <summary>
/// The loop. It knows how to show a list of screens and how to survive anything they throw;
/// it knows nothing whatsoever about booking.
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
            ShowMenu();

            var choice = ui.AskInteger("Choose", 0, _screens.Count + 1);

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

            if (choice == _screens.Count + 1)
            {
                SwitchUser();
                continue;
            }

            RunScreen(_screens[choice.Value - 1]);
        }
    }

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
        ui.Muted("Demo data is loaded. Use 'Simulation' to move the clock and watch the time based rules fire.");
    }

    private void ShowMenu()
    {
        ui.Section($"Signed in as {session.Customer.FullName} ({session.Customer.Tier})"
            + $" | organiser: {session.Organizer.OrganizationName}"
            + $" | {Format.Moment(clock.UtcNow)}");
        ui.Blank();

        for (var index = 0; index < _screens.Count; index++)
        {
            ui.Write($"  {Format.Number(index + 1),2}. {_screens[index].Title}");
        }

        ui.Write($"  {Format.Number(_screens.Count + 1),2}. Switch user");
        ui.Write($"  {Format.Number(0),2}. Exit");
        ui.Blank();
    }

    private void SwitchUser()
    {
        ui.Section("Switch user");

        var customer = ui.Choose("Continue as", [.. users.GetCustomers()], Describe);
        if (customer is not null)
        {
            session.SwitchCustomer(customer);
            ui.Success($"Now acting as {customer.FullName}.");
        }

        var organizer = ui.Choose<Organizer>(
            "Organiser",
            [.. users.GetOrganizers()],
            item => $"{item.FullName} - {item.OrganizationName}");

        if (organizer is not null)
        {
            session.SwitchOrganizer(organizer);
            ui.Success($"Organiser set to {organizer.OrganizationName}.");
        }

        ui.Pause();
        ui.Clear();
    }

    private static string Describe(Customer customer) =>
        $"{customer.FullName,-16} {customer.Tier,-8} "
        + $"{Format.Number(customer.CompletedBookings)} completed booking(s)";
}
