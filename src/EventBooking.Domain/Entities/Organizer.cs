using EventBooking.Domain.Common;
using EventBooking.Domain.Enums;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Domain.Entities;

/// <summary>Someone who creates and runs events on behalf of an organisation.</summary>
public sealed class Organizer : User
{
    public Organizer(UserId id, string fullName, EmailAddress email, string organizationName)
        : base(id, fullName, email)
    {
        OrganizationName = Guard.MaxLength(Guard.NotEmpty(organizationName), 120);
    }

    public string OrganizationName { get; }

    public override UserRole Role => UserRole.Organizer;

    public override string ToString() => $"{FullName} ({OrganizationName})";
}
