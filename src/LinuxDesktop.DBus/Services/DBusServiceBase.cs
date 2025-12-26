using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tmds.DBus.Protocol;

namespace Olbrasoft.LinuxDesktop.DBus.Services;

/// <summary>
/// Base class for D-Bus service implementations.
/// Provides common D-Bus communication patterns and disposal logic.
/// </summary>
public abstract class DBusServiceBase : IAsyncDisposable
{
    protected readonly Connection Connection;
    protected readonly ILogger Logger;
    private bool _disposed;

    /// <summary>
    /// D-Bus service name (e.g., "org.gnome.Shell").
    /// </summary>
    protected abstract string ServiceName { get; }

    /// <summary>
    /// D-Bus object path (e.g., "/org/gnome/Shell/Extensions/Windows").
    /// </summary>
    protected abstract string ObjectPath { get; }

    /// <summary>
    /// D-Bus interface name (e.g., "org.gnome.Shell.Extensions.Windows").
    /// </summary>
    protected abstract string Interface { get; }

    protected DBusServiceBase(Connection connection, ILogger? logger = null)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        Logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Calls a D-Bus method and returns string result.
    /// </summary>
    protected async Task<string> CallMethodReturningStringAsync(string method, CancellationToken cancellationToken = default)
    {
        var message = CreateMethodCall(method);
        return await Connection.CallMethodAsync(message, ReadString, this);
    }

    /// <summary>
    /// Calls a D-Bus method with uint argument and returns string result.
    /// </summary>
    protected async Task<string> CallMethodWithArgReturningStringAsync(string method, uint arg, CancellationToken cancellationToken = default)
    {
        var writer = Connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: ServiceName,
            path: ObjectPath,
            @interface: Interface,
            member: method,
            signature: "u");
        writer.WriteUInt32(arg);
        var message = writer.CreateMessage();

        return await Connection.CallMethodAsync(message, ReadString, this);
    }

    /// <summary>
    /// Calls a D-Bus method with uint argument.
    /// </summary>
    protected async Task CallMethodWithArgAsync(string method, uint arg, CancellationToken cancellationToken = default)
    {
        var writer = Connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: ServiceName,
            path: ObjectPath,
            @interface: Interface,
            member: method,
            signature: "u");
        writer.WriteUInt32(arg);
        var message = writer.CreateMessage();

        await Connection.CallMethodAsync(message);
    }

    /// <summary>
    /// Calls a D-Bus method with custom arguments.
    /// </summary>
    protected async Task CallMethodAsync(string method, Action<MessageWriter> writeArgs, string signature, CancellationToken cancellationToken = default)
    {
        var writer = Connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: ServiceName,
            path: ObjectPath,
            @interface: Interface,
            member: method,
            signature: signature);
        writeArgs(writer);
        var message = writer.CreateMessage();

        await Connection.CallMethodAsync(message);
    }

    /// <summary>
    /// Creates a D-Bus method call message.
    /// </summary>
    protected MessageBuffer CreateMethodCall(string method)
    {
        var writer = Connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: ServiceName,
            path: ObjectPath,
            @interface: Interface,
            member: method);
        return writer.CreateMessage();
    }

    protected static string ReadString(Message message, object? state)
    {
        var reader = message.GetBodyReader();
        return reader.ReadString();
    }

    protected static int ReadUInt32(Message message, object? state)
    {
        var reader = message.GetBodyReader();
        return (int)reader.ReadUInt32();
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;
        Connection.Dispose();
        return ValueTask.CompletedTask;
    }
}
