
namespace EventBooking.Domain.ValueObjects;

/// <summary>A single talk in a conference programme, pinned to a track and a time slot.</summary>
public sealed record ConferenceSession(string Title, string Speaker, string Track, DateRange Slot)
{
    public override string ToString() => $"[{Track}] {Title} - {Speaker} ({Slot})";
}
