namespace ApiGatewayKit.Core.Domain.Entities;

/// <summary>
/// Audit bilgisi tutan entity'ler iÃ§in base sÄ±nÄ±f
/// </summary>
/// <typeparam name="TId">ID tipi</typeparam>
public abstract class AuditableEntity<TId> : BaseEntity<TId>
{
    /// <summary>
    /// OluÅŸturulma tarihi (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// OluÅŸturan kullanÄ±cÄ± ID
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Son gÃ¼ncelleme tarihi (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Son gÃ¼ncelleyen kullanÄ±cÄ± ID
    /// </summary>
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Guid ID'li auditable entity
/// </summary>
public abstract class AuditableEntity : AuditableEntity<Guid>
{
}

/// <summary>
/// Soft delete destekli entity
/// </summary>
/// <typeparam name="TId">ID tipi</typeparam>
public abstract class SoftDeleteEntity<TId> : AuditableEntity<TId>
{
    /// <summary>
    /// SilinmiÅŸ mi?
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Silinme tarihi (UTC)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Silen kullanÄ±cÄ± ID
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Soft delete yapar
    /// </summary>
    public void SoftDelete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    /// <summary>
    /// Soft delete'i geri alÄ±r
    /// </summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}

/// <summary>
/// Guid ID'li soft delete entity
/// </summary>
public abstract class SoftDeleteEntity : SoftDeleteEntity<Guid>
{
}


