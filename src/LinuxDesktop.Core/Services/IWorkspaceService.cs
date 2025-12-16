using Olbrasoft.LinuxDesktop.Core.Models;

namespace Olbrasoft.LinuxDesktop.Core.Services;

/// <summary>
/// Service for interacting with desktop workspaces.
/// </summary>
public interface IWorkspaceService
{
    /// <summary>
    /// Gets all workspaces.
    /// </summary>
    Task<IReadOnlyList<WorkspaceInfo>> GetWorkspacesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of workspaces.
    /// </summary>
    Task<int> GetWorkspaceCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the index of the active workspace.
    /// </summary>
    Task<int> GetActiveWorkspaceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Switches to a specific workspace.
    /// </summary>
    Task SwitchWorkspaceAsync(int index, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets windows on a specific workspace.
    /// </summary>
    Task<IReadOnlyList<WindowInfo>> GetWorkspaceWindowsAsync(int index, CancellationToken cancellationToken = default);
}
