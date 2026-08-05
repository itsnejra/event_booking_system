using System.Globalization;
using EventBooking.Domain.Events;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.ConsoleApp.Ui;

/// <summary>
/// Display formatting, and the single place that decides which culture the console renders in.
/// </summary>
/// <remarks>
/// The domain formats itself for logs and messages; how a date or a number looks on screen is a
/// presentation decision. Keeping it here also means no screen has to remember to pass a format
/// provider - if it goes through <c>Format</c>, it is already right.
/// </remarks>
public static class Format
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static string Number(int value) => value.ToString(Culture);

    public static string Number(decimal value) => value.ToString("N2", Culture);

    public static string Moment(DateTimeOffset value) =>
        value.ToLocalTime().ToString("dd.MM.yyyy HH:mm", Culture);

    public static string Day(DateTimeOffset value) =>
        value.ToLocalTime().ToString("dd.MM.yyyy", Culture);

    public static string Range(DateRange range)
    {
        ArgumentNullException.ThrowIfNull(range);

        var start = range.Start.ToLocalTime();
        var end = range.End.ToLocalTime();

        return start.Date == end.Date
            ? $"{Moment(range.Start)}-{end.ToString("HH:mm", Culture)}"
            : $"{Moment(range.Start)} - {Moment(range.End)}";
    }

    public static string Duration(TimeSpan value) => value switch
    {
        { TotalDays: >= 1 } => $"{value.TotalDays.ToString("0.#", Culture)} day(s)",
        { TotalHours: >= 1 } => $"{value.TotalHours.ToString("0.#", Culture)} hour(s)",
        _ => $"{value.TotalMinutes.ToString("0", Culture)} minute(s)",
    };

    public static string Ratio(int part, int whole) => $"{Number(part)}/{Number(whole)}";

    public static string Availability(Event @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        return @event.IsSoldOut ? "SOLD OUT" : Ratio(@event.AvailableTickets, @event.TotalCapacity);
    }
}
