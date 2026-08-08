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
    public static IServiceCollection AddEventBookingInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Registered twice: as itself so the concrete type resolves, and per interface for the layers above.
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
