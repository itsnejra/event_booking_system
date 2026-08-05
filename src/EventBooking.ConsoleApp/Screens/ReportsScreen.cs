using EventBooking.Application.Reporting;
using EventBooking.ConsoleApp.Ui;

namespace EventBooking.ConsoleApp.Screens;

/// <summary>Read-only view of how the platform is doing.</summary>
public sealed class ReportsScreen(ReportingService reporting, IUserInterface ui) : IScreen
{
    public string Title => "Reports";

    public void Show()
    {
        ShowSummary();
        ShowEventPerformance();
        ShowRevenueByCategory();
        ShowTopCustomers();
    }

    private void ShowSummary()
    {
        var summary = reporting.Summary();

        ui.Section("At a glance");
        ui.Bullet(
            "Events",
            $"{Format.Number(summary.TotalEvents)} "
            + $"({Format.Number(summary.PublishedEvents)} on sale, {Format.Number(summary.SoldOutEvents)} sold out)");
        ui.Bullet(
            "Bookings",
            $"{Format.Number(summary.TotalBookings)} ({Format.Number(summary.ActiveHolds)} awaiting payment)");
        ui.Bullet("Tickets sold", Format.Number(summary.TicketsSold));
        ui.Bullet("Net revenue", summary.NetRevenue.ToString());
    }

    private void ShowEventPerformance()
    {
        var reports = reporting.EventPerformance();

        ui.Section("Per event");
        if (reports.Count == 0)
        {
            ui.Muted("  No events yet.");
            return;
        }

        ui.Table(
            ["Event", "Status", "Sold", "Occupancy", "Bookings", "Cancelled", "Gross", "Refunds", "Net"],
            [
                .. reports.Select(report => new List<string>
                {
                    report.Event.Title,
                    report.Event.Status.ToString(),
                    Format.Ratio(report.TicketsSold, report.Capacity),
                    report.Occupancy.ToString(),
                    Format.Number(report.Bookings),
                    Format.Number(report.Cancellations),
                    report.GrossRevenue.ToString(),
                    report.Refunds.ToString(),
                    report.NetRevenue.ToString(),
                })
            ]);
    }

    private void ShowRevenueByCategory()
    {
        var lines = reporting.RevenueByCategory();

        ui.Section("Revenue by category");
        if (lines.Count == 0)
        {
            ui.Muted("  Nothing to report.");
            return;
        }

        ui.Table(
            ["Category", "Events", "Tickets", "Net revenue"],
            [
                .. lines.Select(line => new List<string>
                {
                    line.Category.ToString(),
                    Format.Number(line.Events),
                    Format.Number(line.TicketsSold),
                    line.NetRevenue.ToString(),
                })
            ]);
    }

    private void ShowTopCustomers()
    {
        var lines = reporting.TopCustomers();

        ui.Section("Top customers");
        if (lines.Count == 0)
        {
            ui.Muted("  Nothing to report.");
            return;
        }

        ui.Table(
            ["Customer", "Tier", "Bookings", "Tickets", "Spend"],
            [
                .. lines.Select(line => new List<string>
                {
                    line.Customer.FullName,
                    line.Customer.Tier.ToString(),
                    Format.Number(line.Bookings),
                    Format.Number(line.TicketsBought),
                    line.TotalSpend.ToString(),
                })
            ]);
    }
}
