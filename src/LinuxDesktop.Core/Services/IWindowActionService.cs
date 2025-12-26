namespace Olbrasoft.LinuxDesktop.Core.Services;

/// <summary>
/// Service for performing actions on windows (state changes).
/// Part of Interface Segregation Principle (ISP) refactoring.
/// </summary>
public interface IWindowActionService
{
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
}
