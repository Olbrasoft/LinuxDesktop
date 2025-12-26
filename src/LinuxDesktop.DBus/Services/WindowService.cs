using System.Text.Json;
using Olbrasoft.LinuxDesktop.Core.Models;
using Olbrasoft.LinuxDesktop.Core.Services;
using Olbrasoft.LinuxDesktop.DBus.DTOs;
using Tmds.DBus.Protocol;

namespace Olbrasoft.LinuxDesktop.DBus.Services;

/// <summary>
/// Window service implementation using GNOME Shell "Window Calls" extension via D-Bus.
/// Requires: https://extensions.gnome.org/extension/4724/window-calls/
/// </summary>
public class WindowService : DBusServiceBase, IWindowService
{
    protected override string ServiceName => "org.gnome.Shell";
    protected override string ObjectPath => "/org/gnome/Shell/Extensions/Windows";
    protected override string Interface => "org.gnome.Shell.Extensions.Windows";

    private static readonly JsonSerializerOptions JsonOptions = new();

    public WindowService(Connection connection) : base(connection)
    {
    }

    public static async Task<WindowService> CreateAsync(CancellationToken cancellationToken = default)
    {
        var connection = new Connection(Address.Session!);
        await connection.ConnectAsync();
        return new WindowService(connection);
    }

    public async Task<IReadOnlyList<WindowInfo>> GetWindowsAsync(CancellationToken cancellationToken = default)
    {
        var json = await CallMethodReturningStringAsync("List", cancellationToken);
        if (string.IsNullOrEmpty(json))
            return [];

        return ParseWindowList(json);
    }

    public async Task<WindowDetails?> GetWindowDetailsAsync(uint windowId, CancellationToken cancellationToken = default)
    {
        var json = await CallMethodWithArgReturningStringAsync("Details", windowId, cancellationToken);
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
        return await CallMethodWithArgReturningStringAsync("GetTitle", windowId, cancellationToken);
    }

    public async Task ActivateWindowAsync(uint windowId, CancellationToken cancellationToken = default)
    {
        await CallMethodWithArgAsync("Activate", windowId, cancellationToken);
    }

    public async Task CloseWindowAsync(uint windowId, CancellationToken cancellationToken = default)
    {
        await CallMethodWithArgAsync("Close", windowId, cancellationToken);
    }

    public async Task MaximizeWindowAsync(uint windowId, CancellationToken cancellationToken = default)
    {
        await CallMethodWithArgAsync("Maximize", windowId, cancellationToken);
    }

    public async Task MinimizeWindowAsync(uint windowId, CancellationToken cancellationToken = default)
    {
        await CallMethodWithArgAsync("Minimize", windowId, cancellationToken);
    }

    public async Task UnmaximizeWindowAsync(uint windowId, CancellationToken cancellationToken = default)
    {
        await CallMethodWithArgAsync("Unmaximize", windowId, cancellationToken);
    }

    public async Task UnminimizeWindowAsync(uint windowId, CancellationToken cancellationToken = default)
    {
        await CallMethodWithArgAsync("Unminimize", windowId, cancellationToken);
    }

    public async Task MoveWindowAsync(uint windowId, int x, int y, CancellationToken cancellationToken = default)
    {
        await CallMethodAsync("Move", writer =>
        {
            writer.WriteUInt32(windowId);
            writer.WriteInt32(x);
            writer.WriteInt32(y);
        }, "uii", cancellationToken);
    }

    public async Task ResizeWindowAsync(uint windowId, int width, int height, CancellationToken cancellationToken = default)
    {
        await CallMethodAsync("Resize", writer =>
        {
            writer.WriteUInt32(windowId);
            writer.WriteUInt32((uint)width);
            writer.WriteUInt32((uint)height);
        }, "uuu", cancellationToken);
    }

    public async Task MoveWindowToWorkspaceAsync(uint windowId, int workspaceIndex, CancellationToken cancellationToken = default)
    {
        await CallMethodAsync("MoveToWorkspace", writer =>
        {
            writer.WriteUInt32(windowId);
            writer.WriteUInt32((uint)workspaceIndex);
        }, "uu", cancellationToken);
    }

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
}
