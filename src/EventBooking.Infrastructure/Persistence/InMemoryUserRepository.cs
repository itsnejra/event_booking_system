using EventBooking.Domain.Entities;
using EventBooking.Domain.Interfaces;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Infrastructure.Persistence;

public sealed class InMemoryUserRepository()
    : InMemoryRepository<User, UserId>(user => user.Id), IUserRepository
{
    public User? FindByEmail(EmailAddress email) =>
        Snapshot.FirstOrDefault(user => user.Email == email);

    public IReadOnlyCollection<Customer> GetCustomers() => [.. Snapshot.OfType<Customer>()];

    public IReadOnlyCollection<Organizer> GetOrganizers() => [.. Snapshot.OfType<Organizer>()];
}
