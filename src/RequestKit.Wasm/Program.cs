using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RequestKit.Core.Services;
using RequestKit.Core.State;
using RequestKit.Shared.Interop;
using RequestKit.Wasm;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// State
builder.Services.AddSingleton<AppState>();
builder.Services.AddSingleton<ApiClientState>();
builder.Services.AddSingleton<DiffState>();
builder.Services.AddSingleton<RegexState>();

// JS Interop
builder.Services.AddSingleton<MonacoEditorInterop>();
builder.Services.AddSingleton<KeyboardInterop>();
builder.Services.AddSingleton<FileInterop>();
builder.Services.AddSingleton<HttpFetchInterop>();
builder.Services.AddSingleton<RegexInterop>();
builder.Services.AddSingleton<IWorkspaceStorageService, IndexedDbWorkspaceStorage>();

await builder.Build().RunAsync();
