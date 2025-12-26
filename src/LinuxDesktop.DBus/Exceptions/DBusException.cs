namespace Olbrasoft.LinuxDesktop.DBus.Exceptions;

/// <summary>
/// Exception thrown when D-Bus communication fails.
/// </summary>
public class DBusException : Exception
{
    /// <summary>
    /// D-Bus error name (e.g., "org.freedesktop.DBus.Error.ServiceUnknown").
    /// </summary>
    public string ErrorName { get; }

    /// <summary>
    /// Creates a new DBusException with error name and message.
    /// </summary>
    /// <param name="errorName">D-Bus error name.</param>
    /// <param name="message">Human-readable error message.</param>
    public DBusException(string errorName, string message)
        : base($"{errorName}: {message}")
    {
        ErrorName = errorName;
    }

    /// <summary>
    /// Creates a new DBusException with error name, message, and inner exception.
    /// </summary>
    /// <param name="errorName">D-Bus error name.</param>
    /// <param name="message">Human-readable error message.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public DBusException(string errorName, string message, Exception innerException)
        : base($"{errorName}: {message}", innerException)
    {
        ErrorName = errorName;
    }
}
