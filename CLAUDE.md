# LinuxDesktop - Claude Code Notes

## Project Overview

**.NET 10 library for Linux desktop integration** - enables monitoring user context (workspace, active window, application) via D-Bus communication with GNOME Shell.

**Primary use case:** VirtualAssistant context awareness - know where user is on desktop to provide intelligent, context-aware notifications and navigation.

## Architecture

**Clean Architecture with ISP (Interface Segregation Principle)**

```
LinuxDesktop.Core/              # Interfaces & models (abstractions)
    ↓ depends on
LinuxDesktop.DBus/              # D-Bus implementation
LinuxDesktop.Accessibility/     # AT-SPI focus tracking (experimental)
```

**Services:**
- `IWindowService` - Window management (list, focus, activate, move, resize)
- `IWorkspaceService` - Workspace management (count, active, switch)
- `IIdleService` - User activity tracking (idle time)
- `IAccessibilityService` - AT-SPI real-time focus tracking (captures focus changes inside apps)

## Key Functionality

### 1. Window Tracking
```csharp
await using var windowService = await WindowService.CreateAsync();
var windows = await windowService.GetWindowsAsync();
var focused = await windowService.GetFocusedWindowAsync();
// Returns: WindowInfo { Id, Title, WmClass, HasFocus, InCurrentWorkspace, Pid }
```

### 2. Workspace Awareness
```csharp
await using var workspaceService = await WorkspaceService.CreateAsync();
var current = await workspaceService.GetActiveWorkspaceAsync(); // 0-indexed
var total = await workspaceService.GetWorkspaceCountAsync();
```

### 3. Idle Detection
```csharp
await using var idleService = await IdleMonitorService.CreateAsync();
var idle = await idleService.GetIdleTimeSpanAsync();
```

### 4. Real-time Focus Tracking (AT-SPI)
```csharp
await using var a11y = await AccessibilityService.CreateAsync();
await foreach (var evt in a11y.WatchFocusChangesAsync())
{
    // Captures focus changes INSIDE apps (e.g., browser tab switches)
    Console.WriteLine($"{evt.Widget.Name} ({evt.Widget.Role})");
}
```

## System Dependencies (CRITICAL!)

### GNOME Extensions (REQUIRED)
1. **window-calls@domandoman.xyz** - D-Bus interface for Window/Workspace APIs
2. **focus-tracker@olbrasoft.cz** - Custom extension, emits WorkspaceChanged/FocusChanged signals

### D-Bus Services
- Session bus: `org.gnome.Shell`, `org.gnome.Mutter.IdleMonitor`
- Accessibility bus: `org.a11y.atspi.Registry`

### Platform Requirements
- Linux with D-Bus
- GNOME Shell 48+
- .NET 10 Runtime
- `at-spi2-registryd` daemon (for Accessibility)

## Package Usage

### Development Phase (Current)

**Use local NuGet packages** for rapid iteration:

#### 1. Pack Packages Locally

```bash
cd ~/Olbrasoft/LinuxDesktop
dotnet pack -c Release -o ./artifacts
```

**Output:** `~/Olbrasoft/LinuxDesktop/artifacts/*.nupkg`

#### 2. Add Local Source (in VirtualAssistant)

```bash
cd ~/Olbrasoft/VirtualAssistant
dotnet nuget add source ~/Olbrasoft/LinuxDesktop/artifacts --name "LinuxDesktopLocal"
```

#### 3. Install Local Packages

```bash
# Use exact version
dotnet add package Olbrasoft.LinuxDesktop.Core --version 1.0.0
dotnet add package Olbrasoft.LinuxDesktop.DBus --version 1.0.0
```

#### 4. Iterate Quickly

After changes to LinuxDesktop:
```bash
# Re-pack
cd ~/Olbrasoft/LinuxDesktop
dotnet pack -c Release -o ./artifacts

# Update VirtualAssistant
cd ~/Olbrasoft/VirtualAssistant
dotnet nuget locals all --clear
dotnet restore
dotnet build
```

**Why local packages?**
- ✅ Instant feedback (no 5-15 min NuGet.org delay)
- ✅ Test before publishing
- ✅ Same workflow as production (NuGet packages)

