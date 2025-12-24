using Tmds.DBus;

namespace Olbrasoft.LinuxDesktop.Monitor.GrpcService.Services;

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
    private readonly DesktopStateCache _cache;
    private readonly ILogger<DesktopStateMonitorService> _logger;
    private Connection? _connection;
    private IDesktopState? _desktopState;
    private IDisposable? _workspaceSubscription;
    private IDisposable? _focusSubscription;

    public DesktopStateMonitorService(
        DesktopStateCache cache,
        ILogger<DesktopStateMonitorService> logger)
    {
        _cache = cache;
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

            // Create proxy to Desktop State service
            _desktopState = _connection.CreateProxy<IDesktopState>(
                "org.olbrasoft.Desktop",
                new ObjectPath("/org/olbrasoft/Desktop")
            );
            _logger.LogInformation("Created proxy to org.olbrasoft.Desktop");

            // Read initial state
            var currentWorkspace = await _desktopState.GetAsync<int>("CurrentWorkspace");
            var totalWorkspaces = await _desktopState.GetAsync<int>("TotalWorkspaces");
            var activeWindow = await _desktopState.GetAsync<string>("ActiveWindow");
            var activeApp = await _desktopState.GetAsync<string>("ActiveApplication");

            _cache.SetInitialState(currentWorkspace, totalWorkspaces, activeWindow, activeApp);

            _logger.LogInformation("Initial state loaded: Workspace {Workspace}/{Total}, Window: {Window}, App: {App}",
                currentWorkspace, totalWorkspaces, activeWindow, activeApp);

            // Subscribe to workspace changes
            _workspaceSubscription = await _desktopState.WatchWorkspaceChangedAsync(args =>
            {
                _logger.LogDebug("Workspace changed: {NewIndex} / {Total}", args.NewIndex, args.TotalWorkspaces);
                _cache.UpdateWorkspace(args.NewIndex, args.TotalWorkspaces);
            });
            _logger.LogInformation("Subscribed to WorkspaceChanged signal");

            // Subscribe to focus changes
            _focusSubscription = await _desktopState.WatchFocusChangedAsync(args =>
            {
                _logger.LogDebug("Focus changed: {Title} ({AppId})", args.WindowTitle, args.AppId);
                _cache.UpdateFocus(args.WindowTitle, args.AppId, args.WmClass);
            });
            _logger.LogInformation("Subscribed to FocusChanged signal");

            _logger.LogInformation("Desktop State Monitor Service is ready");

            // Keep service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Desktop State Monitor Service");
            throw;
        }
    }

    public override void Dispose()
    {
        _workspaceSubscription?.Dispose();
        _focusSubscription?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
