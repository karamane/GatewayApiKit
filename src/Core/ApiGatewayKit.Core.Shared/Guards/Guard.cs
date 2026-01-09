using ApiGatewayKit.Core.Shared.Exceptions;

namespace ApiGatewayKit.Core.Shared.Guards;

/// <summary>
/// Guard clause implementasyonu - Defensive programming iÃ§in
/// </summary>
public static class Guard
{
    /// <summary>
    /// DeÄŸerin null olmadÄ±ÄŸÄ±nÄ± kontrol eder
    /// </summary>
    public static T AgainstNull<T>(T? value, string parameterName) where T : class
    {
        if (value is null)
            throw new ArgumentNullException(parameterName, $"{parameterName} cannot be null.");
        
        return value;
    }

    /// <summary>
    /// String'in null veya boÅŸ olmadÄ±ÄŸÄ±nÄ± kontrol eder
    /// </summary>
    public static string AgainstNullOrEmpty(string? value, string parameterName)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException($"{parameterName} cannot be null or empty.", parameterName);
        
        return value;
    }

    /// <summary>
    /// String'in null, boÅŸ veya whitespace olmadÄ±ÄŸÄ±nÄ± kontrol eder
    /// </summary>
    public static string AgainstNullOrWhiteSpace(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{parameterName} cannot be null, empty, or whitespace.", parameterName);
        
        return value;
    }

    /// <summary>
    /// SayÄ±nÄ±n negatif olmadÄ±ÄŸÄ±nÄ± kontrol eder
    /// </summary>
    public static int AgainstNegative(int value, string parameterName)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(parameterName, value, $"{parameterName} cannot be negative.");
        
        return value;
    }

    /// <summary>
    /// SayÄ±nÄ±n sÄ±fÄ±r veya negatif olmadÄ±ÄŸÄ±nÄ± kontrol eder
    /// </summary>
    public static int AgainstNegativeOrZero(int value, string parameterName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(parameterName, value, $"{parameterName} must be greater than zero.");
        
        return value;
    }

    /// <summary>
    /// DeÄŸerin belirtilen aralÄ±kta olduÄŸunu kontrol eder
    /// </summary>
    public static int AgainstOutOfRange(int value, string parameterName, int min, int max)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(parameterName, value, 
                $"{parameterName} must be between {min} and {max}.");
        
        return value;
    }

    /// <summary>
    /// Koleksiyonun null veya boÅŸ olmadÄ±ÄŸÄ±nÄ± kontrol eder
    /// </summary>
    public static IEnumerable<T> AgainstNullOrEmpty<T>(IEnumerable<T>? value, string parameterName)
    {
        if (value is null || !value.Any())
            throw new ArgumentException($"{parameterName} cannot be null or empty.", parameterName);
        
        return value;
    }

    /// <summary>
    /// GUID'in boÅŸ olmadÄ±ÄŸÄ±nÄ± kontrol eder
    /// </summary>
    public static Guid AgainstEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
            throw new ArgumentException($"{parameterName} cannot be empty GUID.", parameterName);
        
        return value;
    }

    /// <summary>
    /// KoÅŸulun doÄŸru olduÄŸunu kontrol eder, deÄŸilse business exception fÄ±rlatÄ±r
    /// </summary>
    public static void AgainstBusinessRule(bool condition, string errorMessage, string errorCode = "BUSINESS_RULE_VIOLATION")
    {
        if (condition)
            throw new BusinessException(errorMessage, errorCode);
    }

    /// <summary>
    /// KaynaÄŸÄ±n bulunduÄŸunu kontrol eder
    /// </summary>
    public static T AgainstNotFound<T>(T? value, string resourceType, string resourceId) where T : class
    {
        if (value is null)
            throw new NotFoundException(resourceType, resourceId);
        
        return value;
    }

    /// <summary>
    /// String uzunluÄŸunun maksimum deÄŸeri aÅŸmadÄ±ÄŸÄ±nÄ± kontrol eder
    /// </summary>
    public static string AgainstMaxLength(string? value, int maxLength, string parameterName)
    {
        if (!string.IsNullOrEmpty(value) && value.Length > maxLength)
            throw new ArgumentException($"{parameterName} cannot exceed {maxLength} characters.", parameterName);
        
        return value ?? string.Empty;
    }

    /// <summary>
    /// Email formatÄ±nÄ±n geÃ§erli olduÄŸunu kontrol eder
    /// </summary>
    public static string AgainstInvalidEmail(string? value, string parameterName)
    {
        AgainstNullOrWhiteSpace(value, parameterName);
        
        if (!value!.Contains('@') || !value.Contains('.'))
            throw new ValidationException(parameterName, "Invalid email format.");
        
        return value;
    }
}


