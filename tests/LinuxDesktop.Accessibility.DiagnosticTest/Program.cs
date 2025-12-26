using Tmds.DBus;
using Tmds.DBus.Protocol;

Console.WriteLine("=== AT-SPI Diagnostic Test ===\n");

try
{
    // 1. Connect to session bus
    Console.WriteLine("[1] Connecting to session bus...");
    var sessionConnection = new Tmds.DBus.Connection(Tmds.DBus.Protocol.Address.Session!);
    await sessionConnection.ConnectAsync();
    Console.WriteLine("    ✓ Connected to session bus");

    // 2. Get AT-SPI bus address
    Console.WriteLine("\n[2] Getting AT-SPI bus address...");
    var a11yBus = sessionConnection.CreateProxy<IAccessibilityBus>("org.a11y.Bus", "/org/a11y/bus");
    var atspiAddress = await a11yBus.GetAddressAsync();
    Console.WriteLine($"    ✓ AT-SPI bus: {atspiAddress}");

    sessionConnection.Dispose();

    // 3. Connect to AT-SPI bus
    Console.WriteLine("\n[3] Connecting to AT-SPI bus...");
    var atspiConnection = new Tmds.DBus.Protocol.Connection(atspiAddress);
    await atspiConnection.ConnectAsync();
    Console.WriteLine($"    ✓ Connected with unique name: {atspiConnection.UniqueName}");

    // 4. Skip listing names (API complexity)
    Console.WriteLine("\n[4] Skipping name listing...");

    // 5. Try to register with AT-SPI Registry
    Console.WriteLine("\n[5] Registering with AT-SPI Registry...");
    var writer = atspiConnection.GetMessageWriter();
    writer.WriteMethodCallHeader(
        destination: "org.a11y.atspi.Registry",
        path: "/org/a11y/atspi/registry",
        @interface: "org.a11y.atspi.Registry",
        signature: "sasas",
        member: "RegisterEvent");

    writer.WriteString("object:state-changed:focused");
    writer.WriteArray(Array.Empty<string>());
    writer.WriteArray(new[] { atspiConnection.UniqueName! });

    await atspiConnection.CallMethodAsync(writer.CreateMessage());
    Console.WriteLine("    ✓ Registered for focus events");

    // 6. Add match rule for signals
    Console.WriteLine("\n[6] Adding match rule for StateChanged signals...");
    var writer2 = atspiConnection.GetMessageWriter();
    writer2.WriteMethodCallHeader(
        destination: "org.freedesktop.DBus",
        path: "/org/freedesktop/DBus",
        @interface: "org.freedesktop.DBus",
        signature: "s",
        member: "AddMatch");

    writer2.WriteString("type='signal',interface='org.a11y.atspi.Event.Object',member='StateChanged'");

    await atspiConnection.CallMethodAsync(writer2.CreateMessage());
    Console.WriteLine("    ✓ Match rule added");

    // 7. Listen for events
    Console.WriteLine("\n[7] Listening for events (10 seconds)...");
    Console.WriteLine("    ** CLICK IN DIFFERENT APPLICATIONS NOW! **\n");

    var eventReceived = false;
    var subscription = await atspiConnection.AddMatchAsync<string>(
        new MatchRule
        {
            Type = MessageType.Signal,
            Interface = "org.a11y.atspi.Event.Object",
            Member = "StateChanged"
        },
        (Message message, object? state) =>
        {
            var detail = message.GetBodyReader().ReadString().ToString();
            return detail;
        },
        (Exception? ex, string detail, object? readerState, object? handlerState) =>
        {
            if (ex != null)
            {
                Console.WriteLine($"    ❌ Error: {ex.Message}");
                return;
            }

            eventReceived = true;
            Console.WriteLine($"    ✓ EVENT RECEIVED! Detail: {detail}");
        },
        ObserverFlags.None,
        null,
        null,
        false
    );

    await Task.Delay(10000);
    subscription.Dispose();

    if (!eventReceived)
    {
        Console.WriteLine("    ⚠️  No events received");
    }

    atspiConnection.Dispose();

    Console.WriteLine("\n=== Diagnostic complete ===");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ ERROR: {ex.Message}");
    Console.WriteLine($"\nStack trace:");
    Console.WriteLine(ex.StackTrace);
    Environment.Exit(1);
}

[DBusInterface("org.a11y.Bus")]
public interface IAccessibilityBus : IDBusObject
{
    Task<string> GetAddressAsync();
}
