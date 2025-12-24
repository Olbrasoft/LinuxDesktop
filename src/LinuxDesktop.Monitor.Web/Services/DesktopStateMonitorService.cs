using Microsoft.AspNetCore.SignalR;
using Olbrasoft.LinuxDesktop.Monitor.Web.Hubs;
using Tmds.DBus;

namespace Olbrasoft.LinuxDesktop.Monitor.Web.Services;

public struct WorkspaceChangedArgs
{
    public int NewIndex;
    public int TotalWorkspaces;
}

public struct FocusChangedArgs
{
    public string WindowTitle;
    public string AppId;
    public string WmClass;
}

[DBusInterface("org.olbrasoft.Desktop")]
public interface IDesktopState : IDBusObject
{
    Task<T> GetAsync<T>(string propertyName);
    Task<IDisposable> WatchWorkspaceChangedAsync(Action<WorkspaceChangedArgs> handler);
    Task<IDisposable> WatchFocusChangedAsync(Action<FocusChangedArgs> handler);
}

public class DesktopStateMonitorService : BackgroundService
{
    private readonly IHubContext<DesktopStateHub> _hubContext;
    private readonly ILogger<DesktopStateMonitorService> _logger;
    private Connection? _connection;
    private IDesktopState? _desktopState;
    private IDisposable? _workspaceSubscription;
    private IDisposable? _focusSubscription;

    public DesktopStateMonitorService(
        IHubContext<DesktopStateHub> hubContext,
        ILogger<DesktopStateMonitorService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Desktop State Monitor Service starting...");

        try
        {
            // Connect to D-Bus
            _connection = new Connection(Address.Session!);
            await _connection.ConnectAsync();
            _logger.LogInformation("Connected to D-Bus session bus");

            await SendLog("✓ Connected to D-Bus");

            // Create proxy to Desktop State service
            _desktopState = _connection.CreateProxy<IDesktopState>(
                "org.olbrasoft.Desktop",
                new ObjectPath("/org/olbrasoft/Desktop")
            );
            _logger.LogInformation("Created proxy to org.olbrasoft.Desktop");

            await SendLog("✓ Connected to Desktop State Extension");

            // Read initial state
            var currentWorkspace = await _desktopState.GetAsync<int>("CurrentWorkspace");
            var totalWorkspaces = await _desktopState.GetAsync<int>("TotalWorkspaces");
            var activeWindow = await _desktopState.GetAsync<string>("ActiveWindow");
            var activeApp = await _desktopState.GetAsync<string>("ActiveApplication");

            await SendLog($"Initial state: Workspace {currentWorkspace}/{totalWorkspaces}");
            await SendLog($"Active window: \"{activeWindow}\"");
            await SendLog($"Active application: \"{activeApp}\"");

            // Subscribe to workspace changes
            _workspaceSubscription = await _desktopState.WatchWorkspaceChangedAsync(async args =>
            {
                _logger.LogInformation("Workspace changed: {NewIndex} / {Total}", args.NewIndex, args.TotalWorkspaces);

                await _hubContext.Clients.All.SendAsync(
                    "WorkspaceChanged",
                    args.NewIndex,
                    args.TotalWorkspaces,
                    cancellationToken: stoppingToken);

                await SendLog($"→ Workspace changed: {args.NewIndex} / {args.TotalWorkspaces}");
            });
            _logger.LogInformation("Subscribed to WorkspaceChanged signal");

            await SendLog("✓ Subscribed to WorkspaceChanged");

            // Subscribe to focus changes
            _focusSubscription = await _desktopState.WatchFocusChangedAsync(async args =>
            {
                _logger.LogInformation("Focus changed: {Title} ({AppId})", args.WindowTitle, args.AppId);

                await _hubContext.Clients.All.SendAsync(
                    "FocusChanged",
                    args.WindowTitle,
                    args.AppId,
                    args.WmClass,
                    cancellationToken: stoppingToken);

                await SendLog($"→ Focus: \"{args.WindowTitle}\" ({args.AppId})");
            });
            _logger.LogInformation("Subscribed to FocusChanged signal");

            await SendLog("✓ Subscribed to FocusChanged");
            await SendLog("📡 Monitoring desktop state...");

            // Keep service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Desktop State Monitor Service");
            await SendLog($"✗ Error: {ex.Message}");
            throw;
        }
    }

    private async Task SendLog(string message)
    {
        await _hubContext.Clients.All.SendAsync("LogMessage", message);
    }

    public override void Dispose()
    {
        _workspaceSubscription?.Dispose();
        _focusSubscription?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
