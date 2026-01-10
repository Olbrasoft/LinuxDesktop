# LinuxDesktop

[![NuGet - Core](https://img.shields.io/nuget/v/Olbrasoft.LinuxDesktop.Core.svg)](https://www.nuget.org/packages/Olbrasoft.LinuxDesktop.Core/)
[![NuGet - DBus](https://img.shields.io/nuget/v/Olbrasoft.LinuxDesktop.DBus.svg)](https://www.nuget.org/packages/Olbrasoft.LinuxDesktop.DBus/)
[![NuGet - Accessibility](https://img.shields.io/nuget/v/Olbrasoft.LinuxDesktop.Accessibility.svg)](https://www.nuget.org/packages/Olbrasoft.LinuxDesktop.Accessibility/)

.NET library for Linux desktop integration using D-Bus (Tmds.DBus).

## Why This Library Exists

This library was created to enable **intelligent interaction between VirtualAssistant and the Linux desktop environment**.

### The Problem

VirtualAssistant (voice-controlled assistant) needs to be context-aware:
- **Where is the user?** Which workspace, which application has focus?
- **Should I speak now?** Don't announce "Claude Code finished" if user is already looking at Claude Code
- **Can I help navigate?** Switch user to relevant workspace/app when work is completed elsewhere

### The Solution

LinuxDesktop provides APIs for:

1. **Context Awareness**
   - Detect active window and application
   - Know which workspace user is on
   - Monitor focus changes in real-time

2. **Intelligent Notifications**
   - VirtualAssistant can decide WHEN to notify based on user's current context
   - Example: Don't say "OpenCode finished task" if user is already in OpenCode workspace

3. **Voice-Controlled Navigation**
   - Switch workspaces programmatically
   - Bring applications to focus
   - Example flow:
     ```
     VirtualAssistant: "Claude Code dokončil práci na issue 42. Chceš se tam přepnout?"
     User: "Ano"
     VirtualAssistant: [switches to Claude Code workspace automatically]
     ```

### Real-World Scenario

```
User is working in OpenCode (workspace 1)
Claude Code finishes a task in workspace 2

WITHOUT LinuxDesktop:
  VirtualAssistant: "Claude Code dokončil úkol" 
  User: [must manually switch workspace, find the window]

WITH LinuxDesktop:
  VirtualAssistant detects: user is in OpenCode, not Claude Code
  VirtualAssistant: "Claude Code dokončil práci. Přepnout tě tam?"
  User: "Ano"
  LinuxDesktop: [switches to workspace 2, focuses Claude Code window]
  User: [immediately sees the completed work, no keyboard interaction needed]
```

## Features

### Current (Planned for Phase 1)
- **Window Detection** - Get active window title and application
- **Workspace Info** - Current workspace, workspace count
- **Focus Monitoring** - Real-time notifications when focus changes

### Future Phases
- **Workspace Switching** - Programmatically switch workspaces
- **Window Focus** - Bring specific window to front
- **Power Management** - Sleep, shutdown, screen lock detection
- **System Notifications** - Desktop notifications via D-Bus

## Architecture

Uses [Tmds.DBus](https://github.com/tmds/Tmds.DBus) for D-Bus communication with Linux desktop services.

### D-Bus Services Used

| Service | Bus | Purpose |
|---------|-----|---------|
| `org.gnome.Shell` | session | Window/workspace info, switching |
| `org.gnome.SessionManager` | session | Session state |
| `org.freedesktop.login1` | system | Power management |
| `org.freedesktop.Notifications` | session | Desktop notifications |

## Project Structure

```
LinuxDesktop/
├── src/
│   ├── LinuxDesktop.Core/           # Interfaces, models
│   ├── LinuxDesktop.DBus/           # D-Bus implementations
│   └── LinuxDesktop.Gnome/          # GNOME-specific features
├── tests/
│   ├── LinuxDesktop.Core.Tests/
│   └── LinuxDesktop.DBus.Tests/
├── LinuxDesktop.sln
└── README.md
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Linux with D-Bus (GNOME, KDE, etc.)

### Installation

```bash
git clone https://github.com/Olbrasoft/LinuxDesktop.git
cd LinuxDesktop
dotnet build
```

### Running Tests

```bash
dotnet test
```

### NuGet Installation

Install via NuGet (recommended):

```bash
# DBus - D-Bus implementation (most common, includes Core as dependency)
dotnet add package Olbrasoft.LinuxDesktop.DBus

# Core - Interfaces and models only (if you only need abstractions)
dotnet add package Olbrasoft.LinuxDesktop.Core

# Accessibility - AT-SPI support (optional, experimental)
dotnet add package Olbrasoft.LinuxDesktop.Accessibility
```

**Package dependencies:**
- `LinuxDesktop.DBus` → requires `LinuxDesktop.Core` (automatic)
- `LinuxDesktop.Accessibility` → standalone (optional)

**For most use cases**, you only need:
```bash
dotnet add package Olbrasoft.LinuxDesktop.DBus
```
(This automatically includes `LinuxDesktop.Core` as a transitive dependency)

## Integration with VirtualAssistant

This library is consumed by [VirtualAssistant](https://github.com/Olbrasoft/VirtualAssistant) via NuGet packages:

**Current approach:** ✅ NuGet packages (stable, published to NuGet.org)

~~**Previous approach:** Project references (deprecated, used during development)~~

```csharp
// Example usage in VirtualAssistant
var windowService = serviceProvider.GetRequiredService<IWindowService>();

// Check if user is already looking at the relevant app
var activeWindow = await windowService.GetActiveWindowAsync();
if (activeWindow.Application != "claude-code")
{
    // User is elsewhere, offer to switch
    await tts.SpeakAsync("Claude Code dokončil práci. Přepnout tě tam?");
    
    if (await WaitForUserConfirmation())
    {
        await windowService.FocusWindowAsync("claude-code");
    }
}
```

## GNOME Shell Extension

This library includes a custom GNOME Shell extension (`focus-tracker@olbrasoft.cz`) that provides D-Bus APIs for desktop context awareness.

### D-Bus Interface

**Bus name:** `org.olbrasoft.Desktop`
**Object path:** `/org/olbrasoft/Desktop`

| Method | Signature | Description |
|--------|-----------|-------------|
| `GetPointerPosition` | `() → (ii)` | Returns cursor (x, y) coordinates |
| `GetActiveWindowGeometry` | `() → (iiii)` | Returns window (x, y, width, height) |
| `GetWorkspaceApplications` | `(i) → a(sss)` | Returns apps on workspace (appId, title, wmClass) |

| Signal | Args | Description |
|--------|------|-------------|
| `WorkspaceChanged` | `(i, i)` | Emitted on workspace switch (newIndex, total) |
| `FocusChanged` | `(s, s, s)` | Emitted on focus change (title, appId, wmClass) |

| Property | Type | Description |
|----------|------|-------------|
| `CurrentWorkspace` | `i` | Active workspace index (0-based) |
| `TotalWorkspaces` | `i` | Number of workspaces |
| `ActiveWindow` | `s` | Focused window title |
| `ActiveApplication` | `s` | Focused application ID |

### Installation

**Automated (via CI/CD):**

Extension is automatically deployed to `~/.local/share/gnome-shell/extensions/` after push to `main` branch.

**Manual installation:**

```bash
cd gnome-extension
npm install
npm run build
mkdir -p ~/.local/share/gnome-shell/extensions/focus-tracker@olbrasoft.cz
cp dist/extension.js metadata.json ~/.local/share/gnome-shell/extensions/focus-tracker@olbrasoft.cz/
```

### Development

```bash
cd gnome-extension
npm install        # Install dependencies
npm run build      # Build TypeScript → JavaScript
npm run watch      # Rebuild on changes (development)
npm run clean      # Remove dist/
```

### Extension Reload

After deployment, reload the extension:

- **X11:** Alt+F2 → type `r` → Enter
- **Wayland:** `gnome-extensions disable focus-tracker@olbrasoft.cz && gnome-extensions enable focus-tracker@olbrasoft.cz`

Or log out and back in.

### Verify Extension

```bash
# Check extension is enabled
gnome-extensions list --enabled | grep focus-tracker

# Test D-Bus interface
dbus-send --session --print-reply \
  --dest=org.olbrasoft.Desktop \
  /org/olbrasoft/Desktop \
  org.olbrasoft.Desktop.GetPointerPosition
```

## Related Projects

- [VirtualAssistant](https://github.com/Olbrasoft/VirtualAssistant) - Primary consumer of this library
- [SpeechToText](https://github.com/Olbrasoft/SpeechToText) - Uses similar D-Bus tray icon approach
- [Tmds.DBus](https://github.com/tmds/Tmds.DBus) - D-Bus library this project builds on

## License

MIT License - see [LICENSE](LICENSE) file.
