
namespace EventBooking.Domain.Exceptions;

/// <summary>An operation was attempted from a state that does not allow it.</summary>
public sealed class InvalidStateTransitionException : DomainException
{
    public InvalidStateTransitionException(string subject, object currentState, string attemptedAction)
        : base($"Cannot {attemptedAction} {subject} while it is {currentState}.")
    {
        Subject = subject;
        CurrentState = currentState;
        AttemptedAction = attemptedAction;
    }

    public string Subject { get; }

    public object CurrentState { get; }

    public string AttemptedAction { get; }
}
