using Tmds.DBus.Protocol;

Console.WriteLine("=== AT-SPI Debug Test ===\n");

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

    // Step 2: Connect to accessibility bus
    Console.WriteLine("2. Connecting to accessibility bus...");
    var a11yBus = new Connection(a11yBusAddress);
    await a11yBus.ConnectAsync();

    Console.WriteLine($"   ✓ Connected! UniqueName: {a11yBus.UniqueName}\n");

    // Step 3: Subscribe to Focus signals using AddMatchAsync
    Console.WriteLine("3. Subscribing to focus events using AddMatchAsync...");

    var matchRule = new MatchRule
    {
        Type = MessageType.Signal,
        Interface = "org.a11y.atspi.Event.Focus",
        Member = "Focus"
    };

    Console.WriteLine($"   Match rule: {matchRule}\n");

    var eventCount = 0;
    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    // Auto-stop after 15 seconds
    cts.CancelAfter(TimeSpan.FromSeconds(15));

    var subscription = await a11yBus.AddMatchAsync<(string sender, string path)>(
        matchRule,
        (Message message, object? state) =>
        {
            Console.WriteLine($"   [DEBUG] Message received! Type={message.MessageType}, Interface={message.InterfaceAsString}");
            var sender = message.SenderAsString ?? "unknown";
            var path = message.PathAsString ?? "/";
            return (sender, path);
        },
        (Exception? ex, (string sender, string path) data, object? readerState, object? handlerState) =>
        {
            Console.WriteLine($"   [DEBUG] Handler called! ex={ex?.Message}, sender={data.sender}, path={data.path}");

            if (ex != null)
            {
                Console.WriteLine($"   ❌ Error: {ex.Message}");
                return;
            }

            eventCount++;
            Console.WriteLine($"[{eventCount}] Focus event from {data.sender} at {data.path}");
        },
        ObserverFlags.None,
        null,
        null,
        false
    );

    Console.WriteLine("   ✓ Subscription created\n");
    Console.WriteLine("4. Monitoring focus events for 15 seconds...");
    Console.WriteLine("   Switch focus between applications now!\n");

    try
    {
        await Task.Delay(Timeout.Infinite, cts.Token);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine($"\n✓ Monitoring stopped. Total events: {eventCount}");
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
