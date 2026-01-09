namespace ApiGatewayKit.Core.Application.Interfaces.Persistence;

/// <summary>
/// Unit of Work pattern interface
/// Transaction yÃ¶netimi ve repository koordinasyonu saÄŸlar
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// DeÄŸiÅŸiklikleri kaydeder
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Transaction baÅŸlatÄ±r
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Transaction'Ä± commit eder
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Transaction'Ä± rollback eder
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Transaction iÃ§inde iÅŸlem yapar
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transaction iÃ§inde iÅŸlem yapar (return deÄŸersiz)
    /// </summary>
    Task ExecuteInTransactionAsync(
        Func<Task> operation,
        CancellationToken cancellationToken = default);
}


