using Microsoft.Extensions.Logging;
using RequestKit.Core.Services;
using RequestKit.Core.State;
using RequestKit.Shared.Interop;

namespace RequestKit.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // State
        builder.Services.AddSingleton<AppState>();
        builder.Services.AddSingleton<ApiClientState>();
        builder.Services.AddSingleton<DiffState>();
        builder.Services.AddSingleton<RegexState>();

        // JS Interop (via BlazorWebView)
        builder.Services.AddSingleton<MonacoEditorInterop>();
        builder.Services.AddSingleton<KeyboardInterop>();
        builder.Services.AddSingleton<FileInterop>();
        builder.Services.AddSingleton<HttpFetchInterop>();
        builder.Services.AddSingleton<RegexInterop>();
        builder.Services.AddSingleton<IWorkspaceStorageService, IndexedDbWorkspaceStorage>();

        return builder.Build();
    }
}
