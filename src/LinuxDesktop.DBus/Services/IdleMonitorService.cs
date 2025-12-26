using Olbrasoft.LinuxDesktop.Core.Services;
using Tmds.DBus.Protocol;

namespace Olbrasoft.LinuxDesktop.DBus.Services;

/// <summary>
/// Idle monitoring service using GNOME Mutter IdleMonitor via D-Bus.
/// </summary>
public class IdleMonitorService : DBusServiceBase, IIdleService
{
    protected override string ServiceName => "org.gnome.Mutter.IdleMonitor";
    protected override string ObjectPath => "/org/gnome/Mutter/IdleMonitor/Core";
    protected override string Interface => "org.gnome.Mutter.IdleMonitor";

    public IdleMonitorService(Connection connection) : base(connection)
    {
    }

    public static async Task<IdleMonitorService> CreateAsync(CancellationToken cancellationToken = default)
    {
        var connection = new Connection(Address.Session!);
        await connection.ConnectAsync();
        return new IdleMonitorService(connection);
    }

    public async Task<ulong> GetIdleTimeAsync(CancellationToken cancellationToken = default)
    {
        var message = CreateMethodCall("GetIdletime");
        return await Connection.CallMethodAsync(message, ReadUInt64, this);
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
}
