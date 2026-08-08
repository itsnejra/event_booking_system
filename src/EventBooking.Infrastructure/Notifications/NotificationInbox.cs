using EventBooking.Application.Notifications;
using EventBooking.Domain.Interfaces;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Infrastructure.Notifications;

/// <summary>A delivered message, with the moment it was sent.</summary>
public sealed record DeliveredNotification(Notification Notification, DateTimeOffset SentAt);

/// <summary>
/// Collects messages instead of sending them. Printing straight to the console would interleave with
/// whatever menu is on screen; keeping an inbox lets the demo show exactly what a real mail server
/// would have received, on request.
/// </summary>
public sealed class NotificationInbox(IClock clock) : INotificationChannel
{
    private readonly List<DeliveredNotification> _messages = [];
    private readonly Lock _gate = new();

    public IReadOnlyList<DeliveredNotification> Messages
    {
        get
        {
            lock (_gate)
            {
                return [.. _messages];
            }
        }
    }

    public void Send(Notification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        lock (_gate)
        {
            _messages.Add(new DeliveredNotification(notification, clock.UtcNow));
        }
    }

    public IReadOnlyList<DeliveredNotification> For(EmailAddress recipient) =>
        [.. Messages.Where(message => message.Notification.Recipient == recipient)];

    public void Clear()
    {
        lock (_gate)
        {
            _messages.Clear();
        }
    }
}
