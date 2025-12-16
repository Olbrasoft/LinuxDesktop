# LinuxDesktop

.NET library for Linux desktop integration using D-Bus (Tmds.DBus).

## Features (Planned)

- **Window Management** - Detect active window, workspace changes
- **Focus Detection** - Know which application has focus
- **System Notifications** - Desktop notifications via D-Bus
- **Power Management** - Sleep, shutdown, screen lock detection
- **Tray Icons** - StatusNotifierItem support

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

## Architecture

Uses [Tmds.DBus](https://github.com/tmds/Tmds.DBus) for D-Bus communication.

### D-Bus Services Used

| Service | Purpose |
|---------|--------|
| `org.gnome.Shell` | GNOME window/workspace info |
| `org.freedesktop.login1` | Session/power management |
| `org.freedesktop.Notifications` | Desktop notifications |
| `org.kde.StatusNotifierWatcher` | Tray icons |

## Project Structure

```
LinuxDesktop/
├── src/
│   ├── LinuxDesktop.Core/           # Core interfaces and models
│   ├── LinuxDesktop.DBus/           # D-Bus implementations
│   └── LinuxDesktop.Gnome/          # GNOME-specific features
├── tests/
│   ├── LinuxDesktop.Core.Tests/
│   └── LinuxDesktop.DBus.Tests/
├── LinuxDesktop.sln
└── README.md
```

## Related Projects

- [VirtualAssistant](https://github.com/Olbrasoft/VirtualAssistant) - Will consume this library
- [SpeechToText](https://github.com/Olbrasoft/SpeechToText) - Uses similar tray icon approach

## License

MIT License - see [LICENSE](LICENSE) file.
