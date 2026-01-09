namespace Olbrasoft.LinuxDesktop.Core.Services;

/// <summary>
/// Service for querying cursor and window positions via GNOME Shell extension.
/// Uses focus-tracker@olbrasoft.cz D-Bus service.
/// </summary>
public interface IPointerService : IAsyncDisposable
{
    /// <summary>
    /// Gets the current mouse pointer position.
    /// </summary>
    /// <returns>Tuple of (X, Y) screen coordinates, or null if unavailable.</returns>
    Task<(int X, int Y)?> GetPointerPositionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the geometry of the currently focused window.
    /// </summary>
    /// <returns>Tuple of (X, Y, Width, Height), or null if no window is focused or service unavailable.</returns>
    Task<(int X, int Y, int Width, int Height)?> GetActiveWindowGeometryAsync(CancellationToken cancellationToken = default);
}
