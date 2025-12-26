using Olbrasoft.LinuxDesktop.Core.Models;

namespace Olbrasoft.LinuxDesktop.Core.Services;

/// <summary>
/// Service for querying window information (read-only operations).
/// Part of Interface Segregation Principle (ISP) refactoring.
/// </summary>
public interface IWindowQueryService
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
}
