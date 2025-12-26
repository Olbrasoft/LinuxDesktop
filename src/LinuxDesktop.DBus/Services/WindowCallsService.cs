using System.Text.Json;
using Olbrasoft.LinuxDesktop.Core.Models;
using Olbrasoft.LinuxDesktop.Core.Services;
using Olbrasoft.LinuxDesktop.DBus.DTOs;
using Olbrasoft.LinuxDesktop.DBus.Exceptions;
using Tmds.DBus.Protocol;

namespace Olbrasoft.LinuxDesktop.DBus.Services;

/// <summary>
/// Window service implementation using GNOME Shell "Window Calls" extension via D-Bus.
/// Requires: https://extensions.gnome.org/extension/4724/window-calls/
/// </summary>
public class WindowCallsService : IWindowService, IWorkspaceService, IAsyncDisposable
{
    private const string ServiceName = "org.gnome.Shell";
    private const string ObjectPath = "/org/gnome/Shell/Extensions/Windows";
    private const string Interface = "org.gnome.Shell.Extensions.Windows";

    private readonly Connection _connection;
    private bool _disposed;

    public WindowCallsService(Connection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public static async Task<WindowCallsService> CreateAsync(CancellationToken cancellationToken = default)
    {
        var connection = new Connection(Address.Session!);
        await connection.ConnectAsync();
        return new WindowCallsService(connection);
    }

    public async Task<IReadOnlyList<WindowInfo>> GetWindowsAsync(CancellationToken cancellationToken = default)
    {
        var json = await CallMethodReturningStringAsync("List");
        if (string.IsNullOrEmpty(json))
            return Array.Empty<WindowInfo>();

        return ParseWindowList(json);
    }

    public async Task<WindowDetails?> GetWindowDetailsAsync(uint windowId, CancellationToken cancellationToken = default)
    {
        var json = await CallMethodWithArgReturningStringAsync("Details", windowId);
        if (string.IsNullOrEmpty(json))
            return null;

        return ParseWindowDetails(json);
    }

    public async Task<WindowInfo?> GetFocusedWindowAsync(CancellationToken cancellationToken = default)
    {
        var windows = await GetWindowsAsync(cancellationToken);
        return windows.FirstOrDefault(w => w.HasFocus);
    }

    public async Task<string?> GetWindowTitleAsync(uint windowId, CancellationToken cancellationToken = default)
    {
        return await CallMethodWithArgReturningStringAsync("GetTitle", windowId);
    }

    public async Task ActivateWindowAsync(uint windowId, CancellationToken cancellationToken = default)
    {
        await CallMethodWithArgAsync("Activate", windowId);
    }

    public async Task CloseWindowAsync(uint windowId, CancellationToken cancellationToken = default)
    {
        await CallMethodWithArgAsync("Close", windowId);
    }

    public async Task MaximizeWindowAsync(uint windowId, CancellationToken cancellationToken = default)
    {
        await CallMethodWithArgAsync("Maximize", windowId);
    }

    public async Task MinimizeWindowAsync(uint windowId, CancellationToken cancellationToken = default)
    {
        await CallMethodWithArgAsync("Minimize", windowId);
    }

    public async Task UnmaximizeWindowAsync(uint windowId, CancellationToken cancellationToken = default)
    {
        await CallMethodWithArgAsync("Unmaximize", windowId);
    }

    public async Task UnminimizeWindowAsync(uint windowId, CancellationToken cancellationToken = default)
    {
        await CallMethodWithArgAsync("Unminimize", windowId);
    }

    public async Task MoveWindowAsync(uint windowId, int x, int y, CancellationToken cancellationToken = default)
    {
        await CallMethodAsync("Move", writer =>
        {
            writer.WriteUInt32(windowId);
            writer.WriteInt32(x);
            writer.WriteInt32(y);
        }, "uii");
    }

    public async Task ResizeWindowAsync(uint windowId, int width, int height, CancellationToken cancellationToken = default)
    {
        await CallMethodAsync("Resize", writer =>
        {
            writer.WriteUInt32(windowId);
            writer.WriteUInt32((uint)width);
            writer.WriteUInt32((uint)height);
        }, "uuu");
    }

    public async Task MoveWindowToWorkspaceAsync(uint windowId, int workspaceIndex, CancellationToken cancellationToken = default)
    {
        await CallMethodAsync("MoveToWorkspace", writer =>
        {
            writer.WriteUInt32(windowId);
            writer.WriteUInt32((uint)workspaceIndex);
        }, "uu");
    }

    // IWorkspaceService implementation

    public async Task<IReadOnlyList<WorkspaceInfo>> GetWorkspacesAsync(CancellationToken cancellationToken = default)
    {
        var json = await CallMethodReturningStringAsync("GetWorkspaces");
        if (string.IsNullOrEmpty(json))
            return Array.Empty<WorkspaceInfo>();

        return ParseWorkspaceList(json);
    }

    public async Task<int> GetWorkspaceCountAsync(CancellationToken cancellationToken = default)
    {
        var message = CreateMethodCall("GetWorkspaceCount");
        return await _connection.CallMethodAsync(message, ReadUInt32, this);
    }

    public async Task<int> GetActiveWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        var message = CreateMethodCall("GetActiveWorkspace");
        return await _connection.CallMethodAsync(message, ReadUInt32, this);
    }

