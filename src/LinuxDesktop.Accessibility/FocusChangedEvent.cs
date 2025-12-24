namespace Olbrasoft.LinuxDesktop.Accessibility;

/// <summary>
/// Event raised when keyboard focus changes to a different accessible widget.
/// </summary>
/// <param name="Widget">The newly focused widget</param>
/// <param name="Timestamp">When the focus change occurred</param>
public record FocusChangedEvent(
    AccessibleWidget Widget,
    DateTimeOffset Timestamp);
