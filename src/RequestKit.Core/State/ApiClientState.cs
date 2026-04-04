using RequestKit.Core.Models;
using RequestKit.Core.Services;

namespace RequestKit.Core.State;

public class ApiClientState
{
    private const int MaxHistoryEntries = 500;

    public event Action? OnChange;
    public event Action? OnDirty;
    public event Action? OnResponseReceived;

    public string CurrentWorkspaceId { get; private set; } = Guid.NewGuid().ToString();
    public string CurrentWorkspaceName { get; private set; } = "My Workspace";
    public WorkspaceSettings Settings { get; private set; } = new();
    public bool IsWorkspaceDirty { get; private set; }
    public bool IsWorkspaceSaving { get; private set; }
    public DateTime? LastSavedAt { get; private set; }
    public SemaphoreSlim WorkspacePersistenceLock { get; } = new(1, 1);

    public RequestDefinition ActiveRequest { get; private set; } = new();
    public ResponseData? ActiveResponse { get; private set; }
    public bool IsLoading { get; private set; }
    public List<Collection> Collections { get; private set; } = [];
    public Dictionary<string, RequestDefinition> Requests { get; private set; } = new();
    public List<Models.Environment> Environments { get; private set; } = [];
    public string? ActiveEnvironmentId { get; private set; }
    public List<HistoryEntry> History { get; private set; } = [];
    public CorsProxyConfig CorsProxy { get; private set; } = new();
    public string? SelectedRequestId { get; private set; }

    public Models.Environment? ActiveEnvironment =>
        ActiveEnvironmentId != null
            ? Environments.FirstOrDefault(e => e.Id == ActiveEnvironmentId)
            : Environments.FirstOrDefault();

    public void SetWorkspaceName(string name)
    {
        var normalized = string.IsNullOrWhiteSpace(name) ? "My Workspace" : name.Trim();
        if (CurrentWorkspaceName == normalized) return;

        CurrentWorkspaceName = normalized;
        MarkDirty();
    }

    public void SetWorkspaceSettings(WorkspaceSettings settings)
    {
        Settings = settings;
        MarkDirty();
    }

    public void UpdateWorkspaceSettings(Func<WorkspaceSettings, WorkspaceSettings> update)
    {
        Settings = update(Settings);
        MarkDirty();
    }

    public void SetWorkspaceSaving(bool isSaving)
    {
        if (IsWorkspaceSaving == isSaving) return;

        IsWorkspaceSaving = isSaving;
        OnChange?.Invoke();
    }

    public void MarkWorkspaceSaved(DateTime? savedAt = null)
    {
        IsWorkspaceDirty = false;
        IsWorkspaceSaving = false;
        LastSavedAt = savedAt ?? DateTime.UtcNow;
        OnChange?.Invoke();
    }

    public void SetActiveRequest(RequestDefinition request)
    {
        ActiveRequest = request;
        SelectedRequestId = request.Id;
        OnChange?.Invoke();
    }

    public void UpdateActiveRequest(RequestDefinition request)
    {
        ActiveRequest = request with { ModifiedAt = DateTime.UtcNow };
        if (Requests.ContainsKey(request.Id))
            Requests[request.Id] = ActiveRequest;
        MarkDirty();
    }

    public void SetLoading(bool loading)
    {
        IsLoading = loading;
        OnChange?.Invoke();
    }

    public void SetResponse(ResponseData response)
    {
        ActiveResponse = response;
        OnResponseReceived?.Invoke();
        OnChange?.Invoke();
    }

    public void AddHistoryEntry(HistoryEntry entry)
    {
        History.Insert(0, entry);
        if (History.Count > (Settings.MaxHistoryEntries > 0 ? Settings.MaxHistoryEntries : MaxHistoryEntries))
            History.RemoveAt(History.Count - 1);
        MarkDirty();
    }

    public void AddRequest(RequestDefinition request, string? collectionId = null, string? folderId = null)
    {
        Requests[request.Id] = request with
        {
            CreatedAt = request.CreatedAt == default ? DateTime.UtcNow : request.CreatedAt,
            ModifiedAt = DateTime.UtcNow
        };

        if (collectionId != null)
        {
            PlaceRequest(collectionId, folderId, request.Id);
        }
        MarkDirty();
    }

    public void DeleteRequest(string requestId)
    {
        Requests.Remove(requestId);
        RemoveRequestFromCollections(requestId);
        if (SelectedRequestId == requestId)
        {
            SelectedRequestId = null;
            ActiveRequest = new();
            ActiveResponse = null;
        }
        MarkDirty();
    }

    public void AddCollection(Collection collection)
    {
        Collections.Add(collection);
        MarkDirty();
    }

    public void SetEnvironments(List<Models.Environment> environments)
    {
        Environments = environments;
        MarkDirty();
    }

    public void SetActiveEnvironment(string? environmentId)
    {
        ActiveEnvironmentId = environmentId;
        MarkDirty();
    }

