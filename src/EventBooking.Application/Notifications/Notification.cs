using EventBooking.Domain.ValueObjects;

namespace EventBooking.Application.Notifications;

/// <summary>A message to one person. Channel-agnostic on purpose - e-mail today, SMS tomorrow.</summary>
public sealed record Notification(EmailAddress Recipient, string Subject, string Body);
