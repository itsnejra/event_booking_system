using EventBooking.Domain.Users;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Abstractions;

public interface IUserRepository : IRepository<User, UserId>
{
    User? FindByEmail(EmailAddress email);

    IReadOnlyCollection<Customer> GetCustomers();

    IReadOnlyCollection<Organizer> GetOrganizers();
}
