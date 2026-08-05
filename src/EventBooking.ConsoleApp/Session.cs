using EventBooking.Domain.Users;

namespace EventBooking.ConsoleApp;

/// <summary>
/// Who the demo is currently acting as.
/// </summary>
/// <remarks>
/// A real system would authenticate and hold one identity at a time. Here both a customer and an
/// organiser are signed in at once so that a reviewer can move between the two sides of the system
/// without logging in and out - a deliberate simplification of the demo, not of the model.
/// </remarks>
public sealed class Session
{
    private Customer? _customer;
    private Organizer? _organizer;

    public Customer Customer =>
        _customer ?? throw new InvalidOperationException("No customer is signed in.");

    public Organizer Organizer =>
        _organizer ?? throw new InvalidOperationException("No organiser is signed in.");

    public void SignIn(Customer customer, Organizer organizer)
    {
        _customer = customer;
        _organizer = organizer;
    }

    public void SwitchCustomer(Customer customer) => _customer = customer;

    public void SwitchOrganizer(Organizer organizer) => _organizer = organizer;
}
