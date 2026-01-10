namespace Olbrasoft.LinuxDesktop.Core.Services;

/// <summary>
/// Service for tracking pointer (mouse cursor) position and text caret position.
/// Also provides methods for displaying recording overlay at cursor/caret position.
/// </summary>
public interface IPointerService : IAsyncDisposable
{
    /// <summary>
    /// Gets the current mouse pointer position on screen.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>Tuple of (X, Y) screen coordinates, or null if unavailable.</returns>
    Task<(int X, int Y)?> GetPointerPositionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the geometry of the currently focused window.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>Tuple of (X, Y, Width, Height) window bounds, or null if no window is focused.</returns>
    Task<(int X, int Y, int Width, int Height)?> GetActiveWindowGeometryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows a recording overlay at the text caret position (or mouse position as fallback).
    /// Used for visual indication during dictation/voice input.
    /// </summary>
    /// <param name="text">Text to display in the overlay (e.g., "Recording...").</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    Task ShowRecordingOverlayAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hides the recording overlay.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    Task HideRecordingOverlayAsync(CancellationToken cancellationToken = default);
}
