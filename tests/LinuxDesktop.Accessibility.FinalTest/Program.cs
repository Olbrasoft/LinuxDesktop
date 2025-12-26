using Olbrasoft.LinuxDesktop.Accessibility;

Console.WriteLine("=== AT-SPI Focus Detection - Final Test ===\n");

try
{
    Console.WriteLine("1. Creating AccessibilityService...");
    await using var service = await AccessibilityService.CreateAsync();
    Console.WriteLine("   ✓ Service created and registered with AT-SPI Registry\n");

    Console.WriteLine("2. Starting focus event monitoring...");
    Console.WriteLine("   This will run for 30 seconds.");
    Console.WriteLine("   ** TRY CLICKING IN DIFFERENT TEXT FIELDS AND BUTTONS! **\n");

    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    cts.CancelAfter(TimeSpan.FromSeconds(30));

    var eventCount = 0;

    try
    {
        await foreach (var focusEvent in service.WatchFocusChangesAsync(cts.Token))
        {
            eventCount++;
            Console.WriteLine($"\n[{eventCount}] FOCUS EVENT RECEIVED!");
            Console.WriteLine($"    Widget:      {focusEvent.Widget.Name}");
            Console.WriteLine($"    Role:        {focusEvent.Widget.Role}");
            Console.WriteLine($"    Application: {focusEvent.Widget.ApplicationName}");
            Console.WriteLine($"    Time:        {focusEvent.Timestamp:HH:mm:ss.fff}");
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine($"\n\n=== Monitoring stopped ===");
        Console.WriteLine($"Total focus events received: {eventCount}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ ERROR: {ex.Message}");
    Console.WriteLine($"\nStack trace:");
    Console.WriteLine(ex.StackTrace);
    Environment.Exit(1);
}

Console.WriteLine("\n✅ Test complete!");
