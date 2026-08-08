
namespace EventBooking.Domain.Exceptions;

/// <summary>Something was referenced by identifier but does not exist.</summary>
public sealed class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object identifier)
        : base($"{entityName} '{identifier}' does not exist.")
    {
        EntityName = entityName;
        Identifier = identifier;
    }

    public string EntityName { get; }

    public object Identifier { get; }

    public static EntityNotFoundException For<TEntity>(object identifier) =>
        new(typeof(TEntity).Name, identifier);
}
