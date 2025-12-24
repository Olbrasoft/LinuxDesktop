using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Tmds.DBus.Protocol;

namespace Olbrasoft.LinuxDesktop.Accessibility;

/// <summary>
/// AT-SPI accessibility service implementation using D-Bus.
/// </summary>
public class AccessibilityService : IAccessibilityService
{
    private readonly Connection _sessionBus;
    private Connection? _accessibilityBus;
    private bool _disposed;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public AccessibilityService()
    {
        _sessionBus = new Connection(Address.Session!);
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

            // Connect to session bus
            await _sessionBus.ConnectAsync();

            // Get accessibility bus address
            var writer = _sessionBus.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: "org.a11y.Bus",
                path: "/org/a11y/bus",
                @interface: "org.a11y.Bus",
                member: "GetAddress");

            var busAddress = await _sessionBus.CallMethodAsync(
                writer.CreateMessage(),
                (Message msg, object? state) => msg.GetBodyReader().ReadString(),
                null);

            // Connect to accessibility bus
            _accessibilityBus = new Connection(busAddress);
            await _accessibilityBus.ConnectAsync();

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
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
        // We would need to:
        // 1. Query all applications via Registry
        // 2. Ask each for focused widget
        // 3. Find which one reports focus
        // This is complex and not commonly needed - focus watching is the primary use case

        throw new NotImplementedException(
            "Getting current focused widget requires querying all applications. " +
            "Use WatchFocusChangesAsync() to monitor focus changes instead.");
    }

    public async IAsyncEnumerable<FocusChangedEvent> WatchFocusChangesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        if (_accessibilityBus == null)
            throw new InvalidOperationException("Accessibility bus not initialized");

        // Register for focus events
        var matchRule = "type='signal',interface='org.a11y.atspi.Event.Focus',member='Focus'";
        var matchWriter = _accessibilityBus.GetMessageWriter();
        matchWriter.WriteMethodCallHeader(
            destination: "org.freedesktop.DBus",
            path: "/org/freedesktop/DBus",
            @interface: "org.freedesktop.DBus",
            signature: "s",
            member: "AddMatch");
        matchWriter.WriteString(matchRule);
        await _accessibilityBus.CallMethodAsync(matchWriter.CreateMessage());

        // Create channel for events
        var channel = Channel.CreateUnbounded<FocusChangedEvent>();

        // Note: Tmds.DBus.Protocol doesn't expose async message reading
        // This is a known limitation from Phase 2 analysis
        // For a production implementation, we would need to:
        // 1. Use Tmds.DBus (higher-level library) instead
        // 2. Or implement custom D-Bus message loop with System.IO.Pipelines
        // 3. Or use reflection to access internal MessageStream (brittle)

        // For now, signal completion immediately to indicate the limitation
        channel.Writer.Complete(new NotImplementedException(
            "Signal listening requires Tmds.DBus (higher-level library) or custom message loop. " +
            "See AT-SPI-WAYLAND-ANALYSIS.md Phase 2 findings for details."));

        await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return evt;
        }
    }

    private async Task<AccessibleWidget?> QueryAccessibleWidgetAsync(
        string sender,
        string objectPath,
        CancellationToken cancellationToken)
    {
        if (_accessibilityBus == null)
            return null;

        try
        {
            // Query Name property
            var nameWriter = _accessibilityBus.GetMessageWriter();
            nameWriter.WriteMethodCallHeader(
                destination: sender,
                path: objectPath,
                @interface: "org.freedesktop.DBus.Properties",
                signature: "ss",
                member: "Get");
            nameWriter.WriteString("org.a11y.atspi.Accessible");
            nameWriter.WriteString("Name");

            var name = await _accessibilityBus.CallMethodAsync(
                nameWriter.CreateMessage(),
                (Message msg, object? state) =>
                {
                    var reader = msg.GetBodyReader();
                    reader.ReadSignature(); // variant signature
                    return reader.ReadString();
                },
                null);

            // Query Role property
            var roleWriter = _accessibilityBus.GetMessageWriter();
            roleWriter.WriteMethodCallHeader(
                destination: sender,
                path: objectPath,
                @interface: "org.freedesktop.DBus.Properties",
                signature: "ss",
                member: "Get");
            roleWriter.WriteString("org.a11y.atspi.Accessible");
            roleWriter.WriteString("Role");

            var roleId = await _accessibilityBus.CallMethodAsync(
                roleWriter.CreateMessage(),
                (Message msg, object? state) =>
                {
                    var reader = msg.GetBodyReader();
                    reader.ReadSignature();
                    return reader.ReadUInt32();
                },
                null);

            // Try to get application name (sender's bus name)
            var appName = sender; // Fallback to bus name

            return new AccessibleWidget(
                Name: name,
                Role: (AccessibleRole)roleId,
                ApplicationName: appName,
                ObjectPath: objectPath);
        }
        catch
        {
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        _accessibilityBus?.Dispose();
        _sessionBus.Dispose();
        _initLock.Dispose();

        await Task.CompletedTask;
    }
}
