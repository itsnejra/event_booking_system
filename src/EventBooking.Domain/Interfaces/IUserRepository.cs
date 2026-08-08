using EventBooking.Domain.Entities;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Interfaces;

public interface IUserRepository : IRepository<User, UserId>
{
    User? FindByEmail(EmailAddress email);

    IReadOnlyCollection<Customer> GetCustomers();

    IReadOnlyCollection<Organizer> GetOrganizers();
}
