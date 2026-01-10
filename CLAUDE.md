# LinuxDesktop - Claude Code Notes

## Project Overview

**.NET 10 library for Linux desktop integration** - enables monitoring user context (workspace, active window, application) via D-Bus communication with GNOME Shell.

**Primary use case:** VirtualAssistant context awareness - know where user is on desktop to provide intelligent, context-aware notifications and navigation.

## 📦 Packaging Workflow

**DUAL PUBLISHING** - Packages are automatically created in two locations:

| Location | When | Purpose |
|----------|------|---------|
| **Local** (`~/Olbrasoft/LinuxDesktop/artifacts/`) | After every `git commit` | Development & testing |
| **NuGet.org** | After `git push` to main (GitHub Actions) | Production release |

**Development flow:**
1. Make changes → `git commit` → **local packages ready instantly**
2. Test in VirtualAssistant using local packages
3. When stable → `git push` → **published to NuGet.org**
4. Switch VirtualAssistant to NuGet.org packages

**Version format:**
- Local: `1.0.COMMITS-local` (e.g., 1.0.123-local)
- Production: `1.0.RUN_NUMBER` (e.g., 1.0.5)

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

### Development Workflow (AUTOMATIC)

**IMPORTANT:** When developing LinuxDesktop, local packages are **automatically created** after each commit.

#### Automatic Pack on Commit

A git post-commit hook automatically:
1. ✅ Increments version based on commit count: `1.0.COMMITS-local`
2. ✅ Packs NuGet packages to `./artifacts/`
3. ✅ Shows package info after commit

**You don't need to manually run `dotnet pack`** - it happens automatically!

**Example after commit:**
```
[main abc1234] feat: Add new feature
 1 file changed, 10 insertions(+)
🔨 Post-commit: Packing NuGet packages...
✅ Packages packed successfully to ./artifacts/
📦 Version: 1.0.123-local
-rw-r--r-- 1 user user 15K Jan  4 11:00 Olbrasoft.LinuxDesktop.Core.1.0.123-local.nupkg
-rw-r--r-- 1 user user 18K Jan  4 11:00 Olbrasoft.LinuxDesktop.DBus.1.0.123-local.nupkg
-rw-r--r-- 1 user user 14K Jan  4 11:00 Olbrasoft.LinuxDesktop.Accessibility.1.0.123-local.nupkg
```

#### Package Location

**Local artifacts:** `~/Olbrasoft/LinuxDesktop/artifacts/`

These packages are automatically created after **every commit** and ready to use immediately.

#### Using Local Packages in VirtualAssistant

**1. Add local NuGet source (one-time setup):**
```bash
cd ~/Olbrasoft/VirtualAssistant
dotnet nuget add source ~/Olbrasoft/LinuxDesktop/artifacts --name "LinuxDesktopLocal"
```

**2. Install/update packages:**
```bash
# Find latest version in artifacts
ls ~/Olbrasoft/LinuxDesktop/artifacts/*.nupkg | tail -1

# Install with latest version (e.g., 1.0.123-local)
dotnet add package Olbrasoft.LinuxDesktop.DBus --version 1.0.123-local

# Or use wildcard to always get latest local version
dotnet add package Olbrasoft.LinuxDesktop.DBus --version "1.0.*-local"
```

**3. Update to latest after LinuxDesktop changes:**
```bash
cd ~/Olbrasoft/VirtualAssistant
dotnet nuget locals all --clear
dotnet restore
dotnet build
```

#### Development Cycle

**Workflow:**
1. Make changes to LinuxDesktop code
2. `git add .` and `git commit -m "..."`
3. ✅ **Post-commit hook automatically packs packages**
4. In VirtualAssistant: `dotnet nuget locals all --clear && dotnet restore && dotnet build`
5. Test your changes immediately

**No waiting for NuGet.org!**
- ✅ Instant feedback (packages ready after commit)
- ✅ Test before publishing to production
- ✅ Same workflow as production (NuGet packages)
- ✅ Automatic versioning (commit count)

### Production Phase (After Integration Testing)

When LinuxDesktop changes are **fully tested and integrated** in VirtualAssistant, switch to NuGet.org packages:

**Why switch?**
- ✅ Development complete - no more rapid changes
- ✅ Tested in VirtualAssistant
- ✅ Ready for stable version
- ✅ Other projects can use the same version

