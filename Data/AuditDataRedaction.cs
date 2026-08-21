using System.Text.RegularExpressions;

namespace Vantage.PMS.Data;

public static partial class AuditDataRedaction
{
    private const string RedactedValue = "[REDACTED]";

    private static readonly string[] SensitivePropertyFragments =
    [
        "PASSWORD", "APIKEY", "SECRET", "TOKEN", "EMAIL", "USERNAME",
        "PHONE", "MOBILE", "ADDRESS", "CITY", "STATE", "POSTAL", "COUNTRY",
        "FIRSTNAME", "MIDDLENAME", "LASTNAME", "BIRTH", "PASSPORT",
        "IDENTIFICATION", "IDNUMBER", "NATIONALITY", "TIN", "SSS", "GSIS",
        "PHILHEALTH", "PAGIBIG", "BANK", "CARD", "CVV", "ACCOUNTNUMBER",
        "PHOTO", "SIGNATURE", "EMERGENCY", "CONTACT"
    ];

    public static object? RedactAuditValue(string propertyName, object? value)
    {
        if (value is null)
        {
            return null;
        }

        var normalized = propertyName.ToUpperInvariant();
        return SensitivePropertyFragments.Any(fragment => normalized.Contains(fragment, StringComparison.Ordinal))
            ? RedactedValue
            : value;
    }

    public static string? RedactErrorText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var withoutConnectionSecrets = ConnectionSecretPattern().Replace(value, "$1=" + RedactedValue);
        return QuerySecretPattern().Replace(withoutConnectionSecrets, "$1" + RedactedValue);
    }

    [GeneratedRegex(@"(?i)(?<![?&])\b(password|pwd|apikey|api_key|secret|token)\s*=\s*([^;,\s&]+)")]
    private static partial Regex ConnectionSecretPattern();

    [GeneratedRegex(@"(?i)([?&](?:password|pwd|apikey|api_key|secret|token)=)[^&\s]+")]
    private static partial Regex QuerySecretPattern();
}
