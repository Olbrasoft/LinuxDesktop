# D-Bus Research for LinuxDesktop

## Overview

This document summarizes the D-Bus API research for GNOME desktop integration.
Research conducted on: Debian 13 (Trixie), GNOME Shell 48.4

## Key Findings

### Available D-Bus Services

| Service | Purpose | Usability |
|---------|---------|-----------|
| `org.gnome.Shell.Extensions.Windows` | **Full window management** | BEST |
| `org.gnome.Mutter.IdleMonitor` | User activity monitoring | WORKS |
| `org.gnome.Shell.Introspect` | Built-in window info | LIMITED (access denied) |
| `org.gnome.Shell` | Main shell interface | LIMITED (Eval disabled) |
| `org.gnome.Mutter.DisplayConfig` | Display configuration | WORKS |

### Recommended Primary API

**`org.gnome.Shell.Extensions.Windows`** is the best option because:
- Full access to window list
- Complete window manipulation
- JSON response format
- No access restrictions

**Note:** This API requires the "Window Calls" GNOME extension (`window-calls@domandoman.xyz`).

## API Details

### org.gnome.Shell.Extensions.Windows

**Service:** `org.gnome.Shell`
**Path:** `/org/gnome/Shell/Extensions/Windows`
**Interface:** `org.gnome.Shell.Extensions.Windows`

#### Methods

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `List()` | - | JSON string | Get all windows |
| `Details(winid)` | uint32 | JSON string | Get detailed window info |
| `GetTitle(winid)` | uint32 | string | Get window title |
| `GetFrameRect(winid)` | uint32 | string | Get position/size |
| `MoveToWorkspace(winid, workspaceNum)` | uint32, uint32 | - | Move to workspace |
| `Activate(winid)` | uint32 | - | Focus window |
| `Maximize(winid)` | uint32 | - | Maximize window |
| `Minimize(winid)` | uint32 | - | Minimize window |
| `Unmaximize(winid)` | uint32 | - | Restore from maximized |
| `Unminimize(winid)` | uint32 | - | Restore from minimized |
| `Close(winid)` | uint32 | - | Close window |
| `Move(winid, x, y)` | uint32, int32, int32 | - | Move window |
| `Resize(winid, width, height)` | uint32, uint32, uint32 | - | Resize window |
| `MoveResize(winid, x, y, w, h)` | uint32, int32, int32, uint32, uint32 | - | Move and resize |

#### Window List Response (JSON)

```json
[
  {
    "in_current_workspace": true,
    "wm_class": "google-chrome",
    "wm_class_instance": "google-chrome",
    "title": "GitHub - Google Chrome",
    "pid": 7606,
    "id": 1176635767,
    "frame_type": 0,
    "window_type": 0,
    "focus": true
  }
]
```

#### Window Details Response (JSON)

```json
{
  "in_current_workspace": true,
  "wm_class": "google-chrome",
  "wm_class_instance": "google-chrome",
  "pid": 7606,
  "id": 1176635767,
  "maximized": 0,
  "frame_type": 0,
  "window_type": 0,
  "layer": 2,
  "monitor": 0,
  "role": null,
  "title": "GitHub - Google Chrome",
  "canclose": true,
  "canmaximize": true,
  "canminimize": true,
  "focus": true,
  "moveable": true,
  "resizeable": true,
  "x": 0,
  "y": 32,
  "width": 2560,
  "height": 1408
}
```

### org.gnome.Mutter.IdleMonitor

**Service:** `org.gnome.Mutter.IdleMonitor`
**Path:** `/org/gnome/Mutter/IdleMonitor/Core`
**Interface:** `org.gnome.Mutter.IdleMonitor`

#### Methods

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `GetIdletime()` | - | uint64 | Milliseconds since last input |
| `AddIdleWatch(interval)` | uint64 | uint32 | Watch for idle (returns watch ID) |
| `AddUserActiveWatch()` | - | uint32 | Watch for user becoming active |
| `RemoveWatch(id)` | uint32 | - | Remove a watch |
| `ResetIdletime()` | - | - | Reset idle counter |

