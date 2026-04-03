using System.Text.Json;
using Microsoft.JSInterop;
using RequestKit.Core.Models;

namespace RequestKit.Shared.Interop;

public class HttpFetchInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public HttpFetchInterop(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/RequestKit.Shared/js/fetchInterop.js").AsTask());
    }

    public async Task<FetchResult> SendAsync(string method, string url, Dictionary<string, string> headers, string? body, int timeoutMs = 30000)
    {
        var module = await _moduleTask.Value;
        var json = await module.InvokeAsync<JsonElement>("sendRequest", method, url, headers, body, timeoutMs);

        var success = json.GetProperty("success").GetBoolean();
        var responseHeaders = new List<KeyValueEntry>();
        if (json.TryGetProperty("headers", out var hdrs) && hdrs.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in hdrs.EnumerateObject())
            {
                responseHeaders.Add(new KeyValueEntry { Key = prop.Name, Value = prop.Value.GetString() ?? "" });
            }
        }

        return new FetchResult
        {
            Success = success,
            StatusCode = json.GetProperty("statusCode").GetInt32(),
            StatusText = json.GetProperty("statusText").GetString() ?? "",
            Headers = responseHeaders,
            Body = json.GetProperty("body").GetString() ?? "",
            ResponseTimeMs = json.GetProperty("responseTimeMs").GetDouble(),
            ResponseSizeBytes = json.GetProperty("responseSizeBytes").GetInt64(),
            ErrorMessage = json.TryGetProperty("errorMessage", out var err) && err.ValueKind != JsonValueKind.Null ? err.GetString() : null
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}

public record FetchResult
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string StatusText { get; init; } = "";
    public List<KeyValueEntry> Headers { get; init; } = [];
    public string Body { get; init; } = "";
    public double ResponseTimeMs { get; init; }
    public long ResponseSizeBytes { get; init; }
    public string? ErrorMessage { get; init; }
}
