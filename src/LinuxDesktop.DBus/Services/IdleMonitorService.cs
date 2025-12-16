using Olbrasoft.LinuxDesktop.Core.Services;
using Tmds.DBus.Protocol;

namespace Olbrasoft.LinuxDesktop.DBus.Services;

/// <summary>
/// Idle monitoring service using GNOME Mutter IdleMonitor via D-Bus.
/// </summary>
public class IdleMonitorService : IIdleService, IAsyncDisposable
{
    private const string ServiceName = "org.gnome.Mutter.IdleMonitor";
    private const string ObjectPath = "/org/gnome/Mutter/IdleMonitor/Core";
    private const string Interface = "org.gnome.Mutter.IdleMonitor";

    private readonly Connection _connection;
    private bool _disposed;

    public IdleMonitorService(Connection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public static async Task<IdleMonitorService> CreateAsync(CancellationToken cancellationToken = default)
    {
        var connection = new Connection(Address.Session!);
        await connection.ConnectAsync();
        return new IdleMonitorService(connection);
    }

    public async Task<ulong> GetIdleTimeAsync(CancellationToken cancellationToken = default)
    {
        var writer = _connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: ServiceName,
            path: ObjectPath,
            @interface: Interface,
            member: "GetIdletime");
        var message = writer.CreateMessage();

        return await _connection.CallMethodAsync(message, ReadUInt64, this);
    }

    private static ulong ReadUInt64(Message message, object? state)
    {
        var reader = message.GetBodyReader();
        return reader.ReadUInt64();
    }

    public async Task<TimeSpan> GetIdleTimeSpanAsync(CancellationToken cancellationToken = default)
    {
        var idleTimeMs = await GetIdleTimeAsync(cancellationToken);
        return TimeSpan.FromMilliseconds(idleTimeMs);
    }

    public async Task<bool> IsIdleForAsync(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var idleTime = await GetIdleTimeSpanAsync(cancellationToken);
        return idleTime >= duration;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _connection.Dispose();
        await Task.CompletedTask;
    }
}
