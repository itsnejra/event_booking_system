using EventBooking.Domain.Entities;
using EventBooking.Domain.Enums;

namespace EventBooking.ConsoleApp;

/// <summary>
/// Who is signed in. Exactly one person at a time, with a role - which is what the menu filters on.
/// </summary>
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

    public bool IsSignedIn => _user is not null;

    public void SignIn(User user) => _user = user ?? throw new ArgumentNullException(nameof(user));

    public void SignOut() => _user = null;
}
