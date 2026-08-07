using EventBooking.Application.Maintenance;
using EventBooking.ConsoleApp.Ui;
using EventBooking.Domain.Users;
using EventBooking.Infrastructure.Time;

namespace EventBooking.ConsoleApp.Screens;

/// <summary>
/// Moves the clock and runs the scheduled job, so the time-dependent rules can be watched rather
/// than taken on trust: holds lapse, early bird pricing ends, refunds shrink as the date approaches,
/// an undersubscribed workshop calls itself off, finished events close.
/// </summary>
public sealed class SimulationScreen(
    AdjustableClock clock,
    MaintenanceService maintenance,
    IUserInterface ui) : IScreen
{
    private static readonly (string Label, TimeSpan Amount)[] Jumps =
    [
        ("Advance 20 minutes - lets unpaid holds lapse", TimeSpan.FromMinutes(20)),
        ("Advance 1 day", TimeSpan.FromDays(1)),
        ("Advance 20 days - past the early bird window", TimeSpan.FromDays(20)),
        ("Advance 60 days - past every seeded event", TimeSpan.FromDays(60)),
    ];

    private const string RunMaintenanceOption = "Run maintenance now";
    private const string ResetOption = "Reset the clock to real time";

    public string Title => "Simulation - move the clock, run maintenance";

    /// <summary>A demo tool rather than part of the product, so it is not tied to a role.</summary>
    public UserRole? RequiredRole => null;

    public void Show()
    {
        ui.Bullet("Now", Format.Moment(clock.UtcNow));
        ui.Bullet("Offset from real time", Format.Duration(clock.Offset));

        ui.Blank();

        var options = Jumps
            .Select(jump => jump.Label)
            .Append(RunMaintenanceOption)
            .Append(ResetOption)
            .ToList();

        var chosen = ui.Choose("Action", options, option => option);
        switch (chosen)
        {
            case null:
                return;

            case RunMaintenanceOption:
                RunMaintenance();
                return;

            case ResetOption:
                clock.Reset();
                ui.Success($"Back to real time: {Format.Moment(clock.UtcNow)}");
                return;

            default:
                Advance(Jumps.First(jump => jump.Label == chosen).Amount);
                return;
        }
    }

    private void Advance(TimeSpan amount)
    {
        clock.Advance(amount);
        ui.Success($"The clock now reads {Format.Moment(clock.UtcNow)}.");

        // Moving time forward without running the job would only ever show half the story.
        RunMaintenance();
    }

    private void RunMaintenance()
    {
        var summary = maintenance.Run();

        ui.Section("Maintenance run");
        ui.Bullet("Holds expired", Format.Number(summary.ExpiredHolds));
        ui.Bullet("Events completed", Format.Number(summary.CompletedEvents));
        ui.Bullet("Events cancelled", Format.Number(summary.CancelledEvents));
        ui.Bullet("Bookings refunded", Format.Number(summary.RefundedBookings));

        ui.Muted(summary.DidSomething
            ? "  Check the notification inbox to see what the customers were told."
            : "  Nothing was due.");
    }
}
