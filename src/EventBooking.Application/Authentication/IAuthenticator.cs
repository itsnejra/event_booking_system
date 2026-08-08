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
    /// <remarks>
    /// A rejected sign-in gives back nothing rather than a reason. "No such address" and "wrong
    /// credentials" are the same answer on purpose: telling them apart tells a stranger which
    /// addresses are registered.
    /// </remarks>
    User? Authenticate(string emailAddress);
}
