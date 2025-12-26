using Tmds.DBus.Protocol;

Console.WriteLine("=== AT-SPI Object Events Test (.NET) ===\n");

try
{
    // Connect to accessibility bus
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

    Console.WriteLine($"   ✓ Address: {a11yBusAddress}\n");
    sessionBus.Dispose();

    Console.WriteLine("2. Connecting to accessibility bus...");
    var a11yBus = new Connection(a11yBusAddress);
    await a11yBus.ConnectAsync();
    Console.WriteLine($"   ✓ Connected! UniqueName: {a11yBus.UniqueName}\n");

    // Try monitoring Object events instead of Focus
    Console.WriteLine("3. Subscribing to org.a11y.atspi.Event.Object:StateChanged...");

    var matchRule = new MatchRule
    {
        Type = MessageType.Signal,
        Interface = "org.a11y.atspi.Event.Object",
        Member = "StateChanged"
    };

    Console.WriteLine($"   Match rule: {matchRule}\n");

    var eventCount = 0;
    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    cts.CancelAfter(TimeSpan.FromSeconds(20));

    var subscription = await a11yBus.AddMatchAsync<(string iface, string member)>(
        matchRule,
        (Message message, object? state) =>
        {
            return (message.InterfaceAsString ?? "?", message.MemberAsString ?? "?");
        },
        (Exception? ex, (string iface, string member) data, object? rs, object? hs) =>
        {
            if (ex != null)
            {
                Console.WriteLine($"   ❌ Error: {ex.Message}");
                return;
            }

            eventCount++;
            Console.WriteLine($"[{eventCount}] Signal: {data.iface}.{data.member}");
        },
        ObserverFlags.None,
        null,
        null,
        false
    );

    Console.WriteLine("   ✓ Subscription created\n");
    Console.WriteLine("4. Monitoring for 20 seconds...");
    Console.WriteLine("   ** INTERACT WITH APPS - CLICK AROUND! **\n");

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