#### Signals

- `WatchFired(uint32 id)` - Emitted when a watch triggers

### org.gnome.Shell.Introspect (Limited)

**Note:** Most methods require special permissions and return "AccessDenied".

#### Methods

| Method | Status |
|--------|--------|
| `GetWindows()` | ACCESS DENIED |
| `GetRunningApplications()` | ACCESS DENIED |

#### Signals (may work for monitoring)

- `WindowsChanged()` - Emitted when windows change
- `RunningApplicationsChanged()` - Emitted when apps change

#### Properties

| Property | Type | Value |
|----------|------|-------|
| `AnimationsEnabled` | bool | true |
| `ScreenSize` | (int, int) | (2560, 1440) |
| `version` | uint | 3 |

### org.gnome.Shell (Main)

**Path:** `/org/gnome/Shell`

#### Methods

| Method | Status | Description |
|--------|--------|-------------|
| `Eval(script)` | DISABLED | JavaScript execution (security) |
| `FocusSearch()` | WORKS | Opens search |
| `ShowApplications()` | WORKS | Shows app grid |
| `FocusApp(id)` | WORKS | Focus an application |

#### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Mode` | string | readonly | "user" |
| `OverviewActive` | bool | readwrite | Control overview |
| `ShellVersion` | string | readonly | "48.4" |

## Key Questions Answered

| Question | Answer |
|----------|--------|
| Can we get active window title? | YES - via Extensions.Windows.List() (check focus:true) |
| Can we get window application name? | YES - wm_class field |
| Can we get window list? | YES - Extensions.Windows.List() |
| Can we get current workspace? | PARTIAL - in_current_workspace per window |
| Can we switch workspace? | YES - MoveToWorkspace() moves windows |
| Can we monitor focus changes? | YES - poll List() or use Introspect signals |
| What properties does window have? | Many: title, wm_class, pid, x/y/width/height, focus, maximized, etc. |
| Can we get idle time? | YES - IdleMonitor.GetIdletime() |

## Limitations

1. **Eval disabled**: Cannot run arbitrary JavaScript in GNOME Shell
2. **Introspect restricted**: Built-in GetWindows() requires special permissions
3. **Extension dependency**: Best API requires "Window Calls" extension installed
4. **No workspace switching**: Can move windows, but no direct workspace switch command

## Recommended Architecture

```
LinuxDesktop.Core
  - IWindowService (interface)
  - IIdleService (interface)
  - WindowInfo, WindowDetails DTOs

LinuxDesktop.DBus
  - WindowCallsService (implements IWindowService via D-Bus)
  - IdleMonitorService (implements IIdleService via D-Bus)
  - Generated Tmds.DBus interfaces
```

## Test Commands

```bash
# List all windows
gdbus call --session --dest org.gnome.Shell \
  --object-path /org/gnome/Shell/Extensions/Windows \
  --method org.gnome.Shell.Extensions.Windows.List

# Get window details
gdbus call --session --dest org.gnome.Shell \
  --object-path /org/gnome/Shell/Extensions/Windows \
  --method org.gnome.Shell.Extensions.Windows.Details <window_id>

# Get idle time (milliseconds)
gdbus call --session --dest org.gnome.Mutter.IdleMonitor \
  --object-path /org/gnome/Mutter/IdleMonitor/Core \
  --method org.gnome.Mutter.IdleMonitor.GetIdletime

# Activate (focus) a window
gdbus call --session --dest org.gnome.Shell \
  --object-path /org/gnome/Shell/Extensions/Windows \
  --method org.gnome.Shell.Extensions.Windows.Activate <window_id>
```

## Required GNOME Extension

**Window Calls** by domandoman
UUID: `window-calls@domandoman.xyz`

Install via: https://extensions.gnome.org/extension/4724/window-calls/

Without this extension, the `org.gnome.Shell.Extensions.Windows` interface will not be available.
