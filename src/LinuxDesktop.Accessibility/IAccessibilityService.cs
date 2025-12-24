namespace Olbrasoft.LinuxDesktop.Accessibility;

/// <summary>
/// Service for monitoring accessibility events and querying accessible widgets.
/// Uses AT-SPI (Assistive Technology Service Provider Interface) on Linux.
/// </summary>
public interface IAccessibilityService : IAsyncDisposable
{
    /// <summary>
    /// Gets the currently focused accessible widget.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The focused widget, or null if no widget has focus</returns>
    Task<AccessibleWidget?> GetFocusedWidgetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Watches for focus change events.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async stream of focus change events</returns>
    IAsyncEnumerable<FocusChangedEvent> WatchFocusChangesAsync(CancellationToken cancellationToken = default);
}