### Production Phase (Future)

After packages published to NuGet.org:

```bash
# Remove local source
dotnet nuget remove source LinuxDesktopLocal

# Install from NuGet.org
dotnet add package Olbrasoft.LinuxDesktop.DBus
```

### Alternative: Project Reference

If not using NuGet workflow:
```xml
<ProjectReference Include="../LinuxDesktop/src/LinuxDesktop.Core/LinuxDesktop.Core.csproj" />
<ProjectReference Include="../LinuxDesktop/src/LinuxDesktop.DBus/LinuxDesktop.DBus.csproj" />
```

## Running Services

```bash
# gRPC monitor (port 5054)
systemctl --user status desktop-monitor-grpc.service

# Web dashboard (port 5051)
systemctl --user status desktop-monitor-web.service
```

## Build & Test

```bash
cd ~/Olbrasoft/LinuxDesktop
dotnet build
dotnet test  # 21+ automated tests (xUnit + Moq)
```

## Local Artifacts

**Package location:** `~/Olbrasoft/LinuxDesktop/artifacts/`

After `dotnet pack`, this directory contains:
- `Olbrasoft.LinuxDesktop.Core.{version}.nupkg`
- `Olbrasoft.LinuxDesktop.DBus.{version}.nupkg`
- `Olbrasoft.LinuxDesktop.Accessibility.{version}.nupkg`

**Important:** This directory is in `.gitignore` - packages not committed to Git.

## VirtualAssistant Integration

### Setup in VirtualAssistant

**1. Add local NuGet source:**
```bash
cd ~/Olbrasoft/VirtualAssistant
dotnet nuget add source ~/Olbrasoft/LinuxDesktop/artifacts --name "LinuxDesktopLocal"
```

**2. Install packages:**
```bash
dotnet add package Olbrasoft.LinuxDesktop.DBus --version 1.0.0
```

**3. Register services in `Program.cs`:**
```csharp
// Add LinuxDesktop services
builder.Services.AddSingleton<IWindowService>(sp => WindowService.CreateAsync().Result);
builder.Services.AddSingleton<IWorkspaceService>(sp => WorkspaceService.CreateAsync().Result);
```

### Usage Example: Context-Aware Notifications

```csharp
// Inject service
private readonly IWindowService _windowService;

public async Task NotifyTaskCompleted(string appName, string message)
{
    var focused = await _windowService.GetFocusedWindowAsync();

    if (focused.WmClass == appName)
    {
        // User already in the app - don't interrupt
        _logger.LogInformation("User already in {App}, skipping notification", appName);
        return;
    }

    // User is elsewhere - offer to switch
    await _tts.SpeakAsync($"{message}. Přepnout tě tam?");

    if (await _voice.WaitForConfirmation())
    {
        var windows = await _windowService.GetWindowsAsync();
        var targetWindow = windows.FirstOrDefault(w => w.WmClass == appName);

        if (targetWindow != null)
        {
            await _windowService.ActivateWindowAsync(targetWindow.Id);
            _logger.LogInformation("Switched to {App}", appName);
        }
    }
}
```

## Design Patterns Used

- **Repository Pattern** - Services return immutable DTOs (records)
- **Factory Pattern** - `CreateAsync()` for async initialization
- **Template Method** - `DBusServiceBase` provides common D-Bus logic
- **Observer Pattern** - AT-SPI events via `IAsyncEnumerable<T>`
- **Adapter Pattern** - D-Bus DTO → Core Models mapping

## Future Plans

- Publish as NuGet package (needs `.csproj` metadata)
- Stabilize AT-SPI integration (GLib MainLoop complexity)
- Cross-desktop support (KDE Plasma?)

## Limitations

⚠️ **Platform-specific:**
- Linux + GNOME only
- Requires custom GNOME extensions
- Not cross-platform

⚠️ **Development status:**
- NOT production-ready NuGet
- Accessibility part is experimental
- API may change

## References

- **Gemini.md** - Analysis of packaging and system dependencies
- **docs/RESEARCH.md** - D-Bus API research for GNOME Shell
- **docs/AT-SPI-RESEARCH.md** - Complete AT-SPI integration analysis
