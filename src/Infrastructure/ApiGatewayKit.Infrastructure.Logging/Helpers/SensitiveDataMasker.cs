using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ApiGatewayKit.Infrastructure.Logging.Services;
using Microsoft.Extensions.Options;

namespace ApiGatewayKit.Infrastructure.Logging.Helpers;

/// <summary>
/// Hassas veri maskeleme servisi
/// KonfigÃ¼rasyondan okunan sensitive fields ile Ã§alÄ±ÅŸÄ±r
/// </summary>
public interface ISensitiveDataMasker
{
    /// <summary>
    /// JSON iÃ§indeki hassas alanlarÄ± maskeler
    /// </summary>
    string? MaskJson(string? json);

    /// <summary>
    /// Text iÃ§indeki hassas verileri maskeler
    /// </summary>
    string MaskText(string text);

    /// <summary>
    /// Log injection saldÄ±rÄ±larÄ±nÄ± Ã¶nlemek iÃ§in string'i temizler
    /// </summary>
    string SanitizeForLogging(string? input);

    /// <summary>
    /// Dictionary iÃ§indeki hassas alanlarÄ± maskeler
    /// </summary>
    Dictionary<string, string> MaskDictionary(Dictionary<string, string>? dictionary);
}

/// <summary>
/// Hassas verileri maskeleyen servis implementasyonu
/// KonfigÃ¼rasyondan sensitive fields okur
/// Static metodlar da saÄŸlar (DI olmadan kullanÄ±m iÃ§in)
/// </summary>
public partial class SensitiveDataMasker : ISensitiveDataMasker
{
    private const string MaskedValue = "***MASKED***";
    private readonly HashSet<string> _sensitiveFields;
    private readonly SensitiveDataOptions _options;

    // Static instance (varsayÄ±lan options ile)
    private static readonly Lazy<SensitiveDataMasker> _defaultInstance = new(() =>
        new SensitiveDataMasker(Microsoft.Extensions.Options.Options.Create(new SensitiveDataOptions())));

    /// <summary>
    /// VarsayÄ±lan instance (DI olmadan kullanÄ±m iÃ§in)
    /// </summary>
    public static SensitiveDataMasker Default => _defaultInstance.Value;

    public SensitiveDataMasker(IOptions<SensitiveDataOptions> options)
    {
        _options = options.Value;
        _sensitiveFields = new HashSet<string>(_options.SensitiveFields, StringComparer.OrdinalIgnoreCase);
    }


