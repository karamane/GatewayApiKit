using ApiGatewayKit.Core.Shared.Constants;

namespace ApiGatewayKit.Core.Application.Models.Logging;

/// <summary>
/// Audit log entry
/// Denetim loglarÄ± iÃ§in
/// </summary>
public class AuditLogEntry : BaseLogEntry
{
    public AuditLogEntry()
    {
        LogType = LogConstants.LogTypes.Audit;
    }

    #region Audit Bilgileri

    /// <summary>
    /// YapÄ±lan iÅŸlem (Create, Read, Update, Delete, Login, Logout, etc.)
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Entity tipi
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Entity ID
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Eski deÄŸerler (JSON)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// Yeni deÄŸerler (JSON)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// DeÄŸiÅŸiklik detaylarÄ±
    /// </summary>
    public List<PropertyChange>? Changes { get; set; }

    #endregion

    #region Ä°ÅŸlem DetaylarÄ±

    /// <summary>
    /// Ä°ÅŸlem baÅŸarÄ±lÄ± mÄ±?
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// BaÅŸarÄ±sÄ±zlÄ±k nedeni
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Ä°ÅŸlem sÃ¼resi (ms)
    /// </summary>
    public long? DurationMs { get; set; }

    #endregion

    /// <summary>
    /// DeÄŸiÅŸiklik ekler
    /// </summary>
    public void AddChange(string propertyName, object? oldValue, object? newValue)
    {
        Changes ??= new List<PropertyChange>();
        Changes.Add(new PropertyChange
        {
            PropertyName = propertyName,
            OldValue = oldValue,
            NewValue = newValue
        });
    }
}

/// <summary>
/// Property deÄŸiÅŸiklik bilgisi
/// </summary>
public class PropertyChange
{
    /// <summary>
    /// Property adÄ±
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Eski deÄŸer
    /// </summary>
    public object? OldValue { get; set; }

    /// <summary>
    /// Yeni deÄŸer
    /// </summary>
    public object? NewValue { get; set; }
}


