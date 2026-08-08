using EventBooking.Domain.Entities;

namespace EventBooking.Application.Authentication;

/// <summary>
/// Answers "who is asking", and nothing else. What that person may then do is decided by the domain
/// - the event checks its own organiser, the menu shows only the screens a role can open - so this
/// interface deliberately has no idea that roles or permissions exist.
/// </summary>
public interface IAuthenticator
{
    /// <summary>
    /// The user behind these credentials, or <see langword="null"/> if there is none.
    /// </summary>
    User? Authenticate(string emailAddress);
}
