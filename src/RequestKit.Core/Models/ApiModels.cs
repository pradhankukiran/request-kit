namespace RequestKit.Core.Models;

public record KeyValueEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Key { get; init; } = "";
    public string Value { get; init; } = "";
    public bool Enabled { get; init; } = true;
}

public record AuthConfig
{
    public AuthType Type { get; init; } = AuthType.None;
    public string Token { get; init; } = "";
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public string ApiKeyName { get; init; } = "";
    public string ApiKeyValue { get; init; } = "";
    public string CustomHeaderName { get; init; } = "";
    public string CustomHeaderValue { get; init; } = "";
}

public record RequestBody
{
    public BodyType Type { get; init; } = BodyType.None;
    public string RawContent { get; init; } = "";
    public List<KeyValueEntry> FormData { get; init; } = [];
}

public record RequestDefinition
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = "New Request";
    public HttpMethod Method { get; init; } = HttpMethod.GET;
    public string Url { get; init; } = "";
    public List<KeyValueEntry> QueryParams { get; init; } = [];
    public List<KeyValueEntry> Headers { get; init; } = [];
    public AuthConfig Auth { get; init; } = new();
    public RequestBody Body { get; init; } = new();
    public int SortOrder { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; init; } = DateTime.UtcNow;
}

public record ResponseData
{
    public int StatusCode { get; init; }
    public string StatusText { get; init; } = "";
    public double ResponseTimeMs { get; init; }
    public long ResponseSizeBytes { get; init; }
    public List<KeyValueEntry> Headers { get; init; } = [];
    public string Body { get; init; } = "";
    public ContentType DetectedContentType { get; init; } = ContentType.PlainText;
    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
}

public record HistoryEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string RequestName { get; init; } = "";
    public HttpMethod Method { get; init; }
    public string Url { get; init; } = "";
    public int StatusCode { get; init; }
    public double ResponseTimeMs { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string RequestSnapshot { get; init; } = "";
    public string ResponseSnapshot { get; init; } = "";
}
