namespace ApiGatewayKit.Core.Shared.Constants;

/// <summary>
/// Loglama ile ilgili sabit deÄŸerler
/// </summary>
public static class LogConstants
{
    /// <summary>
    /// HTTP Header isimleri
    /// </summary>
    public static class Headers
    {
        public const string CorrelationId = "X-Correlation-ID";
        public const string ParentCorrelationId = "X-Parent-Correlation-ID";
        public const string RequestId = "X-Request-ID";
        public const string ServerName = "X-Server-Name";
    }

    /// <summary>
    /// Log tÃ¼rleri
    /// </summary>
    public static class LogTypes
    {
        public const string Request = "Request";
        public const string Response = "Response";
        public const string Exception = "Exception";
        public const string BusinessException = "BusinessException";
        public const string Audit = "Audit";
        public const string Performance = "Performance";
        public const string Security = "Security";
    }

    /// <summary>
    /// Katman isimleri
    /// </summary>
    public static class Layers
    {
        public const string ClientApi = "ClientApi";
        public const string ServerApi = "ServerApi";
        public const string Business = "Business";
        public const string Domain = "Domain";
        public const string Proxy = "Proxy";
        public const string Infrastructure = "Infrastructure";
    }

    /// <summary>
    /// Exception kategorileri
    /// </summary>
    public static class ExceptionCategories
    {
        public const string System = "System";
        public const string Database = "Database";
        public const string Network = "Network";
        public const string Business = "Business";
        public const string Validation = "Validation";
        public const string Security = "Security";
        public const string External = "External";
    }

    /// <summary>
    /// Maskelenmesi gereken alan isimleri
    /// </summary>
    public static readonly string[] SensitiveFields =
    {
        "password", "pwd", "secret", "token", "apikey", "api_key",
        "authorization", "auth", "credential", "credit_card", "creditcard",
        "card_number", "cardnumber", "cvv", "cvc", "ssn", "social_security",
        "tax_id", "taxid", "pin", "otp", "private_key", "privatekey"
    };
}

/// <summary>
/// Cache ile ilgili sabitler
/// </summary>
public static class CacheConstants
{
    public const string ParametersCacheKey = "app:parameters";
    public const string ParameterUpdateChannel = "parameter:update";
    public const int DefaultExpirationMinutes = 30;
    public const int ParameterExpirationMinutes = 60;
}

/// <summary>
/// Uygulama sabitleri
/// </summary>
public static class AppConstants
{
    public const string ApplicationName = "ApiGatewayKit";
    public const string ClientApiName = "ApiGatewayKit.Api.Client";
    public const string ServerApiName = "ApiGatewayKit.Api.Server";
}


