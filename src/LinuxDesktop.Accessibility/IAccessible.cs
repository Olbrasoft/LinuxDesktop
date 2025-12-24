using Tmds.DBus;

namespace Olbrasoft.LinuxDesktop.Accessibility;

/// <summary>
/// D-Bus interface for AT-SPI Accessible objects.
/// </summary>
[DBusInterface("org.a11y.atspi.Accessible")]
public interface IAccessible : IDBusObject
{
    /// <summary>
    /// Gets the localized name of this accessible.
    /// </summary>
    Task<string> GetNameAsync();

    /// <summary>
    /// Gets the role of this accessible as a numeric identifier.
    /// </summary>
    Task<int> GetRoleAsync();

    /// <summary>
    /// Gets the application this accessible belongs to.
    /// Returns (bus_name, object_path) tuple.
    /// </summary>
    Task<(string, string)> GetApplicationAsync();

    /// <summary>
    /// Gets the description of this accessible.
    /// </summary>
    Task<string> GetDescriptionAsync();
}
