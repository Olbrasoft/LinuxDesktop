# Desktop State Tracker Extension

GNOME Shell extension that exports desktop workspace, window, and application state via D-Bus.

## Status: Phase 2 - Complete

**Current features:**
- ✅ Track current workspace index
- ✅ Track total workspace count
- ✅ Track active window title
- ✅ Track active application ID
- ✅ Emit `WorkspaceChanged` signal on workspace switch
- ✅ Emit `FocusChanged` signal on window/app focus change
- ✅ D-Bus interface at `org.olbrasoft.Desktop`

## Installation

Extension is installed at:
```
~/.local/share/gnome-shell/extensions/focus-tracker@olbrasoft.cz/
```

## Enable Extension

```bash
gnome-extensions enable focus-tracker@olbrasoft.cz
```

## D-Bus Interface

**Service Name:** `org.olbrasoft.Desktop`
**Object Path:** `/org/olbrasoft/Desktop`

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `CurrentWorkspace` | `i` (int32) | Current workspace index (0-based) |
| `TotalWorkspaces` | `i` (int32) | Total number of workspaces |
| `ActiveWindow` | `s` (string) | Active window title |
| `ActiveApplication` | `s` (string) | Active application ID (e.g., "kitty.desktop") |

### Signals

**WorkspaceChanged(newIndex: int32, totalWorkspaces: int32)**
- Emitted when workspace changes
- `newIndex`: New workspace index
- `totalWorkspaces`: Total number of workspaces

**FocusChanged(windowTitle: string, appId: string, wmClass: string)**
- Emitted when window focus changes
- `windowTitle`: Window title
- `appId`: Application ID (e.g., "firefox.desktop")
- `wmClass`: Window manager class

## Verification

### Check extension status
```bash
gnome-extensions info focus-tracker@olbrasoft.cz
```

### Check D-Bus service
```bash
busctl --user list | grep olbrasoft
# Should show: org.olbrasoft.Desktop

busctl --user introspect org.olbrasoft.Desktop /org/olbrasoft/Desktop
# Shows all properties and signals
```

### Test properties
```bash
# Get active window title
busctl --user get-property org.olbrasoft.Desktop /org/olbrasoft/Desktop org.olbrasoft.Desktop ActiveWindow

# Get active application
busctl --user get-property org.olbrasoft.Desktop /org/olbrasoft/Desktop org.olbrasoft.Desktop ActiveApplication

# Get current workspace
busctl --user get-property org.olbrasoft.Desktop /org/olbrasoft/Desktop org.olbrasoft.Desktop CurrentWorkspace

# Get total workspaces
busctl --user get-property org.olbrasoft.Desktop /org/olbrasoft/Desktop org.olbrasoft.Desktop TotalWorkspaces
```

### Monitor signals
```bash
# Watch all signals
busctl --user monitor org.olbrasoft.Desktop

# Switch windows (Alt+Tab) to trigger FocusChanged
# Switch workspaces (Super+Page Up/Down) to trigger WorkspaceChanged
```

## .NET Integration

### Install Tmds.DBus
```bash
dotnet add package Tmds.DBus
```

### Interface Definition
```csharp
using Tmds.DBus;

public struct WorkspaceChangedArgs
{
    public int NewIndex;
    public int TotalWorkspaces;
}

public struct FocusChangedArgs
{
    public string WindowTitle;
    public string AppId;
    public string WmClass;
}

[DBusInterface("org.olbrasoft.Desktop")]
public interface IDesktopState : IDBusObject
{
    // Generic property access
    Task<T> GetAsync<T>(string propertyName);

    // Signals
    Task<IDisposable> WatchWorkspaceChangedAsync(Action<WorkspaceChangedArgs> handler);
    Task<IDisposable> WatchFocusChangedAsync(Action<FocusChangedArgs> handler);
}
```

### Usage Example
```csharp
using var connection = new Connection(Address.Session!);
await connection.ConnectAsync();

var service = connection.CreateProxy<IDesktopState>(
    "org.olbrasoft.Desktop",
    new ObjectPath("/org/olbrasoft/Desktop")
);

// Read current state using generic GetAsync
var currentWorkspace = await service.GetAsync<int>("CurrentWorkspace");
var totalWorkspaces = await service.GetAsync<int>("TotalWorkspaces");
var activeWindow = await service.GetAsync<string>("ActiveWindow");
var activeApp = await service.GetAsync<string>("ActiveApplication");

Console.WriteLine($"Workspace: {currentWorkspace} / {totalWorkspaces}");
Console.WriteLine($"Window: {activeWindow}");
Console.WriteLine($"App: {activeApp}");

// Subscribe to changes
var focusSubscription = await service.WatchFocusChangedAsync(args => {
    Console.WriteLine($"Focus: {args.WindowTitle} ({args.AppId})");
});

var workspaceSubscription = await service.WatchWorkspaceChangedAsync(args => {
    Console.WriteLine($"Workspace: {args.NewIndex} / {args.TotalWorkspaces}");
});

// Later: cleanup
focusSubscription.Dispose();
workspaceSubscription.Dispose();
```

### Test Phase 2
```bash
cd ~/.local/share/gnome-shell/extensions/focus-tracker@olbrasoft.cz/test/DesktopStateTest
dotnet run
```

The test will:
- Read all Phase 1 & 2 properties
- Subscribe to WorkspaceChanged and FocusChanged signals
- Monitor for 30 seconds
- Display any workspace switches or window focus changes

## Troubleshooting

### Extension not enabled
```bash
gnome-extensions enable focus-tracker@olbrasoft.cz
```

### D-Bus service not available
```bash
# Check if extension is running
gnome-extensions info focus-tracker@olbrasoft.cz

# Check logs
journalctl -f /usr/bin/gnome-shell | grep DesktopState
```

### Properties return empty values
This is normal when:
- No window is focused (ActiveWindow, ActiveApplication return empty strings)
- GNOME Shell is starting up

## Development

### View logs
```bash
journalctl -f /usr/bin/gnome-shell | grep DesktopState
```

### Reload extension
```bash
gnome-extensions disable focus-tracker@olbrasoft.cz
gnome-extensions enable focus-tracker@olbrasoft.cz
```

## License

GPL-2.0-or-later (standard for GNOME Shell extensions)
