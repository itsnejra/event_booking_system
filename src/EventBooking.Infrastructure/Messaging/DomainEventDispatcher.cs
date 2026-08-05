using System.Collections.Concurrent;
using EventBooking.Application.Abstractions;
using EventBooking.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace EventBooking.Infrastructure.Messaging;

/// <summary>
/// Delivers collected domain events to every handler registered for their concrete type.
/// </summary>
/// <remarks>
/// The awkward part of this pattern is that the events arrive as <see cref="IDomainEvent"/> while the
/// handlers are generic. Rather than reflecting over <c>Handle</c> on every dispatch, a tiny typed
/// invoker is built once per event type and cached - so the reflection cost is paid at most once per
/// kind of event, and the actual call is an ordinary virtual call.
/// </remarks>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, HandlerInvoker> Invokers = new();

    public void Dispatch(params IHasDomainEvents[] aggregates)
    {
        ArgumentNullException.ThrowIfNull(aggregates);

        var pending = new List<IDomainEvent>();

        // Drain first, handle second. A handler that touches one of these aggregates would otherwise
        // add to the very list being iterated.
        foreach (var aggregate in aggregates)
        {
            if (aggregate.DomainEvents.Count == 0)
            {
                continue;
            }

            pending.AddRange(aggregate.DomainEvents);
            aggregate.ClearDomainEvents();
        }

        foreach (var domainEvent in pending)
        {
            Invokers
                .GetOrAdd(domainEvent.GetType(), CreateInvoker)
                .Invoke(domainEvent, serviceProvider);
        }
    }

    private static HandlerInvoker CreateInvoker(Type domainEventType) =>
        (HandlerInvoker)Activator.CreateInstance(typeof(TypedHandlerInvoker<>).MakeGenericType(domainEventType))!;

    private abstract class HandlerInvoker
    {
        public abstract void Invoke(IDomainEvent domainEvent, IServiceProvider serviceProvider);
    }

    private sealed class TypedHandlerInvoker<TDomainEvent> : HandlerInvoker
        where TDomainEvent : IDomainEvent
    {
        public override void Invoke(IDomainEvent domainEvent, IServiceProvider serviceProvider)
        {
            foreach (var handler in serviceProvider.GetServices<IDomainEventHandler<TDomainEvent>>())
            {
                handler.Handle((TDomainEvent)domainEvent);
            }
        }
    }
}
