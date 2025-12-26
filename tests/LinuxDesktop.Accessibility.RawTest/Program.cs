using System.Buffers;
using Tmds.DBus.Protocol;

Console.WriteLine("=== AT-SPI Raw Message Test ===\n");

try
{
    // Step 1: Get accessibility bus address
    Console.WriteLine("1. Getting accessibility bus address...");
    var sessionBus = new Connection(Address.Session!);
    await sessionBus.ConnectAsync();

    var writer = sessionBus.GetMessageWriter();
    writer.WriteMethodCallHeader(
        destination: "org.a11y.Bus",
        path: "/org/a11y/bus",
        @interface: "org.a11y.Bus",
        member: "GetAddress");

    var a11yBusAddress = await sessionBus.CallMethodAsync(writer.CreateMessage(), (Message msg, object? state) =>
    {
        var reader = msg.GetBodyReader();
        return reader.ReadString();
    }, null);

    Console.WriteLine($"   ✓ Accessibility bus: {a11yBusAddress}\n");
    sessionBus.Dispose();

    // Step 2: Connect to accessibility bus with explicit options
    Console.WriteLine("2. Connecting to accessibility bus...");
    var connectionOptions = new ClientConnectionOptions(a11yBusAddress)
    {
        AutoConnect = false  // Manual control
    };
    var a11yBus = new Connection(connectionOptions);
    await a11yBus.ConnectAsync();

    Console.WriteLine($"   ✓ Connected! UniqueName: {a11yBus.UniqueName}\n");

    // Step 3: Manually add match rule via D-Bus method call
    Console.WriteLine("3. Adding match rule manually...");
    var matchRule = "type='signal',interface='org.a11y.atspi.Event.Focus',member='Focus'";

    var matchWriter = a11yBus.GetMessageWriter();
    matchWriter.WriteMethodCallHeader(
        destination: "org.freedesktop.DBus",
        path: "/org/freedesktop/DBus",
        @interface: "org.freedesktop.DBus",
        signature: "s",
        member: "AddMatch");
    matchWriter.WriteString(matchRule);

    await a11yBus.CallMethodAsync(matchWriter.CreateMessage());
    Console.WriteLine($"   ✓ Match rule added: {matchRule}\n");

    // Step 4: Try AddMatchAsync with callback
    Console.WriteLine("4. Adding callback handler...");
    var eventCount = 0;

    var subscription = await a11yBus.AddMatchAsync<(string, string)>(
        new MatchRule
        {
            Type = MessageType.Signal,
            Interface = "org.a11y.atspi.Event.Focus",
            Member = "Focus"
        },
        (Message message, object? state) =>
        {
            var sender = message.SenderAsString ?? "unknown";
            var path = message.PathAsString ?? "/";
            Console.WriteLine($"   [READER] Message from {sender} at {path}");
            return (sender, path);
        },
        (Exception? ex, (string, string) data, object? rs, object? hs) =>
        {
            if (ex != null)
            {
                Console.WriteLine($"   [HANDLER ERROR] {ex.Message}");
                return;
            }

            eventCount++;
            Console.WriteLine($"[{eventCount}] Focus: {data.Item1} -> {data.Item2}");
        },
        ObserverFlags.None,
        null,
        null,
        true  // emitOnCapturedContext
    );

    Console.WriteLine("   ✓ Callback handler registered\n");

    Console.WriteLine("5. Monitoring for 20 seconds...");
    Console.WriteLine("   ** SWITCH FOCUS BETWEEN APPS NOW! **\n");

    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    cts.CancelAfter(TimeSpan.FromSeconds(20));

    try
    {
        await Task.Delay(Timeout.Infinite, cts.Token);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine($"\n✓ Stopped. Total events: {eventCount}");
    }

    subscription.Dispose();
    a11yBus.Dispose();
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Environment.Exit(1);
}
