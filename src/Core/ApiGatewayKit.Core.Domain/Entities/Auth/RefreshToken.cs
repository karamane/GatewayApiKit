namespace ApiGatewayKit.Core.Domain.Entities.Auth;

/// <summary>
/// Refresh token entity
/// </summary>
public class RefreshToken : BaseEntity<long>
{
    /// <summary>
    /// User ID (FK)
    /// </summary>
    public long UserId { get; private set; }

    /// <summary>
    /// Token deÄŸeri
    /// </summary>
    public string Token { get; private set; } = string.Empty;

    /// <summary>
    /// Token son kullanma tarihi
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Token oluÅŸturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Token oluÅŸturan IP adresi
    /// </summary>
    public string? CreatedByIp { get; private set; }

    /// <summary>
    /// Token iptal edilme tarihi
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// Token iptal eden IP adresi
    /// </summary>
    public string? RevokedByIp { get; private set; }

    /// <summary>
    /// Yerine geÃ§en token (rotation iÃ§in)
    /// </summary>
    public string? ReplacedByToken { get; private set; }

    /// <summary>
    /// Ä°ptal nedeni
    /// </summary>
    public string? RevokedReason { get; private set; }

    /// <summary>
    /// User navigation property
    /// </summary>
    public User User { get; private set; } = null!;

    /// <summary>
    /// Token sÃ¼resi doldu mu?
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Token iptal edildi mi?
    /// </summary>
    public bool IsRevoked => RevokedAt != null;

    /// <summary>
    /// Token aktif mi? (iptal edilmemiÅŸ ve sÃ¼resi dolmamÄ±ÅŸ)
    /// </summary>
    public bool IsActive => !IsRevoked && !IsExpired;

    /// <summary>
    /// Constructor (EF Core iÃ§in)
    /// </summary>
    protected RefreshToken() { }

    /// <summary>
    /// Yeni refresh token oluÅŸturur
    /// </summary>
    public static RefreshToken Create(
        long userId,
        string token,
        DateTime expiresAt,
        string? createdByIp = null)
    {
        return new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = createdByIp
        };
    }

    /// <summary>
    /// Token'Ä± iptal eder
    /// </summary>
    public void Revoke(string? revokedByIp = null, string? reason = null, string? replacedByToken = null)
    {
        RevokedAt = DateTime.UtcNow;
        RevokedByIp = revokedByIp;
        RevokedReason = reason;
        ReplacedByToken = replacedByToken;
    }
}