    public void SetCorsProxy(CorsProxyConfig config)
    {
        CorsProxy = config;
        MarkDirty();
    }

    public void UpdateCorsProxy(Func<CorsProxyConfig, CorsProxyConfig> update)
    {
        CorsProxy = update(CorsProxy);
        MarkDirty();
    }

    public Collection? GetCollection(string collectionId) =>
        Collections.FirstOrDefault(c => c.Id == collectionId);

    public void UpdateCollection(string collectionId, Func<Collection, Collection> update)
    {
        var index = Collections.FindIndex(c => c.Id == collectionId);
        if (index < 0) return;

        Collections[index] = update(Collections[index]);
        MarkDirty();
    }

    public void SetCollectionName(string collectionId, string name) =>
        UpdateCollection(collectionId, collection => collection with { Name = name });

    public void SetCollectionFolders(string collectionId, List<RequestFolder> folders) =>
        UpdateCollection(collectionId, collection => collection with { Folders = folders });

    public void SetCollectionRootRequestIds(string collectionId, List<string> requestIds) =>
        UpdateCollection(collectionId, collection => collection with { RootRequestIds = requestIds });

    public (string CollectionId, string? FolderId)? GetRequestLocation(string requestId)
    {
        foreach (var collection in Collections)
        {
            if (collection.RootRequestIds.Contains(requestId))
            {
                return (collection.Id, null);
            }

            var folderId = FindFolderContainingRequest(collection.Folders, requestId);
            if (folderId is not null)
            {
                return (collection.Id, folderId);
            }
        }

        return null;
    }

    public RequestDefinition? DuplicateRequest(string requestId)
    {
        if (!Requests.TryGetValue(requestId, out var existingRequest))
        {
            return null;
        }

        var location = GetRequestLocation(requestId);
        var duplicate = RequestUrlSynchronizer.SyncParamsFromUrl(existingRequest with
        {
            Id = Guid.NewGuid().ToString(),
            Name = BuildDuplicateName(existingRequest.Name),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        });

        AddRequest(duplicate, location?.CollectionId, location?.FolderId);
        SetActiveRequest(duplicate);
        return duplicate;
    }

    public bool MoveRequest(string requestId, string targetCollectionId, string? targetFolderId = null)
    {
        if (!Requests.ContainsKey(requestId))
        {
            return false;
        }

        var collection = GetCollection(targetCollectionId);
        if (collection is null)
        {
            return false;
        }

        RemoveRequestFromCollections(requestId);
        PlaceRequest(targetCollectionId, targetFolderId, requestId);
        MarkDirty();
        return true;
    }

    public void DeleteCollection(string collectionId)
    {
        var index = Collections.FindIndex(c => c.Id == collectionId);
        if (index < 0) return;

        Collections.RemoveAt(index);
        MarkDirty();
    }

    public Models.Environment? GetEnvironment(string environmentId) =>
        Environments.FirstOrDefault(e => e.Id == environmentId);

    public void AddEnvironment(Models.Environment environment)
    {
        Environments.Add(environment);
        MarkDirty();
    }

    public void UpdateEnvironment(string environmentId, Func<Models.Environment, Models.Environment> update)
    {
        var index = Environments.FindIndex(e => e.Id == environmentId);
        if (index < 0) return;

        Environments[index] = update(Environments[index]);
        MarkDirty();
    }

    public void DeleteEnvironment(string environmentId)
    {
        var index = Environments.FindIndex(e => e.Id == environmentId);
        if (index < 0) return;

        Environments.RemoveAt(index);
        if (ActiveEnvironmentId == environmentId)
            ActiveEnvironmentId = Environments.FirstOrDefault()?.Id;
        MarkDirty();
    }

    public void SetEnvironmentName(string environmentId, string name) =>
        UpdateEnvironment(environmentId, environment => environment with { Name = name });

    public void SetEnvironmentVariables(string environmentId, List<EnvironmentVariable> variables) =>
        UpdateEnvironment(environmentId, environment => environment with { Variables = variables });

    public void AddEnvironmentVariable(string environmentId, EnvironmentVariable variable)
    {
        UpdateEnvironment(environmentId, environment =>
            environment with { Variables = [.. environment.Variables, variable] });
    }

    public void UpdateEnvironmentVariable(string environmentId, string variableId, Func<EnvironmentVariable, EnvironmentVariable> update)
    {
        UpdateEnvironment(environmentId, environment =>
        {
            var variables = environment.Variables.ToList();
            var index = variables.FindIndex(v => v.Id == variableId);
            if (index < 0) return environment;

            variables[index] = update(variables[index]);
            return environment with { Variables = variables };
        });
    }

    public void DeleteEnvironmentVariable(string environmentId, string variableId)
    {
        UpdateEnvironment(environmentId, environment =>
            environment with { Variables = environment.Variables.Where(v => v.Id != variableId).ToList() });
    }

