namespace EventBooking.Application.Notifications;

/// <summary>
/// Somewhere to deliver a <see cref="Notification"/>. The console implementation used by the demo and
/// a real SMTP client differ only in this one method.
/// </summary>
public interface INotificationChannel
{
    void Send(Notification notification);
}
