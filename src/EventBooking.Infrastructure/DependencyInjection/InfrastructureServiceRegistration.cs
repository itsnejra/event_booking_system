using EventBooking.Application.Abstractions;
using EventBooking.Application.Notifications;
using EventBooking.Domain.Interfaces;
using EventBooking.Infrastructure.Identity;
using EventBooking.Infrastructure.Messaging;
using EventBooking.Infrastructure.Notifications;
using EventBooking.Infrastructure.Persistence;
using EventBooking.Infrastructure.Seeding;
using Microsoft.Extensions.DependencyInjection;

namespace EventBooking.Infrastructure.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    /// <summary>
    /// Registers persistence, messaging and notification delivery.
    /// </summary>
    /// <remarks>
    /// Deliberately does <em>not</em> register <see cref="IClock"/>: which clock to run on is a
    /// decision for the host, and the console app wants one it can move (see <c>AdjustableClock</c>).
    /// </remarks>
    public static IServiceCollection AddEventBookingInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Each store is registered twice on purpose: once as itself, so the concrete type is
        // resolvable, and once per interface, so the layers above only ever see the abstraction.
        services.AddSingleton<InMemoryEventRepository>();
        services.AddSingleton<InMemoryBookingRepository>();
        services.AddSingleton<InMemoryUserRepository>();
        services.AddSingleton<InMemoryVenueRepository>();

        services.AddSingleton<IEventRepository>(provider => provider.GetRequiredService<InMemoryEventRepository>());
        services.AddSingleton<IBookingRepository>(provider => provider.GetRequiredService<InMemoryBookingRepository>());
        services.AddSingleton<IUserRepository>(provider => provider.GetRequiredService<InMemoryUserRepository>());
        services.AddSingleton<IVenueRepository>(provider => provider.GetRequiredService<InMemoryVenueRepository>());

        services.AddSingleton<IBookingReferenceGenerator, SequentialBookingReferenceGenerator>();
        services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddSingleton<NotificationInbox>();
        services.AddSingleton<INotificationChannel>(provider => provider.GetRequiredService<NotificationInbox>());

        services.AddSingleton<DemoDataSeeder>();

        return services;
    }
}
