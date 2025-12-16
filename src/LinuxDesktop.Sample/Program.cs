using Olbrasoft.LinuxDesktop.DBus.Services;

Console.WriteLine("LinuxDesktop D-Bus Demo");
Console.WriteLine("========================\n");

try
{
    // Test Idle Monitor
    Console.WriteLine("1. Testing IdleMonitor...");
    await using var idleService = await IdleMonitorService.CreateAsync();
    var idleTime = await idleService.GetIdleTimeSpanAsync();
    Console.WriteLine($"   User idle for: {idleTime.TotalSeconds:F1} seconds");
    Console.WriteLine($"   Idle > 5 min: {await idleService.IsIdleForAsync(TimeSpan.FromMinutes(5))}");
    Console.WriteLine();

    // Test Window Calls
    Console.WriteLine("2. Testing Window Calls extension...");
    await using var windowService = await WindowCallsService.CreateAsync();

    var windows = await windowService.GetWindowsAsync();
    Console.WriteLine($"   Found {windows.Count} windows:\n");

    foreach (var window in windows.Take(10))
    {
        var focusMarker = window.HasFocus ? " [FOCUSED]" : "";
        var workspaceMarker = window.InCurrentWorkspace ? "" : " (other workspace)";
        Console.WriteLine($"   [{window.Id}] {window.WmClass ?? "unknown"}: {window.Title?.Truncate(50)}{focusMarker}{workspaceMarker}");
    }

    if (windows.Count > 10)
        Console.WriteLine($"   ... and {windows.Count - 10} more");

    Console.WriteLine();

    // Get details of focused window
    var focusedWindow = await windowService.GetFocusedWindowAsync();
    if (focusedWindow != null)
    {
        Console.WriteLine("3. Focused window details:");
        var details = await windowService.GetWindowDetailsAsync(focusedWindow.Id);
        if (details != null)
        {
            Console.WriteLine($"   Title: {details.Title}");
            Console.WriteLine($"   Class: {details.WmClass}");
            Console.WriteLine($"   PID: {details.Pid}");
            Console.WriteLine($"   Position: ({details.X}, {details.Y})");
            Console.WriteLine($"   Size: {details.Width}x{details.Height}");
            Console.WriteLine($"   Monitor: {details.Monitor}");
            Console.WriteLine($"   Can close: {details.CanClose}");
            Console.WriteLine($"   Maximized: {details.Maximized}");
        }
    }

    Console.WriteLine("\nDemo completed successfully!");
}
catch (DBusException ex)
{
    Console.WriteLine($"\nD-Bus Error: {ex.Message}");
    if (ex.Message.Contains("Extensions.Windows"))
    {
        Console.WriteLine("\nNote: The 'Window Calls' GNOME extension may not be installed.");
        Console.WriteLine("Install from: https://extensions.gnome.org/extension/4724/window-calls/");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\nError: {ex.Message}");
}

// Extension method for string truncation
public static class StringExtensions
{
    public static string? Truncate(this string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}
