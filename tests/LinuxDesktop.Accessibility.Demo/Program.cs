using Olbrasoft.LinuxDesktop.Accessibility;

var logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "atspi-focus-log.txt");

Console.WriteLine("=== AT-SPI Focus Monitoring Demo ===\n");
Console.WriteLine($"Logging to: {logFile}");
Console.WriteLine("This demo will run for 60 seconds and log all focus events.");
Console.WriteLine("Try switching focus between different applications:\n");
Console.WriteLine("  - Click in terminal");
Console.WriteLine("  - Click in browser URL bar");
Console.WriteLine("  - Click in text editor");
Console.WriteLine("  - Click buttons in GNOME apps");
Console.WriteLine("\nPress Ctrl+C to exit early.\n");

await File.WriteAllTextAsync(logFile, $"=== AT-SPI Focus Log Started at {DateTime.Now} ===\n\n");

try
{
    await using var service = await AccessibilityService.CreateAsync();
    Console.WriteLine("✓ Service created and connected to AT-SPI bus\n");

    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    // Auto-stop after 60 seconds
    cts.CancelAfter(TimeSpan.FromSeconds(60));

    var eventCount = 0;
    var startTime = DateTime.Now;

    Console.WriteLine("Monitoring focus events...\n");

    await foreach (var focusEvent in service.WatchFocusChangesAsync(cts.Token))
    {
        eventCount++;

        var elapsed = (DateTime.Now - startTime).TotalSeconds;
        var logEntry = $"[{eventCount,3}] {elapsed,6:F1}s - {focusEvent.Widget.Role,-15} | {focusEvent.Widget.Name,-30} | {focusEvent.Widget.ApplicationName}\n";

        // Write to console
        Console.Write(logEntry);

        // Append to log file
        await File.AppendAllTextAsync(logFile, logEntry);
    }

    var summary = $"\n=== Session ended at {DateTime.Now} ===\nTotal events: {eventCount}\nDuration: {(DateTime.Now - startTime).TotalSeconds:F1}s\n";
    Console.WriteLine(summary);
    await File.AppendAllTextAsync(logFile, summary);
}
catch (OperationCanceledException)
{
    Console.WriteLine("\nMonitoring stopped.");
}
catch (Exception ex)
{
    var error = $"\n❌ Error: {ex.Message}\n{ex.StackTrace}\n";
    Console.WriteLine(error);
    await File.AppendAllTextAsync(logFile, error);
    Environment.Exit(1);
}

Console.WriteLine($"\nLog file: {logFile}");
