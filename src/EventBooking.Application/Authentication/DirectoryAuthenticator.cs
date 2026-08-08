using EventBooking.Domain.Entities;
using EventBooking.Domain.Interfaces;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Application.Authentication;

/// <summary>
/// Identity by a known address, with nothing to prove it.
/// </summary>
/// <remarks>
/// This is the whole authentication story of the demo, and it is deliberately thin: with the data in
/// memory, a password would be checked against a hash that disappears when the process exits - the
/// look of security without any of it.
/// <para>
/// What matters is that the boundary exists. A real implementation - password, token, single sign-on
/// - replaces this one class and registers itself in place of it. Nothing above changes, because
/// every caller depends on <see cref="IAuthenticator"/>, and authorisation was never in here to
/// begin with.
/// </para>
/// </remarks>
public sealed class DirectoryAuthenticator(IUserRepository users) : IAuthenticator
{
    public User? Authenticate(string emailAddress) =>
        EmailAddress.TryCreate(emailAddress, out var address)
            ? users.FindByEmail(address)
            : null;
}
