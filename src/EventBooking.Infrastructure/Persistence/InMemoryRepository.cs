using EventBooking.Domain.Interfaces;
using EventBooking.Domain.ValueObjects;

namespace EventBooking.Infrastructure.Persistence;

/// <summary>
/// Stores aggregates in a dictionary. Because the store hands back the very same object it was
/// given, a change made through an aggregate's own methods is visible immediately - which is exactly
/// how an ORM with a tracked context behaves, and why the domain needs no explicit save call.
/// </summary>
public abstract class InMemoryRepository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : class
    where TId : struct
{
    private readonly Dictionary<TId, TEntity> _entities = [];
    private readonly Func<TEntity, TId> _identity;
    private readonly Lock _gate = new();

    protected InMemoryRepository(Func<TEntity, TId> identity) => _identity = identity;

    /// <summary>Snapshot for derived repositories to run their queries against.</summary>
    protected IReadOnlyCollection<TEntity> Snapshot
    {
        get
        {
            lock (_gate)
            {
                return [.. _entities.Values];
            }
        }
    }

    public TEntity? FindById(TId id)
    {
        lock (_gate)
        {
            return _entities.GetValueOrDefault(id);
        }
    }

    public IReadOnlyCollection<TEntity> GetAll() => Snapshot;

    public void Add(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var id = _identity(entity);
        lock (_gate)
        {
            if (!_entities.TryAdd(id, entity))
            {
                throw new InvalidOperationException($"{typeof(TEntity).Name} '{id}' is already stored.");
            }
        }
    }
}
