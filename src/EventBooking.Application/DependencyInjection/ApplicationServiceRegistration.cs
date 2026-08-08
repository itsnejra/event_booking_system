using EventBooking.Application.Abstractions;
using EventBooking.Application.Authentication;
using EventBooking.Application.Bookings;
using EventBooking.Application.Catalog;
using EventBooking.Application.Maintenance;
using EventBooking.Application.Notifications;
using EventBooking.Application.Notifications.Handlers;
using EventBooking.Application.Reporting;
using EventBooking.Domain.Bookings;
using EventBooking.Domain.Events;
using EventBooking.Domain.Pricing;
using EventBooking.Domain.Pricing.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace EventBooking.Application.DependencyInjection;

/// <summary>
/// Everything this layer needs, in one place. Each layer registering its own services is what keeps
/// the host from having to know the internals of any of them.
/// </summary>
public static class ApplicationServiceRegistration
{
    /// <summary>
    /// Registers application services, the pricing rule set and the domain event handlers.
    /// The caller supplies persistence, the clock and a notification channel.
    /// </summary>
    /// <remarks>
    /// Singleton lifetimes match the in-memory store used by the console. A web host would make the
    /// services and repositories scoped per request; only these lines would change.
    /// </remarks>
    public static IServiceCollection AddEventBookingApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Pricing rules are registered individually and collected by the engine. Adding a promotion
        // is one more line here plus one new class - no existing code is touched.
        services.AddSingleton<IPricingRule>(EarlyBirdDiscountRule.Default);
        services.AddSingleton<IPricingRule>(GroupDiscountRule.Default);
        services.AddSingleton<IPricingRule>(LoyaltyDiscountRule.Default);
        services.AddSingleton(provider => new PricingEngine(provider.GetServices<IPricingRule>()));

        services.AddSingleton(ReportingOptions.Default);

        // Swapping in a real sign-in means changing this line and nothing else.
        services.AddSingleton<IAuthenticator, DirectoryAuthenticator>();

        services.AddSingleton<CustomerNotifier>();
        services.AddSingleton<BookingService>();
        services.AddSingleton<EventCatalogService>();
        services.AddSingleton<MaintenanceService>();
        services.AddSingleton<ReportingService>();

        return services.AddDomainEventHandlers();
    }

    private static IServiceCollection AddDomainEventHandlers(this IServiceCollection services)
    {
        services.AddSingleton<IDomainEventHandler<BookingConfirmedDomainEvent>, BookingConfirmedNotificationHandler>();
        services.AddSingleton<IDomainEventHandler<BookingCancelledDomainEvent>, BookingCancelledNotificationHandler>();
        services.AddSingleton<IDomainEventHandler<BookingExpiredDomainEvent>, BookingExpiredNotificationHandler>();
        services.AddSingleton<IDomainEventHandler<EventRescheduledDomainEvent>, EventRescheduledNotificationHandler>();
        services.AddSingleton<IDomainEventHandler<TicketsReleasedDomainEvent>, WaitlistNotificationHandler>();

        return services;
    }
}
