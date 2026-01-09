using System.Linq.Expressions;

namespace ApiGatewayKit.Core.Application.Interfaces.Persistence;

/// <summary>
/// Generic repository interface
/// Domain katmanÄ± ile Persistence katmanÄ± arasÄ±nda soyutlama saÄŸlar
/// </summary>
/// <typeparam name="TEntity">Entity tipi</typeparam>
/// <typeparam name="TId">ID tipi</typeparam>
public interface IRepository<TEntity, TId> where TEntity : class
{
    /// <summary>
    /// ID ile entity getirir
    /// </summary>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// TÃ¼m entity'leri getirir
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// KoÅŸula gÃ¶re entity'leri getirir
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// KoÅŸula gÃ¶re tek entity getirir
    /// </summary>
    Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// SayfalanmÄ±ÅŸ sonuÃ§ getirir
    /// </summary>
    Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool ascending = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Entity ekler
    /// </summary>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Birden fazla entity ekler
    /// </summary>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Entity gÃ¼nceller
    /// </summary>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Birden fazla entity gÃ¼nceller
    /// </summary>
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Entity siler
    /// </summary>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// ID ile entity siler
    /// </summary>
    Task DeleteByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// KoÅŸula gÃ¶re entity var mÄ± kontrol eder
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// KoÅŸula gÃ¶re sayÄ± dÃ¶ndÃ¼rÃ¼r
    /// </summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Guid ID'li entity'ler iÃ§in repository
/// </summary>
public interface IRepository<TEntity> : IRepository<TEntity, Guid> where TEntity : class
{
}


