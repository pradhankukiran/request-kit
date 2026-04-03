using Microsoft.JSInterop;

namespace RequestKit.Shared.Interop;

public class MonacoEditorInterop : IAsyncDisposable
{
    private static readonly HashSet<string> AllowedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "json", "xml", "html", "css", "javascript", "typescript",
        "plaintext", "markdown", "yaml", "csharp", "python",
        "go", "rust", "java", "sql", "shell", "powershell",
        "ruby", "php", "c", "cpp", "text"
    };

    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
    private DotNetObjectReference<MonacoEditorInterop>? _dotNetRef;

    public event Func<string, string, Task>? OnExecuteRequested;
    public event Func<string, string, Task>? OnContentChanged;

    public MonacoEditorInterop(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/RequestKit.Shared/js/monacoInterop.js").AsTask());
    }

    public async Task InitializeAsync()
    {
        var module = await _moduleTask.Value;
        _dotNetRef?.Dispose();
        _dotNetRef = DotNetObjectReference.Create(this);
        await module.InvokeAsync<bool>("initialize", _dotNetRef);
    }

    public async Task CreateEditorAsync(string elementId, string initialValue = "", string language = "plaintext", bool readOnly = false)
    {
        var safeLanguage = AllowedLanguages.Contains(language) ? language : "plaintext";
        var module = await _moduleTask.Value;
        await module.InvokeAsync<bool>("createEditor", elementId, initialValue, safeLanguage, readOnly);
    }

    public async Task<string> GetValueAsync(string elementId)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<string>("getValue", elementId);
    }

    public async Task SetValueAsync(string elementId, string value)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("setValue", elementId, value);
    }

    public async Task SetLanguageAsync(string elementId, string language)
    {
        var safeLanguage = AllowedLanguages.Contains(language) ? language : "plaintext";
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("setLanguage", elementId, safeLanguage);
    }

    public async Task SetReadOnlyAsync(string elementId, bool readOnly)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("setReadOnly", elementId, readOnly);
    }

    public async Task SetDecorationsAsync(string elementId, List<DecorationRange> ranges)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("setDecorations", elementId, ranges);
    }

    public async Task<bool> HasEditorAsync(string elementId)
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<bool>("hasEditor", elementId);
    }

    public async Task FocusAsync(string elementId)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("focus", elementId);
    }

    public async Task DisposeEditorAsync(string elementId)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("dispose", elementId);
    }

    [JSInvokable]
    public async Task OnExecuteRequested_Internal(string editorId, string value)
    {
        if (OnExecuteRequested != null) await OnExecuteRequested.Invoke(editorId, value);
    }

    [JSInvokable]
    public async Task OnContentChanged_Internal(string editorId, string value)
    {
        if (OnContentChanged != null) await OnContentChanged.Invoke(editorId, value);
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("disposeAll");
            await module.DisposeAsync();
        }
        _dotNetRef?.Dispose();
    }
}

public record DecorationRange
{
    public int StartLine { get; init; }
    public int StartCol { get; init; }
    public int EndLine { get; init; }
    public int EndCol { get; init; }
    public string ClassName { get; init; } = "rk-match-highlight";
}
