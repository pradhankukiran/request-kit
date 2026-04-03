using RequestKit.Core.Models;

namespace RequestKit.Core.Services;

public static class ContentTypeDetector
{
    public static ContentType Detect(string body, string? contentTypeHeader = null)
    {
        if (!string.IsNullOrEmpty(contentTypeHeader))
        {
            var lower = contentTypeHeader.ToLowerInvariant();
            if (lower.StartsWith("application/json") || lower.Contains("+json")) return ContentType.Json;
            if (lower.StartsWith("application/xml") || lower.StartsWith("text/xml") || lower.Contains("+xml")) return ContentType.Xml;
            if (lower.StartsWith("text/html")) return ContentType.Html;
            if (lower.Contains("text/plain")) return ContentType.PlainText;
        }

        if (string.IsNullOrWhiteSpace(body)) return ContentType.PlainText;

        var trimmed = body.TrimStart();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('[')) return ContentType.Json;
        if (trimmed.StartsWith('<'))
        {
            if (trimmed.Contains("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("<html", StringComparison.OrdinalIgnoreCase))
                return ContentType.Html;
            return ContentType.Xml;
        }

        return ContentType.PlainText;
    }

    public static string ToMonacoLanguage(ContentType type) => type switch
    {
        ContentType.Json => "json",
        ContentType.Xml => "xml",
        ContentType.Html => "html",
        _ => "plaintext"
    };
}
