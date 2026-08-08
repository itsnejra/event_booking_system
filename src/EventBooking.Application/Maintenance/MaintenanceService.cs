using EventBooking.Application.Abstractions;
using EventBooking.Application.Bookings;
using EventBooking.Domain.Enums;
using EventBooking.Domain.Interfaces;

namespace EventBooking.Application.Maintenance;

/// <summary>What just happened during a maintenance run.</summary>
public sealed record MaintenanceSummary(int ExpiredHolds, int CompletedEvents, int CancelledEvents, int RefundedBookings)
{
    public bool DidSomething => ExpiredHolds + CompletedEvents + CancelledEvents > 0;
}

/// <summary>
/// The scheduled job of the system, gathered in one place: expire abandoned holds, close events that
/// are over, and let each event run whatever periodic check its own type defines.
/// </summary>
/// <remarks>
/// In production a timer would call <see cref="Run"/>. Here it is a menu item, which makes the
/// time-dependent behaviour something you can watch happen rather than something you take on faith.
/// </remarks>
public sealed class MaintenanceService(
    IEventRepository events,
    BookingService bookings,
    IDomainEventDispatcher dispatcher,
    IClock clock)
{
    public MaintenanceSummary Run()
    {
        var expiredHolds = bookings.ExpireStaleHolds();
        var completed = 0;
        var cancelled = 0;
        var refunded = 0;

        foreach (var @event in events.GetAll())
        {
            var statusBefore = @event.Status;
            @event.RunScheduledMaintenance(clock.UtcNow);

            if (@event.Status == statusBefore)
            {
                continue;
            }

            dispatcher.Dispatch(@event);

            switch (@event.Status)
            {
                case EventStatus.Completed:
                    completed++;
                    break;

                case EventStatus.Cancelled:
                    cancelled++;
                    refunded += bookings.CancelAllForEvent(
                        @event,
                        @event.CancellationReason ?? "The event was cancelled.").Count;
                    break;

                default:
                    break;
            }
        }

        return new MaintenanceSummary(expiredHolds, completed, cancelled, refunded);
    }
}
