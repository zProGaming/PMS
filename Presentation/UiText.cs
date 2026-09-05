using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Vantage.PMS.Presentation;

public static class UiText
{
    private static readonly HashSet<string> Acronyms = new(StringComparer.OrdinalIgnoreCase)
    { "ID", "IDs", "PMS", "POS", "AR", "AP", "VAT", "BIR", "TIN", "PDF", "CSV", "UI", "UX", "QA", "AI", "API", "URL", "URLS", "SSS", "HDMF", "PHIC", "PHP", "USD", "KPI", "KPIS", "SLA", "MFA", "OTP", "IP", "SMS", "SMTP", "TLS", "SSL", "HR", "FIFO", "FEFO", "JSON", "XML", "SQL", "ETA", "ETD" };

    // For interface labels only, never guest names, entered values, stored keys, or prose.
    public static string Label(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value ?? "";
        var spaced = Regex.Replace(value, "[A-Za-z0-9]+", token => Acronyms.Contains(token.Value) ? token.Value :
            Regex.Replace(Regex.Replace(token.Value, "([a-z0-9])([A-Z])", "$1 $2"), "([A-Z]+)([A-Z][a-z])", "$1 $2"));
        return Regex.Replace(spaced, "[A-Za-z]+(?:'[A-Za-z]+)?", match =>
        {
            var word = match.Value;
            if (word.Equals("IDs", StringComparison.OrdinalIgnoreCase)) return "IDs";
            if (word.Equals("KPIs", StringComparison.OrdinalIgnoreCase)) return "KPIs";
            if (word.Equals("URLs", StringComparison.OrdinalIgnoreCase)) return "URLs";
            return Acronyms.Contains(word) || (word.Length > 1 && word.All(char.IsUpper)) ? word.ToUpperInvariant() : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
        });
    }

    public static IEnumerable<SelectListItem> GetUiEnumSelectList<T>(this IHtmlHelper html) where T : struct =>
        html.GetEnumSelectList<T>().Select(item => new SelectListItem(Label(item.Text), item.Value, item.Selected, item.Disabled) { Group = item.Group });

    public static string Display(object? value) => value is Enum ? Label(value.ToString()) : value?.ToString() ?? "";
}

public sealed class UiLabelMetadataProvider : IDisplayMetadataProvider
{
    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        if (context.Key.MetadataKind != ModelMetadataKind.Property || context.Key.Name is null) return;
        var display = context.Attributes.OfType<DisplayAttribute>().FirstOrDefault();
        context.DisplayMetadata.DisplayName = () => UiText.Label(display?.GetName() ?? context.Key.Name);
    }
}
