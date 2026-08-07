using EventBooking.Domain.Users;

namespace EventBooking.ConsoleApp;

/// <summary>
/// Who is signed in. Exactly one person at a time, with a role - which is what the menu filters on.
/// </summary>
/// <remarks>
/// <see cref="Customer"/> and <see cref="Organizer"/> narrow the signed-in user to the role a screen
/// needs. They throw rather than return null, because a screen that asks for the wrong one is a
/// wiring mistake: the menu already refuses to show a screen to a role that cannot use it.
/// </remarks>
public sealed class Session
{
    private User? _user;

    public User CurrentUser =>
        _user ?? throw new InvalidOperationException("Nobody is signed in.");

    public UserRole CurrentRole => CurrentUser.Role;

    public Customer Customer =>
        CurrentUser as Customer
        ?? throw new InvalidOperationException($"{CurrentUser.FullName} is signed in as an organiser, not a customer.");

    public Organizer Organizer =>
        CurrentUser as Organizer
        ?? throw new InvalidOperationException($"{CurrentUser.FullName} is signed in as a customer, not an organiser.");

    public void SignIn(User user) => _user = user;
}
