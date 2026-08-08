using System.Text;
using EventBooking.Application.DependencyInjection;
using EventBooking.ConsoleApp;
using EventBooking.ConsoleApp.Screens;
using EventBooking.ConsoleApp.Ui;
using EventBooking.Domain.Abstractions;
using EventBooking.Infrastructure.DependencyInjection;
using EventBooking.Infrastructure.Seeding;
using EventBooking.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;

// Composition root. This is the only place in the solution that knows about every layer at once;
// everything else depends on abstractions and is handed what it needs.
Console.OutputEncoding = Encoding.UTF8;

using var provider = BuildServiceProvider();

provider.GetRequiredService<DemoDataSeeder>().Seed();

var signIn = provider.GetRequiredService<SignInScreen>();
var session = provider.GetRequiredService<Session>();
var menu = provider.GetRequiredService<MainMenu>();

// Sign in, use the application, sign out, repeat. Nothing runs without somebody signed in, and the
// menu decides what that somebody may open.
while (signIn.Show() is { } user)
{
    session.SignIn(user);

    if (!menu.Run())
    {
        break;
    }
}

static ServiceProvider BuildServiceProvider()
{
    var services = new ServiceCollection();

    services.AddEventBookingApplication();
    services.AddEventBookingInfrastructure();

    // The demo runs on a clock it can move forward. Production would register SystemClock here, and
    // nothing else in the solution would notice the difference - that is the whole point of IClock.
    services.AddSingleton<AdjustableClock>();
    services.AddSingleton<IClock>(serviceProvider => serviceProvider.GetRequiredService<AdjustableClock>());

    services.AddSingleton<IUserInterface, ConsoleUi>();
    services.AddSingleton<Session>();
    services.AddSingleton<SignInScreen>();
    services.AddSingleton<MainMenu>();

    // Registration order is menu order.
    services.AddSingleton<IScreen, EventCatalogScreen>();
    services.AddSingleton<IScreen, MyBookingsScreen>();
    services.AddSingleton<IScreen, OrganizerScreen>();
    services.AddSingleton<IScreen, ReportsScreen>();
    services.AddSingleton<IScreen, NotificationsScreen>();
    services.AddSingleton<IScreen, SimulationScreen>();

    return services.BuildServiceProvider(new ServiceProviderOptions
    {
        ValidateOnBuild = true,
        ValidateScopes = true,
    });
}
