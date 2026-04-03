using RequestKit.Core.Models;

namespace RequestKit.Core.State;

public class ApiClientState
{
    private const int MaxHistoryEntries = 500;

    public event Action? OnChange;
    public event Action? OnDirty;
    public event Action? OnResponseReceived;

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

    public void SetActiveRequest(RequestDefinition request)
    {
        ActiveRequest = request;
        SelectedRequestId = request.Id;
        OnChange?.Invoke();
    }

    public void UpdateActiveRequest(RequestDefinition request)
    {
        ActiveRequest = request;
        if (Requests.ContainsKey(request.Id))
            Requests[request.Id] = request;
        OnDirty?.Invoke();
        OnChange?.Invoke();
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
        if (History.Count > MaxHistoryEntries)
            History.RemoveAt(History.Count - 1);
        OnDirty?.Invoke();
        OnChange?.Invoke();
    }

    public void AddRequest(RequestDefinition request, string? collectionId = null)
    {
        Requests[request.Id] = request;
        if (collectionId != null)
        {
            var col = Collections.FirstOrDefault(c => c.Id == collectionId);
            if (col != null)
            {
                var idx = Collections.FindIndex(c => c.Id == col.Id);
                if (idx >= 0)
                    Collections[idx] = col with { RootRequestIds = [.. col.RootRequestIds, request.Id] };
            }
        }
        OnDirty?.Invoke();
        OnChange?.Invoke();
    }

    public void DeleteRequest(string requestId)
    {
        Requests.Remove(requestId);
        for (int i = 0; i < Collections.Count; i++)
        {
            var col = Collections[i];
            if (col.RootRequestIds.Contains(requestId))
                Collections[i] = col with { RootRequestIds = col.RootRequestIds.Where(id => id != requestId).ToList() };
        }
        if (SelectedRequestId == requestId)
        {
            SelectedRequestId = null;
            ActiveRequest = new();
            ActiveResponse = null;
        }
        OnDirty?.Invoke();
        OnChange?.Invoke();
    }

    public void AddCollection(Collection collection)
    {
        Collections.Add(collection);
        OnDirty?.Invoke();
        OnChange?.Invoke();
    }

    public void SetEnvironments(List<Models.Environment> environments)
    {
        Environments = environments;
        OnDirty?.Invoke();
        OnChange?.Invoke();
    }

    public void SetActiveEnvironment(string? environmentId)
    {
        ActiveEnvironmentId = environmentId;
        OnDirty?.Invoke();
        OnChange?.Invoke();
    }

    public void SetCorsProxy(CorsProxyConfig config)
    {
        CorsProxy = config;
        OnDirty?.Invoke();
        OnChange?.Invoke();
    }

    public void LoadWorkspace(Workspace workspace)
    {
        Collections = workspace.Collections;
        Requests = workspace.Requests;
        Environments = workspace.Environments;
        ActiveEnvironmentId = workspace.ActiveEnvironmentId;
        History = workspace.History;
        CorsProxy = workspace.CorsProxy;
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
            ModifiedAt = DateTime.UtcNow
        };
    }
}