    public async Task SwitchWorkspaceAsync(int index, CancellationToken cancellationToken = default)
    {
        await CallMethodWithArgAsync("SwitchWorkspace", (uint)index);
    }

    public async Task<IReadOnlyList<WindowInfo>> GetWorkspaceWindowsAsync(int index, CancellationToken cancellationToken = default)
    {
        var json = await CallMethodWithArgReturningStringAsync("GetWorkspaceWindows", (uint)index);
        if (string.IsNullOrEmpty(json))
            return Array.Empty<WindowInfo>();

        return ParseWindowList(json);
    }

    private async Task<string> CallMethodReturningStringAsync(string method)
    {
        var message = CreateMethodCall(method);
        return await _connection.CallMethodAsync(message, ReadString, this);
    }

    private async Task<string> CallMethodWithArgReturningStringAsync(string method, uint arg)
    {
        var writer = _connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: ServiceName,
            path: ObjectPath,
            @interface: Interface,
            member: method,
            signature: "u");
        writer.WriteUInt32(arg);
        var message = writer.CreateMessage();

        return await _connection.CallMethodAsync(message, ReadString, this);
    }

    private async Task CallMethodWithArgAsync(string method, uint arg)
    {
        var writer = _connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: ServiceName,
            path: ObjectPath,
            @interface: Interface,
            member: method,
            signature: "u");
        writer.WriteUInt32(arg);
        var message = writer.CreateMessage();

        await _connection.CallMethodAsync(message);
    }

    private async Task CallMethodAsync(string method, Action<MessageWriter> writeArgs, string signature)
    {
        var writer = _connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: ServiceName,
            path: ObjectPath,
            @interface: Interface,
            member: method,
            signature: signature);
        writeArgs(writer);
        var message = writer.CreateMessage();

        await _connection.CallMethodAsync(message);
    }

    private MessageBuffer CreateMethodCall(string method)
    {
        var writer = _connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: ServiceName,
            path: ObjectPath,
            @interface: Interface,
            member: method);
        return writer.CreateMessage();
    }

    private static string ReadString(Message message, object? state)
    {
        var reader = message.GetBodyReader();
        return reader.ReadString();
    }

    private static int ReadUInt32(Message message, object? state)
    {
        var reader = message.GetBodyReader();
        return (int)reader.ReadUInt32();
    }

    private static readonly JsonSerializerOptions JsonOptions = new();

    private static IReadOnlyList<WindowInfo> ParseWindowList(string json)
    {
        try
        {
            var windows = JsonSerializer.Deserialize<List<WindowInfoDto>>(json, JsonOptions);
            return windows?.Select(w => w.ToWindowInfo()).ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static WindowDetails? ParseWindowDetails(string json)
    {
        try
        {
            var window = JsonSerializer.Deserialize<WindowDetailsDto>(json, JsonOptions);
            return window?.ToWindowDetails();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyList<WorkspaceInfo> ParseWorkspaceList(string json)
    {
        try
        {
            var workspaces = JsonSerializer.Deserialize<List<WorkspaceInfoDto>>(json, JsonOptions);
            return workspaces?.Select(w => w.ToWorkspaceInfo()).ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _connection.Dispose();
        await Task.CompletedTask;
    }
}
