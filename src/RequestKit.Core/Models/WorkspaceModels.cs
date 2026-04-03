namespace RequestKit.Core.Models;

public record RequestFolder
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = "New Folder";
    public List<string> RequestIds { get; init; } = [];
    public List<RequestFolder> SubFolders { get; init; } = [];
    public bool IsExpanded { get; init; } = true;
    public int SortOrder { get; init; }
}

public record Collection
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = "New Collection";
    public List<RequestFolder> Folders { get; init; } = [];
    public List<string> RootRequestIds { get; init; } = [];
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; init; } = DateTime.UtcNow;
}

public record EnvironmentVariable
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Key { get; init; } = "";
    public string Value { get; init; } = "";
    public bool IsSecret { get; init; }
    public bool Enabled { get; init; } = true;
}

public record Environment
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = "Default";
    public List<EnvironmentVariable> Variables { get; init; } = [];
    public int SortOrder { get; init; }
}

public record CorsProxyConfig
{
    public bool Enabled { get; init; }
    public string ProxyUrl { get; init; } = "https://corsproxy.io/?";
}

public record Workspace
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = "My Workspace";
    public List<Collection> Collections { get; init; } = [];
    public Dictionary<string, RequestDefinition> Requests { get; init; } = new();
    public List<Environment> Environments { get; init; } = [];
    public string? ActiveEnvironmentId { get; init; }
    public List<HistoryEntry> History { get; init; } = [];
    public CorsProxyConfig CorsProxy { get; init; } = new();
    public WorkspaceSettings Settings { get; init; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; init; } = DateTime.UtcNow;
    public int Version { get; init; } = 1;
}

public record WorkspaceSummary
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public int RequestCount { get; init; }
    public int CollectionCount { get; init; }
    public DateTime ModifiedAt { get; init; }
}

public record WorkspaceSettings
{
    public bool AutoSaveEnabled { get; init; } = true;
    public int AutoSaveIntervalSeconds { get; init; } = 30;
    public int MaxHistoryEntries { get; init; } = 500;
    public bool FollowRedirects { get; init; } = true;
    public int TimeoutSeconds { get; init; } = 30;
}
