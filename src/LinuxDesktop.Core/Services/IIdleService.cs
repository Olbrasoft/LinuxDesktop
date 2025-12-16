namespace Olbrasoft.LinuxDesktop.Core.Services;

/// <summary>
/// Service for monitoring user activity/idle state.
/// </summary>
public interface IIdleService
{
    /// <summary>
    /// Gets the time since the user was last active.
    /// </summary>
    /// <returns>Idle time in milliseconds.</returns>
    Task<ulong> GetIdleTimeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the time since the user was last active as a TimeSpan.
    /// </summary>
    Task<TimeSpan> GetIdleTimeSpanAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the user has been idle for at least the specified duration.
    /// </summary>
    Task<bool> IsIdleForAsync(TimeSpan duration, CancellationToken cancellationToken = default);
}
