using EventBooking.Domain.Common;
using EventBooking.Domain.Enums;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Entities;

/// <summary>
/// Anyone who can sign in. The base class carries only what every user genuinely has; what a user
/// <em>is</em> - and what that implies - is decided by the subclass through <see cref="Role"/>.
/// </summary>
public abstract class User : Entity<UserId>
{
    protected User(UserId id, string fullName, EmailAddress email)
        : base(id)
    {
        FullName = Guard.MaxLength(Guard.NotEmpty(fullName), 120);
        Email = Guard.NotNull(email);
    }

    public string FullName { get; private set; }

    public EmailAddress Email { get; private set; }

    public abstract UserRole Role { get; }

    public void ChangeContactDetails(string fullName, EmailAddress email)
    {
        FullName = Guard.MaxLength(Guard.NotEmpty(fullName), 120);
        Email = Guard.NotNull(email);
    }

    public override string ToString() => $"{FullName} <{Email}>";
}
