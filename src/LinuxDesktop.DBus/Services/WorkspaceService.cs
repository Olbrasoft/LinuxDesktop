using System.Text.Json;
using Microsoft.Extensions.Logging;
using Olbrasoft.LinuxDesktop.Core.Models;
using Olbrasoft.LinuxDesktop.Core.Services;
using Olbrasoft.LinuxDesktop.DBus.DTOs;
using Tmds.DBus.Protocol;

namespace Olbrasoft.LinuxDesktop.DBus.Services;

/// <summary>
/// Workspace service implementation using GNOME Shell "Window Calls" extension via D-Bus.
/// Requires: https://extensions.gnome.org/extension/4724/window-calls/
/// </summary>
public class WorkspaceService : DBusServiceBase, IWorkspaceService
{
    protected override string ServiceName => "org.gnome.Shell";
    protected override string ObjectPath => "/org/gnome/Shell/Extensions/Windows";
    protected override string Interface => "org.gnome.Shell.Extensions.Windows";

    private static readonly JsonSerializerOptions JsonOptions = new();

    public WorkspaceService(Connection connection, ILogger<WorkspaceService>? logger = null) : base(connection, logger)
    {
    }

    public static async Task<WorkspaceService> CreateAsync(ILogger<WorkspaceService>? logger = null, CancellationToken cancellationToken = default)
    {
        var connection = new Connection(Address.Session!);
        await connection.ConnectAsync();
        return new WorkspaceService(connection, logger);
    }

    public async Task<IReadOnlyList<WorkspaceInfo>> GetWorkspacesAsync(CancellationToken cancellationToken = default)
    {
        var json = await CallMethodReturningStringAsync("GetWorkspaces", cancellationToken);
        if (string.IsNullOrEmpty(json))
            return [];

        return ParseWorkspaceList(json);
    }

    public async Task<int> GetWorkspaceCountAsync(CancellationToken cancellationToken = default)
    {
        var message = CreateMethodCall("GetWorkspaceCount");
        return await Connection.CallMethodAsync(message, ReadUInt32, this);
    }

    public async Task<int> GetActiveWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        var message = CreateMethodCall("GetActiveWorkspace");
        return await Connection.CallMethodAsync(message, ReadUInt32, this);
    }

    public async Task SwitchWorkspaceAsync(int index, CancellationToken cancellationToken = default)
    {
        await CallMethodWithArgAsync("SwitchWorkspace", (uint)index, cancellationToken);
    }

    public async Task<IReadOnlyList<WindowInfo>> GetWorkspaceWindowsAsync(int index, CancellationToken cancellationToken = default)
    {
        var json = await CallMethodWithArgReturningStringAsync("GetWorkspaceWindows", (uint)index, cancellationToken);
        if (string.IsNullOrEmpty(json))
            return [];

        return ParseWindowList(json);
    }

    private IReadOnlyList<WorkspaceInfo> ParseWorkspaceList(string json)
    {
        try
        {
            var workspaces = JsonSerializer.Deserialize<List<WorkspaceInfoDto>>(json, JsonOptions);
            return workspaces?.Select(w => w.ToWorkspaceInfo()).ToList() ?? [];
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Failed to parse workspace list JSON. JSON: {Json}", json);
            return [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error parsing workspace list JSON. JSON: {Json}", json);
            return [];
        }
    }

    private IReadOnlyList<WindowInfo> ParseWindowList(string json)
    {
        try
        {
            var windows = JsonSerializer.Deserialize<List<WindowInfoDto>>(json, JsonOptions);
            return windows?.Select(w => w.ToWindowInfo()).ToList() ?? [];
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Failed to parse window list JSON in WorkspaceService. JSON: {Json}", json);
            return [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error parsing window list JSON in WorkspaceService. JSON: {Json}", json);
            return [];
        }
    }
}
