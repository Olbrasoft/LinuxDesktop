using Olbrasoft.LinuxDesktop.DBus.Exceptions;
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

    // Get details of focused window (use already fetched data)
    var focusedWindow = windows.FirstOrDefault(w => w.HasFocus);
    if (focusedWindow != null)
    {
        Console.WriteLine("3. Focused window details:");
        Console.WriteLine($"   Window ID: {focusedWindow.Id}");

        // Print what we already know from the list
        Console.WriteLine($"   Title: {focusedWindow.Title}");
        Console.WriteLine($"   Class: {focusedWindow.WmClass}");
        Console.WriteLine($"   PID: {focusedWindow.Pid}");
        Console.WriteLine($"   In current workspace: {focusedWindow.InCurrentWorkspace}");

        // Try to get more details
        Console.WriteLine("\n   Fetching extended details...");
        var details = await windowService.GetWindowDetailsAsync(focusedWindow.Id);
        if (details != null)
        {
            Console.WriteLine($"   Position: ({details.X}, {details.Y})");
            Console.WriteLine($"   Size: {details.Width}x{details.Height}");
            Console.WriteLine($"   Monitor: {details.Monitor}");
            Console.WriteLine($"   Can close: {details.CanClose}");
            Console.WriteLine($"   Maximized: {details.Maximized}");
        }
        else
        {
            Console.WriteLine("   (Extended details not available)");
        }

        // Test GetTitle separately
        Console.WriteLine("\n4. Testing GetTitle method...");
        var title = await windowService.GetWindowTitleAsync(focusedWindow.Id);
        Console.WriteLine($"   Title via GetTitle: {title}");

        // Test GetFocusedWindow helper
        Console.WriteLine("\n5. Testing GetFocusedWindow helper...");
        var focused = await windowService.GetFocusedWindowAsync();
        Console.WriteLine($"   Focused window via helper: {focused?.WmClass} - {focused?.Title?.Truncate(40)}");
    }

    // Test with a different window (not focused)
    var otherWindow = windows.FirstOrDefault(w => !w.HasFocus && w.WmClass != null && w.WmClass != "conky");
    if (otherWindow != null)
    {
        Console.WriteLine($"\n6. Testing with non-focused window (ID: {otherWindow.Id})...");
        var otherDetails = await windowService.GetWindowDetailsAsync(otherWindow.Id);
        if (otherDetails != null)
        {
            Console.WriteLine($"   {otherWindow.WmClass}: {otherDetails.Width}x{otherDetails.Height}");
        }
    }

    // Test Workspace methods
    Console.WriteLine("\n7. Testing Workspace methods...");
    var workspaceCount = await windowService.GetWorkspaceCountAsync();
    var activeWorkspace = await windowService.GetActiveWorkspaceAsync();
    Console.WriteLine($"   Workspace count: {workspaceCount}");
    Console.WriteLine($"   Active workspace: {activeWorkspace}");

    Console.WriteLine("\n8. Testing GetWorkspaces...");
    var workspaces = await windowService.GetWorkspacesAsync();
    foreach (var ws in workspaces)
    {
        var activeMarker = ws.IsActive ? " [ACTIVE]" : "";
        Console.WriteLine($"   Workspace {ws.Index}: {ws.WindowCount} windows{activeMarker}");
    }

    Console.WriteLine("\n9. Testing GetWorkspaceWindows (workspace 0)...");
    var ws0Windows = await windowService.GetWorkspaceWindowsAsync(0);
    foreach (var w in ws0Windows.Take(5))
    {
        Console.WriteLine($"   [{w.Id}] {w.WmClass}: {w.Title?.Truncate(40)}");
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
