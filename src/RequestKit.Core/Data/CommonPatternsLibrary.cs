using RequestKit.Core.Models;

namespace RequestKit.Core.Data;

public static class CommonPatternsLibrary
{
    public static readonly List<CommonPattern> Patterns =
    [
        new() { Name = "Email", Category = "Validation", Pattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", Description = "Matches standard email addresses", SampleMatch = "user@example.com" },
        new() { Name = "URL", Category = "Validation", Pattern = @"https?://[^\s/$.?#].[^\s]*", Description = "Matches HTTP and HTTPS URLs", SampleMatch = "https://example.com/path" },
        new() { Name = "IPv4 Address", Category = "Networking", Pattern = @"\b(?:(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\b", Description = "Matches valid IPv4 addresses (0.0.0.0 - 255.255.255.255)", SampleMatch = "192.168.1.1" },
        new() { Name = "IPv6 Address", Category = "Networking", Pattern = @"([0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}", Description = "Matches full IPv6 addresses", SampleMatch = "2001:0db8:85a3:0000:0000:8a2e:0370:7334" },
        new() { Name = "Phone (US)", Category = "Validation", Pattern = @"\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}", Description = "Matches US phone numbers in common formats", SampleMatch = "(555) 123-4567" },
        new() { Name = "Date (YYYY-MM-DD)", Category = "DateTime", Pattern = @"\d{4}-(?:0[1-9]|1[0-2])-(?:0[1-9]|[12]\d|3[01])", Description = "Matches ISO 8601 date format", SampleMatch = "2024-12-25" },
        new() { Name = "Time (HH:MM)", Category = "DateTime", Pattern = @"(?:[01]\d|2[0-3]):[0-5]\d", Description = "Matches 24-hour time format", SampleMatch = "14:30" },
        new() { Name = "UUID", Category = "Identifiers", Pattern = @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", Description = "Matches UUID/GUID format", SampleMatch = "550e8400-e29b-41d4-a716-446655440000" },
        new() { Name = "Hex Color", Category = "Web", Pattern = @"#(?:[0-9a-fA-F]{3}){1,2}\b", Description = "Matches 3 or 6 digit hex color codes", SampleMatch = "#FF5733" },
        new() { Name = "Credit Card", Category = "Validation", Pattern = @"\b(?:\d[ -]*?){13,19}\b", Description = "Matches credit card number patterns (13-19 digits)", SampleMatch = "4111-1111-1111-1111" },
        new() { Name = "ZIP Code (US)", Category = "Validation", Pattern = @"\b\d{5}(?:-\d{4})?\b", Description = "Matches US ZIP codes (5 or 9 digit)", SampleMatch = "90210" },
        new() { Name = "MAC Address", Category = "Networking", Pattern = @"(?:[0-9a-fA-F]{2}[:-]){5}[0-9a-fA-F]{2}", Description = "Matches MAC addresses with : or - separators", SampleMatch = "00:1A:2B:3C:4D:5E" },
        new() { Name = "Slug", Category = "Web", Pattern = @"[a-z0-9]+(?:-[a-z0-9]+)*", Description = "Matches URL-friendly slugs", SampleMatch = "my-blog-post-title" },
        new() { Name = "HTML Tag", Category = "Web", Pattern = @"<\/?[a-zA-Z][a-zA-Z0-9]*(?:\s[^>]*)?>", Description = "Matches opening and closing HTML tags", SampleMatch = "<div class=\"example\">" },
        new() { Name = "JSON String", Category = "Data", Pattern = @"""(?:[^""\\]|\\.)*""", Description = "Matches double-quoted JSON string values", SampleMatch = "\"hello world\"" },
    ];

    public static IEnumerable<string> Categories => Patterns.Select(p => p.Category).Distinct();

    public static IEnumerable<CommonPattern> GetByCategory(string category) =>
        Patterns.Where(p => p.Category == category);
}
