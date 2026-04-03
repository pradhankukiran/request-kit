using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using RequestKit.Core.Models;
using RequestKit.Core.Services;

namespace RequestKit.Shared.Interop;

public class IndexedDbWorkspaceStorage : IWorkspaceStorageService, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public IndexedDbWorkspaceStorage(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/RequestKit.Shared/js/storageInterop.js").AsTask());
    }

    public async Task<IReadOnlyList<WorkspaceSummary>> ListWorkspacesAsync()
    {
        var module = await _moduleTask.Value;
        var json = await module.InvokeAsync<JsonElement>("listWorkspaces");
        var summaries = new List<WorkspaceSummary>();
        foreach (var item in json.EnumerateArray())
        {
            summaries.Add(new WorkspaceSummary
            {
                Id = item.GetProperty("id").GetString() ?? "",
                Name = item.GetProperty("name").GetString() ?? "",
                RequestCount = item.GetProperty("requestCount").GetInt32(),
                CollectionCount = item.GetProperty("collectionCount").GetInt32(),
                ModifiedAt = item.TryGetProperty("modifiedAt", out var dt) && dt.ValueKind == JsonValueKind.String
                    ? DateTime.Parse(dt.GetString()!, System.Globalization.CultureInfo.InvariantCulture)
                    : DateTime.UtcNow
            });
        }
        return summaries;
    }

    public async Task<Workspace> LoadWorkspaceAsync(string workspaceId)
    {
        var module = await _moduleTask.Value;
        var json = await module.InvokeAsync<JsonElement>("loadWorkspace", workspaceId);
        if (json.ValueKind == JsonValueKind.Null)
            throw new InvalidOperationException($"Workspace {workspaceId} not found");
        return JsonSerializer.Deserialize<Workspace>(json.GetRawText(), JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize workspace");
    }

    public async Task SaveWorkspaceAsync(Workspace workspace)
    {
        var module = await _moduleTask.Value;
        var json = JsonSerializer.SerializeToElement(workspace, JsonOptions);
        await module.InvokeAsync<bool>("saveWorkspace", json);
    }

    public async Task DeleteWorkspaceAsync(string workspaceId)
    {
        var module = await _moduleTask.Value;
        await module.InvokeAsync<bool>("deleteWorkspace", workspaceId);
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