    /// <summary>
    /// JSON iÃ§indeki hassas alanlarÄ± maskeler
    /// </summary>
    public string? MaskJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        try
        {
            using var doc = JsonDocument.Parse(json);
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });

            MaskElement(doc.RootElement, writer);

            writer.Flush();
            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch
        {
            // JSON parse edilemezse text olarak maskele
            return MaskText(json);
        }
    }

    /// <summary>
    /// Text iÃ§indeki hassas verileri maskeler
    /// </summary>
    public string MaskText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = text;

        // Key-value pattern'leri maskele
        foreach (var field in _sensitiveFields)
        {
            // "field": "value" pattern
            var jsonPattern = new Regex(
                $"\"{field}\"\\s*:\\s*\"[^\"]*\"",
                RegexOptions.IgnoreCase);
            result = jsonPattern.Replace(result, $"\"{field}\": \"{MaskedValue}\"");

            // field=value pattern (query string)
            var queryPattern = new Regex(
                $"{field}=[^&\\s]*",
                RegexOptions.IgnoreCase);
            result = queryPattern.Replace(result, $"{field}={MaskedValue}");
        }

        // Kredi kartÄ± ve email maskeleme
        result = MaskTextValue(result) ?? result;

        return result;
    }

    /// <summary>
    /// Dictionary iÃ§indeki hassas alanlarÄ± maskeler
    /// </summary>
    public Dictionary<string, string> MaskDictionary(Dictionary<string, string>? dictionary)
    {
        if (dictionary == null)
            return new Dictionary<string, string>();

        var result = new Dictionary<string, string>();

        foreach (var kvp in dictionary)
        {
            if (IsSensitiveField(kvp.Key))
            {
                result[kvp.Key] = MaskedValue;
            }
            else
            {
                result[kvp.Key] = MaskTextValue(kvp.Value) ?? kvp.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Log injection saldÄ±rÄ±larÄ±nÄ± Ã¶nlemek iÃ§in string'i temizler
    /// </summary>
    public string SanitizeForLogging(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Newline karakterlerini temizle
        var sanitized = input
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\t", " ");

        // ANSI escape sequence'leri temizle
        sanitized = AnsiEscapeRegex().Replace(sanitized, "");

        // Control karakterleri temizle
        sanitized = new string(sanitized.Where(c => !char.IsControl(c) || c == ' ').ToArray());

        return sanitized;
    }

    #region Private Methods

    private void MaskElement(
        JsonElement element,
        Utf8JsonWriter writer,
        string? propertyName = null)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    if (IsSensitiveField(property.Name))
                    {
                        writer.WriteStringValue(MaskedValue);
                    }
                    else
                    {
                        MaskElement(property.Value, writer, property.Name);
                    }
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    MaskElement(item, writer);
                }
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                var value = element.GetString();
                writer.WriteStringValue(MaskTextValue(value));
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    private bool IsSensitiveField(string fieldName)
    {
        return _sensitiveFields.Any(f =>
            fieldName.Contains(f, StringComparison.OrdinalIgnoreCase));
    }

    private string? MaskTextValue(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Kredi kartÄ± maskele
        if (_options.MaskCreditCards)
        {
            text = CreditCardRegex().Replace(text, m =>
                $"****-****-****-{m.Value[^4..]}");
        }

        // Email kÄ±smi maskele
        if (_options.MaskEmails)
        {
            text = EmailRegex().Replace(text, m =>
            {
                var parts = m.Value.Split('@');
                if (parts[0].Length > 2)
                    return $"{parts[0][..2]}***@{parts[1]}";
                return $"***@{parts[1]}";
            });
        }

        // Telefon numarasÄ± maskele
        if (_options.MaskPhoneNumbers)
        {
            text = PhoneRegex().Replace(text, m =>
            {
                if (m.Value.Length > 4)
                    return $"***{m.Value[^4..]}";
                return MaskedValue;
            });
        }

        // IBAN maskele
        if (_options.MaskIbans)
        {
            text = IbanRegex().Replace(text, m =>
            {
                if (m.Value.Length > 4)
                    return $"{m.Value[..4]}***{m.Value[^4..]}";
                return MaskedValue;
            });
        }

        return text;
    }

    [GeneratedRegex(@"\b(?:\d{4}[-\s]?){3}\d{4}\b")]
    private static partial Regex CreditCardRegex();

    [GeneratedRegex(@"\b[\w\.-]+@[\w\.-]+\.\w+\b")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\x1B\[[0-9;]*[a-zA-Z]")]
    private static partial Regex AnsiEscapeRegex();

    [GeneratedRegex(@"\b(?:\+90|0)?[5][0-9]{9}\b")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"\bTR\d{24}\b", RegexOptions.IgnoreCase)]
    private static partial Regex IbanRegex();

    #endregion
}

/// <summary>
/// Sensitive data maskeleme seÃ§enekleri
/// appsettings.json'dan okunur
/// </summary>
public class SensitiveDataOptions
{
    public const string SectionName = "SensitiveData";

    /// <summary>
    /// Maskelenecek alan adlarÄ±
    /// </summary>
    public string[] SensitiveFields { get; set; } = new[]
    {
        "password",
        "pwd",
        "secret",
        "token",
        "accessToken",
        "refreshToken",
        "apiKey",
        "api_key",
        "authorization",
        "auth",
        "credential",
        "creditCard",
        "credit_card",
        "cardNumber",
        "card_number",
        "cvv",
        "cvc",
        "pin",
        "ssn",
        "socialSecurity",
        "tckn",
        "tcKimlikNo",
        "identityNumber",
        "nationalId",
        "privateKey",
        "private_key",
        "connectionString",
        "connection_string"
    };

    /// <summary>
    /// Kredi kartÄ± numaralarÄ±nÄ± otomatik maskele
    /// </summary>
    public bool MaskCreditCards { get; set; } = true;

    /// <summary>
    /// Email adreslerini kÄ±smi maskele
    /// </summary>
    public bool MaskEmails { get; set; } = true;

    /// <summary>
    /// Telefon numaralarÄ±nÄ± maskele
    /// </summary>
    public bool MaskPhoneNumbers { get; set; } = true;

    /// <summary>
    /// IBAN'larÄ± maskele
    /// </summary>
    public bool MaskIbans { get; set; } = true;

    /// <summary>
    /// Maskeleme karakteri
    /// </summary>
    public string MaskCharacter { get; set; } = "*";

    /// <summary>
    /// Maskeleme metni
    /// </summary>
    public string MaskedText { get; set; } = "***MASKED***";
}

