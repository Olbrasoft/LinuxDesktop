using Tmds.DBus;

namespace Olbrasoft.LinuxDesktop.Accessibility;

/// <summary>
/// D-Bus interface for AT-SPI Focus events.
/// Signals are modeled as Watch{SignalName}Async methods returning Task&lt;IDisposable&gt;
/// </summary>
[DBusInterface("org.a11y.atspi.Event.Focus")]
internal interface IAtSpiFocusEvent : IDBusObject
{
    /// <summary>
    /// Watches for Focus signal events.
    /// Returns IDisposable to unsubscribe.
    /// </summary>
    Task<IDisposable> WatchFocusAsync(
        Action<(string detail, int detail1, int detail2, object variant, IDictionary<string, object> properties)> handler,
        Action<Exception>? onError = null);
}