**How to switch:**
```bash
cd ~/Olbrasoft/VirtualAssistant

# 1. Remove local source
dotnet nuget remove source LinuxDesktopLocal

# 2. Clear cache
dotnet nuget locals all --clear

# 3. Install from NuGet.org (auto-published via GitHub Actions)
dotnet add package Olbrasoft.LinuxDesktop.DBus --version 1.0.N

# 4. Restore from NuGet.org
dotnet restore
```

**Verify:**
```bash
dotnet restore --verbosity detailed | grep "nuget.org"
# Should show: Installed Olbrasoft.LinuxDesktop.DBus from nuget.org
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

## GNOME Extension Development

### Project Structure

```
gnome-extension/
├── src/
│   ├── extension.ts    # TypeScript source (main extension logic)
│   └── gjs.d.ts        # Type declarations for GJS/GNOME modules
├── dist/
│   └── extension.js    # Generated JavaScript (don't edit directly)
├── metadata.json       # Extension metadata (UUID, version, shell-version)
├── package.json        # npm configuration
├── tsconfig.json       # TypeScript compiler configuration
└── .gitignore          # Excludes node_modules/ and dist/
```

### Build Commands

```bash
cd gnome-extension
npm install        # Install TypeScript and dependencies
npm run build      # Compile TypeScript → JavaScript
npm run watch      # Watch mode - rebuild on file changes
npm run clean      # Remove dist/ directory
```

### TypeScript Configuration

The extension uses TypeScript with specific settings for GJS compatibility:
- **Target:** ES2022 (GJS supports modern JavaScript)
- **Module:** ES2022 (ES modules)
- **Strict mode:** Disabled (GJS type system differs from standard TypeScript)
- **Type declarations:** Custom `gjs.d.ts` for GNOME Shell modules

### CI/CD Pipeline

Two separate GitHub Actions workflows:

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `publish-nuget.yml` | Changes in `src/`, `tests/`, `*.sln` | Build/test .NET, publish NuGet |
| `deploy-extension.yml` | Changes in `gnome-extension/` | Build TypeScript, deploy extension |

**Extension deployment pipeline:**
1. Push to `main` triggers workflow
2. `npm ci` installs dependencies
3. `npm run build` compiles TypeScript
4. Deployment job (self-hosted runner) copies to `~/.local/share/gnome-shell/extensions/`
5. Manual GNOME Shell restart required to load new version

### Manual Deployment

```bash
# Build
cd gnome-extension
npm install
npm run build

# Deploy
TARGET=~/.local/share/gnome-shell/extensions/focus-tracker@olbrasoft.cz
mkdir -p $TARGET
cp dist/extension.js metadata.json $TARGET/

# Reload extension
# X11: Alt+F2 → 'r' → Enter
# Wayland: gnome-extensions disable/enable focus-tracker@olbrasoft.cz
```

### D-Bus Interface

The extension exposes `org.olbrasoft.Desktop` on the session bus:

**Methods:**
- `GetPointerPosition() → (ii)` - Cursor coordinates
- `GetActiveWindowGeometry() → (iiii)` - Window bounds (x, y, w, h)
- `GetWorkspaceApplications(i) → a(sss)` - Apps on workspace

**Signals:**
- `WorkspaceChanged(i, i)` - Workspace switch notification
- `FocusChanged(s, s, s)` - Focus change notification

**Properties:**
- `CurrentWorkspace`, `TotalWorkspaces`, `ActiveWindow`, `ActiveApplication`

### Troubleshooting

**Extension not loading:**
```bash
# Check GNOME Shell logs
journalctl -f /usr/bin/gnome-shell

# Verify extension is recognized
gnome-extensions list | grep focus-tracker

# Check extension errors
gnome-extensions show focus-tracker@olbrasoft.cz
```

**D-Bus not responding:**
```bash
# Test D-Bus interface
dbus-send --session --print-reply \
  --dest=org.olbrasoft.Desktop \
  /org/olbrasoft/Desktop \
  org.olbrasoft.Desktop.GetPointerPosition

# Check if bus name is registered
dbus-send --session --print-reply \
  --dest=org.freedesktop.DBus \
  /org/freedesktop/DBus \
  org.freedesktop.DBus.ListNames | grep olbrasoft
```

**TypeScript compilation errors:**
- Check `gjs.d.ts` for missing type declarations
- GJS uses `gi://` module imports (not standard npm packages)
- Some TypeScript features don't work with GJS runtime

## References

- **Gemini.md** - Analysis of packaging and system dependencies
- **docs/RESEARCH.md** - D-Bus API research for GNOME Shell
- **docs/AT-SPI-RESEARCH.md** - Complete AT-SPI integration analysis
