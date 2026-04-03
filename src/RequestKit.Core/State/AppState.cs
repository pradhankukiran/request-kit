using RequestKit.Core.Models;

namespace RequestKit.Core.State;

public class AppState
{
    public event Action? OnModeChanged;

    public ToolMode ActiveMode { get; private set; } = ToolMode.ApiClient;
    public string? LastResponseBody { get; private set; }
    public ContentType? LastResponseContentType { get; private set; }

    public void SetMode(ToolMode mode)
    {
        if (ActiveMode == mode) return;
        ActiveMode = mode;
        OnModeChanged?.Invoke();
    }

    public void SetLastResponse(string body, ContentType contentType)
    {
        LastResponseBody = body;
        LastResponseContentType = contentType;
    }
}