    public RequestDefinition RestoreHistoryEntryAsCopy(HistoryEntry entry, string? collectionId = null, string? folderId = null)
    {
        var request = RequestSnapshotSerializer.DeserializeRequest(entry.RequestSnapshot)
            ?? new RequestDefinition
            {
                Name = !string.IsNullOrEmpty(entry.RequestName) ? entry.RequestName : "From History",
                Method = entry.Method,
                Url = entry.Url
            };

        var restored = RequestUrlSynchronizer.SyncParamsFromUrl(request with
        {
            Id = Guid.NewGuid().ToString(),
            Name = BuildDuplicateName(string.IsNullOrWhiteSpace(request.Name) ? "History Request" : request.Name),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        });

        AddRequest(restored, collectionId, folderId);
        SetActiveRequest(restored);
        return restored;
    }

    public void LoadWorkspace(Workspace workspace)
    {
        CurrentWorkspaceId = workspace.Id;
        CurrentWorkspaceName = string.IsNullOrWhiteSpace(workspace.Name) ? "My Workspace" : workspace.Name;
        Settings = workspace.Settings;
        Collections = workspace.Collections;
        Requests = workspace.Requests;
        Environments = workspace.Environments;
        ActiveEnvironmentId = workspace.ActiveEnvironmentId;
        History = workspace.History;
        CorsProxy = workspace.CorsProxy;
        IsWorkspaceDirty = false;
        IsWorkspaceSaving = false;
        LastSavedAt = workspace.ModifiedAt;
        ActiveRequest = new();
        ActiveResponse = null;
        SelectedRequestId = null;
        OnChange?.Invoke();
    }

    public Workspace BuildWorkspace(string id, string name)
    {
        return new Workspace
        {
            Id = id,
            Name = name,
            Collections = Collections,
            Requests = Requests,
            Environments = Environments,
            ActiveEnvironmentId = ActiveEnvironmentId,
            History = History,
            CorsProxy = CorsProxy,
            Settings = Settings,
            ModifiedAt = DateTime.UtcNow
        };
    }

    private void MarkDirty()
    {
        IsWorkspaceDirty = true;
        OnDirty?.Invoke();
        OnChange?.Invoke();
    }

    private void PlaceRequest(string collectionId, string? folderId, string requestId)
    {
        var index = Collections.FindIndex(c => c.Id == collectionId);
        if (index < 0)
        {
            return;
        }

        var collection = Collections[index];
        if (string.IsNullOrWhiteSpace(folderId))
        {
            if (!collection.RootRequestIds.Contains(requestId))
            {
                collection = collection with { RootRequestIds = [.. collection.RootRequestIds, requestId] };
                Collections[index] = collection;
            }

            return;
        }

        Collections[index] = collection with
        {
            Folders = AddRequestToFolder(collection.Folders, folderId, requestId)
        };
    }

    private void RemoveRequestFromCollections(string requestId)
    {
        for (var index = 0; index < Collections.Count; index++)
        {
            var collection = Collections[index];
            Collections[index] = collection with
            {
                RootRequestIds = collection.RootRequestIds.Where(id => id != requestId).ToList(),
                Folders = RemoveRequestFromFolders(collection.Folders, requestId)
            };
        }
    }

    private static List<RequestFolder> RemoveRequestFromFolders(IEnumerable<RequestFolder> folders, string requestId)
    {
        return folders
            .Select(folder => folder with
            {
                RequestIds = folder.RequestIds.Where(id => id != requestId).ToList(),
                SubFolders = RemoveRequestFromFolders(folder.SubFolders, requestId)
            })
            .ToList();
    }

    private static List<RequestFolder> AddRequestToFolder(IEnumerable<RequestFolder> folders, string folderId, string requestId)
    {
        return folders
            .Select(folder =>
            {
                if (folder.Id == folderId)
                {
                    return folder.RequestIds.Contains(requestId)
                        ? folder
                        : folder with { RequestIds = [.. folder.RequestIds, requestId] };
                }

                if (folder.SubFolders.Count == 0)
                {
                    return folder;
                }

                return folder with { SubFolders = AddRequestToFolder(folder.SubFolders, folderId, requestId) };
            })
            .ToList();
    }

    private static string? FindFolderContainingRequest(IEnumerable<RequestFolder> folders, string requestId)
    {
        foreach (var folder in folders)
        {
            if (folder.RequestIds.Contains(requestId))
            {
                return folder.Id;
            }

            var nestedFolderId = FindFolderContainingRequest(folder.SubFolders, requestId);
            if (nestedFolderId is not null)
            {
                return nestedFolderId;
            }
        }

        return null;
    }

    private static string BuildDuplicateName(string name)
    {
        var normalized = string.IsNullOrWhiteSpace(name) ? "Request" : name.Trim();
        return normalized.EndsWith("Copy", StringComparison.OrdinalIgnoreCase)
            ? $"{normalized} 2"
            : $"{normalized} Copy";
    }
}
