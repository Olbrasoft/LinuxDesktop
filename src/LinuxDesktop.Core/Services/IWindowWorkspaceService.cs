namespace Olbrasoft.LinuxDesktop.Core.Services;

/// <summary>
/// Service for window-workspace operations.
/// Part of Interface Segregation Principle (ISP) refactoring.
/// </summary>
public interface IWindowWorkspaceService
{
    /// <summary>
    /// Moves a window to a different workspace.
    /// </summary>
    Task MoveWindowToWorkspaceAsync(uint windowId, int workspaceIndex, CancellationToken cancellationToken = default);
}
