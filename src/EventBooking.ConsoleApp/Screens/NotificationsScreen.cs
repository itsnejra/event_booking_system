using EventBooking.ConsoleApp.Ui;
using EventBooking.Domain.Users;
using EventBooking.Infrastructure.Notifications;

namespace EventBooking.ConsoleApp.Screens;

/// <summary>
/// What a mail server would have received. Every message here was produced by a domain event
/// handler - no service in the system knows that notifications exist.
/// </summary>
public sealed class NotificationsScreen(NotificationInbox inbox, Session session, IUserInterface ui) : IScreen
{
    public string Title => "Notification inbox";

    /// <summary>A demo window onto the outbound mail, useful to whoever is signed in.</summary>
    public UserRole? RequiredRole => null;

    public void Show()
    {
        var messages = inbox.Messages;
        var mine = inbox.For(session.CurrentUser.Email);

        ui.Section($"{Format.Number(messages.Count)} message(s) delivered in total");
        ui.Muted($"  {Format.Number(mine.Count)} of them addressed to {session.CurrentUser.Email}.");

        if (messages.Count == 0)
        {
            return;
        }

        var selected = ui.Confirm("Show only my messages?") ? mine : messages;

        ui.Blank();
        foreach (var delivered in selected.OrderByDescending(message => message.SentAt))
        {
            ui.Write($"  {Format.Moment(delivered.SentAt)}  to {delivered.Notification.Recipient}");
            ui.Write($"    {delivered.Notification.Subject}");
            ui.Muted($"    {delivered.Notification.Body}");
            ui.Blank();
        }
    }
}
