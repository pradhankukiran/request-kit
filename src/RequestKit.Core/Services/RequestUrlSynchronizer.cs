using RequestKit.Core.Models;

namespace RequestKit.Core.Services;

public static class RequestUrlSynchronizer
{
    public static RequestDefinition SyncParamsFromUrl(RequestDefinition request)
    {
        return request with
        {
            QueryParams = ParseQueryParams(request.Url, request.QueryParams)
        };
    }

    public static RequestDefinition SyncUrlFromParams(RequestDefinition request)
    {
        return request with
        {
            Url = ApplyQueryParams(request.Url, request.QueryParams)
        };
    }

    public static List<KeyValueEntry> ParseQueryParams(string url, IReadOnlyList<KeyValueEntry>? existingEntries = null)
    {
        var query = SplitUrl(url).Query;
        if (string.IsNullOrEmpty(query))
        {
            return [];
        }

        var existing = existingEntries ?? [];
        var entries = new List<KeyValueEntry>();
        var segments = query.Split('&', StringSplitOptions.RemoveEmptyEntries);

        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];
            var separatorIndex = segment.IndexOf('=');
            var key = separatorIndex >= 0 ? segment[..separatorIndex] : segment;
            var value = separatorIndex >= 0 ? segment[(separatorIndex + 1)..] : string.Empty;
            var existingEntry = index < existing.Count ? existing[index] : null;

            entries.Add(new KeyValueEntry
            {
                Id = existingEntry?.Id ?? Guid.NewGuid().ToString(),
                Key = DecodeQueryValue(key),
                Value = DecodeQueryValue(value),
                Enabled = true
            });
        }

        return entries;
    }

    public static string ApplyQueryParams(string url, IEnumerable<KeyValueEntry> entries)
    {
        var parts = SplitUrl(url);
        var activeEntries = entries
            .Where(entry => entry.Enabled && !string.IsNullOrWhiteSpace(entry.Key))
            .Select(entry => $"{Uri.EscapeDataString(entry.Key)}={Uri.EscapeDataString(entry.Value ?? string.Empty)}")
            .ToList();

        var query = activeEntries.Count > 0 ? $"?{string.Join("&", activeEntries)}" : string.Empty;
        return $"{parts.Base}{query}{parts.Fragment}";
    }

    public static string AppendQueryParameter(string url, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return url;
        }

        var parts = SplitUrl(url);
        var query = string.IsNullOrWhiteSpace(parts.Query)
            ? $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value ?? string.Empty)}"
            : $"{parts.Query}&{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value ?? string.Empty)}";

        return $"{parts.Base}?{query}{parts.Fragment}";
    }

    private static (string Base, string Query, string Fragment) SplitUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return (string.Empty, string.Empty, string.Empty);
        }

        var fragmentIndex = url.IndexOf('#');
        var fragment = fragmentIndex >= 0 ? url[fragmentIndex..] : string.Empty;
        var withoutFragment = fragmentIndex >= 0 ? url[..fragmentIndex] : url;

        var queryIndex = withoutFragment.IndexOf('?');
        if (queryIndex < 0)
        {
            return (withoutFragment, string.Empty, fragment);
        }

        return (
            withoutFragment[..queryIndex],
            withoutFragment[(queryIndex + 1)..],
            fragment);
    }

    private static string DecodeQueryValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return Uri.UnescapeDataString(value.Replace("+", " ", StringComparison.Ordinal));
    }
}
