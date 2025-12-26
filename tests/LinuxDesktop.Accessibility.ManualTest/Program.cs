using Olbrasoft.LinuxDesktop.Accessibility;

Console.WriteLine("=== AT-SPI Focus Detection Test (New API) ===\n");

try
{
    Console.WriteLine("Creating accessibility service...");
    await using var service = await AccessibilityService.CreateAsync();
    Console.WriteLine("✓ Service created\n");

    Console.WriteLine("Subscribing to focus events...");
    Console.WriteLine("Switch focus between applications to test.");
    Console.WriteLine("Press Ctrl+C to exit.\n");

    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    var eventCount = 0;

    await foreach (var focusEvent in service.WatchFocusChangesAsync(cts.Token))
    {
        eventCount++;
        Console.WriteLine($"[{eventCount}] Focus changed at {focusEvent.Timestamp:HH:mm:ss.fff}");
        Console.WriteLine($"    Widget:      {focusEvent.Widget.Name}");
        Console.WriteLine($"    Role:        {focusEvent.Widget.Role}");
        Console.WriteLine($"    Application: {focusEvent.Widget.ApplicationName}");
        Console.WriteLine($"    Path:        {focusEvent.Widget.ObjectPath}");
        Console.WriteLine();
    }

    Console.WriteLine($"\nTotal focus events received: {eventCount}");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Environment.Exit(1);
}
