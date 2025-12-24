using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Tmds.DBus;
using Tmds.DBus.Protocol;
using GLib;

namespace Olbrasoft.LinuxDesktop.Accessibility;

/// <summary>
/// AT-SPI accessibility service implementation using D-Bus.
/// </summary>
public class AccessibilityService : IAccessibilityService
{
    private Tmds.DBus.Protocol.Connection? _protocolConnection;
    private Tmds.DBus.Connection? _proxyConnection;
    private string? _accessibilityBusAddress;
    private bool _disposed;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    // GLib MainLoop for D-Bus event processing
    private MainLoop? _mainLoop;
    private System.Threading.Thread? _mainLoopThread;
    private readonly CancellationTokenSource _mainLoopCts = new();

    public AccessibilityService()
    {
    }

    /// <summary>
    /// Creates and initializes an AccessibilityService instance.
    /// </summary>
    public static async Task<AccessibilityService> CreateAsync(CancellationToken cancellationToken = default)
    {
        var service = new AccessibilityService();
        await service.InitializeAsync(cancellationToken);
        return service;
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
                return;

            // Get accessibility bus address from session bus
            var sessionConnection = new Tmds.DBus.Connection(Tmds.DBus.Protocol.Address.Session!);
            await sessionConnection.ConnectAsync();

            try
            {
                var a11yBus = sessionConnection.CreateProxy<IAccessibilityBus>("org.a11y.Bus", "/org/a11y/bus");
                _accessibilityBusAddress = await a11yBus.GetAddressAsync();
            }
            finally
            {
                sessionConnection.Dispose();
            }

            // Connect to accessibility bus with both Protocol and proxy connections
            _protocolConnection = new Tmds.DBus.Protocol.Connection(_accessibilityBusAddress);
            await _protocolConnection.ConnectAsync();

            _proxyConnection = new Tmds.DBus.Connection(_accessibilityBusAddress);
            await _proxyConnection.ConnectAsync();

            // Register with AT-SPI Registry to receive events
            await RegisterWithRegistryAsync();

            // Start GLib main loop to process D-Bus events
            StartMainLoop();

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private void StartMainLoop()
    {
        _mainLoopThread = new System.Threading.Thread(() =>
        {
            try
            {
                // Create main loop in this thread (GLib requires per-thread context)
                var context = MainContext.Default();

                _mainLoop = MainLoop.New(context, false);

                // Run with SynchronizationContext - this sets up MainLoopSynchronizationContext
                // which allows D-Bus callbacks to be marshalled correctly
                _mainLoop.RunWithSynchronizationContext();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainLoop error: {ex}");
            }
        })
        {
            IsBackground = true,
            Name = "GLib.MainLoop"
        };
        _mainLoopThread.Start();

        // Give the main loop time to start
        System.Threading.Thread.Sleep(200);
    }

    private async Task RegisterWithRegistryAsync()
    {
        if (_protocolConnection == null)
            throw new InvalidOperationException("Protocol connection not initialized");

        // Call org.a11y.atspi.Registry.RegisterEvent
        var writer = _protocolConnection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: "org.a11y.atspi.Registry",
            path: "/org/a11y/atspi/registry",
            @interface: "org.a11y.atspi.Registry",
            signature: "sasas",
            member: "RegisterEvent");

        // Register for state-changed:focused events
        writer.WriteString("object:state-changed:focused");
        writer.WriteArray(System.Array.Empty<string>());  // properties
        writer.WriteArray(new[] { _protocolConnection.UniqueName! });  // app_bus_name

        await _protocolConnection.CallMethodAsync(writer.CreateMessage());
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            await InitializeAsync(cancellationToken);
        }
    }

    public async Task<AccessibleWidget?> GetFocusedWidgetAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        // Note: AT-SPI doesn't have a "get currently focused widget" method
        // We would need to query all applications - use WatchFocusChangesAsync instead
        throw new NotImplementedException(
            "Getting current focused widget requires querying all applications. " +
            "Use WatchFocusChangesAsync() to monitor focus changes instead.");
    }

    public async IAsyncEnumerable<FocusChangedEvent> WatchFocusChangesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        if (_protocolConnection == null || _proxyConnection == null)
            throw new InvalidOperationException("Connections not initialized");

        var channel = Channel.CreateUnbounded<FocusChangedEvent>();

        // Create match rule for Object StateChanged signals
        var matchRule = new MatchRule
        {
            Type = MessageType.Signal,
            Interface = "org.a11y.atspi.Event.Object",
            Member = "StateChanged"
        };

        try
        {
            // Subscribe to StateChanged signals with callback handler
            // IMPORTANT: emitOnCapturedContext = true allows callbacks to run in GLib SynchronizationContext
            var subscription = await _protocolConnection.AddMatchAsync<(string sender, string path, string detail)>(
                matchRule,
                (Message message, object? state) =>
                {
                    var sender = message.SenderAsString ?? "unknown";
                    var path = message.PathAsString ?? "/";

                    // StateChanged signal arguments: (detail: string, detail1: int, detail2: int, variant: any, properties: dict)
                    var reader = message.GetBodyReader();
                    var detail = reader.ReadString().ToString();

                    return (sender, path, detail);
                },
                (Exception? ex, (string sender, string path, string detail) data, object? readerState, object? handlerState) =>
                {
                    if (ex != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Signal error: {ex.Message}");
                        return;
                    }

                    // Only process "focused" state changes
                    if (data.detail != "focused")
                        return;

                    // Fire and forget - handle signal processing asynchronously
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // Query accessible object properties via proxy
                            var accessible = _proxyConnection.CreateProxy<IAccessible>(data.sender, data.path);

                            var name = await accessible.GetNameAsync();
                            var roleId = await accessible.GetRoleAsync();
                            var role = MapRole(roleId);

                            // Get application name
                            var (appSender, appPath) = await accessible.GetApplicationAsync();
                            var app = _proxyConnection.CreateProxy<IAccessible>(appSender, appPath);
                            var appName = await app.GetNameAsync();

                            var widget = new AccessibleWidget(
                                Name: name,
                                Role: role,
                                ApplicationName: appName,
                                Description: null,
                                ObjectPath: data.path
                            );

                            var focusEvent = new FocusChangedEvent(widget, DateTimeOffset.UtcNow);

                            // Write to channel - handle cancellation gracefully
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await channel.Writer.WriteAsync(focusEvent, cancellationToken);
                            }
                        }
                        catch (Exception queryEx)
                        {
                            // Skip widgets we can't query - some may be transient or restricted
                            System.Diagnostics.Debug.WriteLine($"Failed to query accessible: {queryEx.Message}");
                        }
                    }, cancellationToken);
                },
                Tmds.DBus.Protocol.ObserverFlags.None,
                null,
                null,
                false  // Don't capture context - GLib MainLoop runs in background thread
            );

            // Cleanup when cancelled
            cancellationToken.Register(() =>
            {
                subscription.Dispose();
                channel.Writer.TryComplete();
            });
        }
        catch (Exception ex)
        {
            channel.Writer.TryComplete(ex);
        }

        await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return evt;
        }
    }

    private static AccessibleRole MapRole(int roleId)
    {
        return roleId switch
        {
            79 => AccessibleRole.Entry,
            60 => AccessibleRole.Terminal,
            61 => AccessibleRole.Text,
            43 => AccessibleRole.PushButton,
            _ => AccessibleRole.Unknown
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Stop GLib main loop
        if (_mainLoop != null)
        {
            _mainLoop.Quit();
        }

        _mainLoopCts.Cancel();

        // Wait for main loop thread to finish
        if (_mainLoopThread != null && _mainLoopThread.IsAlive)
        {
            _mainLoopThread.Join(System.TimeSpan.FromSeconds(2));
        }

        _protocolConnection?.Dispose();
        _proxyConnection?.Dispose();
        _initLock.Dispose();
        _mainLoopCts.Dispose();

        await Task.CompletedTask;
    }
}

// D-Bus interface for org.a11y.Bus
[DBusInterface("org.a11y.Bus")]
public interface IAccessibilityBus : IDBusObject
{
    Task<string> GetAddressAsync();
}
