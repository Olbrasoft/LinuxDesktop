using Tmds.DBus.Protocol;

Console.WriteLine("=== AT-SPI with Registry Registration Test ===\n");

try
{
    // Step 1: Connect to accessibility bus
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

    // Step 3: Register with org.a11y.atspi.Registry
    Console.WriteLine("3. Registering with org.a11y.atspi.Registry...");

    var registerWriter = a11yBus.GetMessageWriter();
    registerWriter.WriteMethodCallHeader(
        destination: "org.a11y.atspi.Registry",
        path: "/org/a11y/atspi/registry",
        @interface: "org.a11y.atspi.Registry",
        signature: "sasas",  // (event, properties, app_bus_name)
        member: "RegisterEvent");

    // Parameters
    registerWriter.WriteString("object:state-changed:focused");  // event
    registerWriter.WriteArray(Array.Empty<string>());  // properties (empty array)
    registerWriter.WriteArray(new[] { a11yBus.UniqueName! });  // app_bus_name

    await a11yBus.CallMethodAsync(registerWriter.CreateMessage());
    Console.WriteLine("   ✓ Registered for object:state-changed:focused\n");

    // Step 4: Add D-Bus match rule
    Console.WriteLine("4. Adding D-Bus match rule...");

    var matchRule = new MatchRule
    {
        Type = MessageType.Signal,
        Interface = "org.a11y.atspi.Event.Object",
        Member = "StateChanged"
    };

    var eventCount = 0;
    var focusedEventCount = 0;
    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    cts.CancelAfter(TimeSpan.FromSeconds(20));

    var subscription = await a11yBus.AddMatchAsync<(string detail, int detail1)>(
        matchRule,
        (Message message, object? state) =>
        {
            var reader = message.GetBodyReader();
            var detail = reader.ReadString();
            var detail1 = reader.ReadInt32();
            return (detail.ToString(), detail1);
        },
        (Exception? ex, (string detail, int detail1) data, object? rs, object? hs) =>
        {
            if (ex != null)
            {
                Console.WriteLine($"   ❌ Error: {ex.Message}");
                return;
            }

            eventCount++;
            if (data.detail == "focused")
            {
                focusedEventCount++;
                Console.WriteLine($"[{focusedEventCount}] FOCUSED event! (detail1={data.detail1})");
            }
            else if (eventCount % 10 == 0)
            {
                Console.WriteLine($"   ... {eventCount} events total ({data.detail})");
            }
        },
        ObserverFlags.None,
        null,
        null,
        false
    );

    Console.WriteLine("   ✓ Match rule added\n");
    Console.WriteLine("5. Monitoring for 20 seconds...");
    Console.WriteLine("   ** CLICK IN TEXT FIELDS, BUTTONS, ETC! **\n");

    try
    {
        await Task.Delay(Timeout.Infinite, cts.Token);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine($"\n✓ Stopped.");
        Console.WriteLine($"   Total events: {eventCount}");
        Console.WriteLine($"   Focused events: {focusedEventCount}");
    }

    // Deregister
    Console.WriteLine("\n6. Deregistering from Registry...");
    var deregisterWriter = a11yBus.GetMessageWriter();
    deregisterWriter.WriteMethodCallHeader(
        destination: "org.a11y.atspi.Registry",
        path: "/org/a11y/atspi/registry",
        @interface: "org.a11y.atspi.Registry",
        signature: "s",
        member: "DeregisterEvent");
    deregisterWriter.WriteString("object:state-changed:focused");

    await a11yBus.CallMethodAsync(deregisterWriter.CreateMessage());
    Console.WriteLine("   ✓ Deregistered\n");

    subscription.Dispose();
    a11yBus.Dispose();

    Console.WriteLine("✅ Test complete!");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Environment.Exit(1);
}
