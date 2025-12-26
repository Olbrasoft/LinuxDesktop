namespace Olbrasoft.LinuxDesktop.Core.Services;

/// <summary>
/// Service for window layout operations (position and size).
/// Part of Interface Segregation Principle (ISP) refactoring.
/// </summary>
public interface IWindowLayoutService
{
    /// <summary>
    /// Moves a window to a specific position.
    /// </summary>
    Task MoveWindowAsync(uint windowId, int x, int y, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resizes a window.
    /// </summary>
    Task ResizeWindowAsync(uint windowId, int width, int height, CancellationToken cancellationToken = default);
}
