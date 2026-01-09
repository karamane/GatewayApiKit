using ApiGatewayKit.Core.Domain.Entities.Auth;

namespace ApiGatewayKit.Core.Application.Interfaces.Persistence;

/// <summary>
/// RefreshToken repository interface
/// Token yÃ¶netimi iÅŸlemleri iÃ§in Ã¶zelleÅŸtirilmiÅŸ metodlar
/// </summary>
public interface IRefreshTokenRepository : IRepository<RefreshToken, long>
{
    /// <summary>
    /// Token deÄŸerine gÃ¶re refresh token getirir
    /// </summary>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// KullanÄ±cÄ±nÄ±n tÃ¼m aktif refresh token'larÄ±nÄ± getirir
    /// </summary>
    Task<IReadOnlyList<RefreshToken>> GetActiveTokensByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// KullanÄ±cÄ±nÄ±n tÃ¼m refresh token'larÄ±nÄ± iptal eder
    /// </summary>
    Task RevokeAllUserTokensAsync(long userId, string? revokedByIp = null, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// SÃ¼resi dolmuÅŸ token'larÄ± temizler
    /// </summary>
    Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}



