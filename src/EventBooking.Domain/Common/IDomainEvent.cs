namespace EventBooking.Domain.Common;

/// <summary>
/// Something that happened in the domain and that the rest of the system may want to react to.
/// Domain events are past tense and immutable - they are a record of a fact, not a command.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
