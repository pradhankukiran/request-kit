using Microsoft.JSInterop;

namespace RequestKit.Shared.Interop;

public class FileInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public FileInterop(IJSRuntime jsRuntime)
    {
        _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/RequestKit.Shared/js/fileInterop.js").AsTask());
    }

    public async Task DownloadTextAsync(string filename, string content, string mimeType = "text/plain")
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("downloadText", filename, content, mimeType);
    }

    public async Task CopyToClipboardAsync(string text)
    {
        var module = await _moduleTask.Value;
        await module.InvokeAsync<bool>("copyToClipboard", text);
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
