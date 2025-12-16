using Olbrasoft.LinuxDesktop.Core.Models;

namespace Olbrasoft.LinuxDesktop.Core.Services;

/// <summary>
/// Service for interacting with desktop windows.
/// </summary>
public interface IWindowService
{
    /// <summary>
    /// Gets all windows on the desktop.
    /// </summary>
    Task<IReadOnlyList<WindowInfo>> GetWindowsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific window.
    /// </summary>
    Task<WindowDetails?> GetWindowDetailsAsync(uint windowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently focused window.
    /// </summary>
    Task<WindowInfo?> GetFocusedWindowAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the title of a specific window.
    /// </summary>
    Task<string?> GetWindowTitleAsync(uint windowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates (focuses) a window.
    /// </summary>
    Task ActivateWindowAsync(uint windowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a window.
    /// </summary>
    Task CloseWindowAsync(uint windowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Maximizes a window.
    /// </summary>
    Task MaximizeWindowAsync(uint windowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Minimizes a window.
    /// </summary>
    Task MinimizeWindowAsync(uint windowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a window from maximized state.
    /// </summary>
    Task UnmaximizeWindowAsync(uint windowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a window from minimized state.
    /// </summary>
    Task UnminimizeWindowAsync(uint windowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a window to a specific position.
    /// </summary>
    Task MoveWindowAsync(uint windowId, int x, int y, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resizes a window.
    /// </summary>
    Task ResizeWindowAsync(uint windowId, int width, int height, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a window to a different workspace.
    /// </summary>
    Task MoveWindowToWorkspaceAsync(uint windowId, int workspaceIndex, CancellationToken cancellationToken = default);
}
