using EventBooking.Domain.Entities;
using EventBooking.Domain.Interfaces;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Application.Authentication;

/// <summary>
/// Identity by a known address, with nothing to prove it.
/// </summary>
public sealed class DirectoryAuthenticator(IUserRepository users) : IAuthenticator
{
    public User? Authenticate(string emailAddress) =>
        EmailAddress.TryCreate(emailAddress, out var address)
            ? users.FindByEmail(address)
            : null;
}
