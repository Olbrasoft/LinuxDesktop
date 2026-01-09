using Microsoft.Extensions.Logging;
using Olbrasoft.LinuxDesktop.Core.Services;
using Tmds.DBus.Protocol;

namespace Olbrasoft.LinuxDesktop.DBus.Services;

/// <summary>
/// Pointer service implementation using focus-tracker@olbrasoft.cz GNOME Shell extension via D-Bus.
/// Provides cursor position and active window geometry queries.
/// </summary>
public class PointerService : DBusServiceBase, IPointerService
{
    protected override string ServiceName => "org.olbrasoft.Desktop";
    protected override string ObjectPath => "/org/olbrasoft/Desktop";
    protected override string Interface => "org.olbrasoft.Desktop";

    private bool _serviceAvailable = true;

    public PointerService(Connection connection, ILogger<PointerService>? logger = null)
        : base(connection, logger)
    {
    }

    /// <summary>
    /// Creates a new PointerService instance with automatic D-Bus connection.
    /// </summary>
    public static async Task<PointerService> CreateAsync(ILogger<PointerService>? logger = null, CancellationToken ct = default)
    {
        var connection = new Connection(Address.Session!);
        await connection.ConnectAsync();
        return new PointerService(connection, logger);
    }

    /// <inheritdoc/>
    public async Task<(int X, int Y)?> GetPointerPositionAsync(CancellationToken ct = default)
    {
        if (!_serviceAvailable)
            return null;

        try
        {
            var message = CreateMethodCall("GetPointerPosition");
            var result = await Connection.CallMethodAsync(message, ReadInt32Tuple, this);
            return result;
        }
        catch (DBusException ex) when (IsServiceUnavailableError(ex))
        {
            Logger.LogWarning("GNOME Shell extension focus-tracker@olbrasoft.cz not available: {Error}", ex.ErrorName);
            _serviceAvailable = false;
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get pointer position");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<(int X, int Y, int Width, int Height)?> GetActiveWindowGeometryAsync(CancellationToken ct = default)
    {
        if (!_serviceAvailable)
            return null;

        try
        {
            var message = CreateMethodCall("GetActiveWindowGeometry");
            var result = await Connection.CallMethodAsync(message, ReadInt32QuadTuple, this);

            // Extension returns (0, 0, 0, 0) when no window is focused
            if (result is { X: 0, Y: 0, Width: 0, Height: 0 })
                return null;

            return result;
        }
        catch (DBusException ex) when (IsServiceUnavailableError(ex))
        {
            Logger.LogWarning("GNOME Shell extension focus-tracker@olbrasoft.cz not available: {Error}", ex.ErrorName);
            _serviceAvailable = false;
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get active window geometry");
            return null;
        }
    }

    private static bool IsServiceUnavailableError(DBusException ex)
    {
        return ex.ErrorName is
            "org.freedesktop.DBus.Error.ServiceUnknown" or
            "org.freedesktop.DBus.Error.UnknownMethod" or
            "org.freedesktop.DBus.Error.NoReply";
    }

    private static (int X, int Y) ReadInt32Tuple(Message message, object? state)
    {
        var reader = message.GetBodyReader();
        reader.AlignStruct();
        var x = reader.ReadInt32();
        var y = reader.ReadInt32();
        return (x, y);
    }

    private static (int X, int Y, int Width, int Height) ReadInt32QuadTuple(Message message, object? state)
    {
        var reader = message.GetBodyReader();
        reader.AlignStruct();
        var x = reader.ReadInt32();
        var y = reader.ReadInt32();
        var width = reader.ReadInt32();
        var height = reader.ReadInt32();
        return (x, y, width, height);
    }
}
