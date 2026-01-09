using ApiGatewayKit.Core.Domain.Entities.Auth;

namespace ApiGatewayKit.Core.Application.Interfaces.Persistence;

/// <summary>
/// User repository interface
/// Authentication ve user management iÅŸlemleri iÃ§in Ã¶zelleÅŸtirilmiÅŸ metodlar
/// </summary>
public interface IUserRepository : IRepository<User, long>
{
    /// <summary>
    /// KullanÄ±cÄ± adÄ±na gÃ¶re kullanÄ±cÄ± getirir
    /// </summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Email'e gÃ¶re kullanÄ±cÄ± getirir
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// KullanÄ±cÄ± adÄ± kullanÄ±lÄ±yor mu kontrol eder
    /// </summary>
    Task<bool> IsUsernameExistsAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Email kullanÄ±lÄ±yor mu kontrol eder
    /// </summary>
    Task<bool> IsEmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// KullanÄ±cÄ±yÄ± refresh token'larÄ± ile birlikte getirir
    /// </summary>
    Task<User?> GetWithRefreshTokensAsync(long userId, CancellationToken cancellationToken = default);
}



