using System.Text.Json;
using Microsoft.JSInterop;
using RequestKit.Core.Models;

namespace RequestKit.Shared.Interop;

public class RegexInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public RegexInterop(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/RequestKit.Shared/js/regexInterop.js").AsTask());
    }

    public async Task<(List<RegexMatch> Matches, string? Error)> ExecuteAsync(string pattern, string testString, string flags)
    {
        var module = await _moduleTask.Value;
        var json = await module.InvokeAsync<JsonElement>("execute", pattern, testString, flags);

        var success = json.GetProperty("success").GetBoolean();
        string? error = null;
        if (!success)
            error = json.GetProperty("errorMessage").GetString();

        var matches = new List<RegexMatch>();
        foreach (var m in json.GetProperty("matches").EnumerateArray())
        {
            var groups = new List<RegexGroup>();
            foreach (var g in m.GetProperty("groups").EnumerateArray())
            {
                groups.Add(new RegexGroup
                {
                    GroupIndex = g.GetProperty("groupIndex").GetInt32(),
                    Name = g.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
                        ? nameEl.GetString()
                        : null,
                    Value = g.GetProperty("value").GetString() ?? "",
                    Index = g.GetProperty("index").GetInt32(),
                    Length = g.GetProperty("length").GetInt32()
                });
            }
            matches.Add(new RegexMatch
            {
                Index = m.GetProperty("index").GetInt32(),
                Length = m.GetProperty("length").GetInt32(),
                Value = m.GetProperty("value").GetString() ?? "",
                Groups = groups
            });
        }

        return (matches, error);
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
