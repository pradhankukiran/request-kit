using RequestKit.Core.Models;

namespace RequestKit.Core.Services;

public interface IWorkspaceStorageService
{
    Task<IReadOnlyList<WorkspaceSummary>> ListWorkspacesAsync();
    Task<Workspace> LoadWorkspaceAsync(string workspaceId);
    Task SaveWorkspaceAsync(Workspace workspace);
    Task DeleteWorkspaceAsync(string workspaceId);
}
