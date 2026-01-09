namespace ApiGatewayKit.Core.Domain.Entities;

/// <summary>
/// TÃ¼m entity'lerin base sÄ±nÄ±fÄ±
/// </summary>
/// <typeparam name="TId">ID tipi</typeparam>
public abstract class BaseEntity<TId>
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Domain events listesi
    /// </summary>
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Domain events (readonly)
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Domain event ekler
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Domain event siler
    /// </summary>
    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// TÃ¼m domain event'leri temizler
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity<TId> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (Id is null || other.Id is null)
            return false;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return (GetType().ToString() + Id).GetHashCode();
    }

    public static bool operator ==(BaseEntity<TId>? left, BaseEntity<TId>? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(BaseEntity<TId>? left, BaseEntity<TId>? right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Guid ID'li entity'ler iÃ§in base sÄ±nÄ±f
/// </summary>
public abstract class BaseEntity : BaseEntity<Guid>
{
}

/// <summary>
/// Domain event interface
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}


