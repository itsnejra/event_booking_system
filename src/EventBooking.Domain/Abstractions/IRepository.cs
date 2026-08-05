namespace EventBooking.Domain.Abstractions;

/// <summary>
/// Collection-like access to a set of aggregates. The interface stays deliberately small:
/// there is no <c>Update</c> and no <c>Remove</c>, because nothing in this system needs them -
/// aggregates are mutated through their own methods and events are cancelled rather than deleted.
/// A database-backed implementation would add a unit of work around this; see the README.
/// </summary>
public interface IRepository<TEntity, in TId>
    where TEntity : class
    where TId : struct
{
    TEntity? FindById(TId id);

    IReadOnlyCollection<TEntity> GetAll();

    void Add(TEntity entity);
}
