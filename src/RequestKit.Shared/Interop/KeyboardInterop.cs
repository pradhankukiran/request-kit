using Microsoft.JSInterop;

namespace RequestKit.Shared.Interop;

public class KeyboardInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
    private DotNetObjectReference<KeyboardInterop>? _dotNetRef;

    public event Action<string>? OnShortcut;

    public KeyboardInterop(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/RequestKit.Shared/js/keyboardInterop.js").AsTask());
    }

    public async Task InitializeAsync()
    {
        var module = await _moduleTask.Value;
        _dotNetRef?.Dispose();
        _dotNetRef = DotNetObjectReference.Create(this);
        await module.InvokeVoidAsync("initialize", _dotNetRef);
    }

    [JSInvokable]
    public void OnShortcut_Internal(string shortcut) => OnShortcut?.Invoke(shortcut);

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("dispose");
            await module.DisposeAsync();
        }
        _dotNetRef?.Dispose();
    }
}
