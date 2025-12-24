# Desktop State Monitor Web

Real-time web dashboard for monitoring GNOME desktop state via D-Bus.

## Overview

This ASP.NET Core web application connects to the `focus-tracker@olbrasoft.cz` GNOME Shell extension and provides real-time monitoring of:
- **Workspace changes** - tracks when you switch between workspaces
- **Window focus changes** - monitors active window and application
- **Desktop state** - displays current workspace, total workspaces, active window title, and active application

## Architecture

```
GNOME Shell Extension (focus-tracker@olbrasoft.cz)
    ↓ D-Bus (org.olbrasoft.Desktop)
DesktopStateMonitorService (BackgroundService)
    ↓ SignalR Hub
Web Browser (Real-time Dashboard)
```

## Features

- ✅ Real-time workspace tracking
- ✅ Real-time window/application focus tracking
- ✅ Live log display with color-coded messages
- ✅ Network accessible - view from any device on same network
- ✅ Auto-reconnect on connection loss
- ✅ Dark mode UI optimized for readability

## Requirements

### System Requirements

- **GNOME Shell** (tested on 48.4)
- **D-Bus session bus**
- **.NET 10.0 SDK**
- **Linux** (Debian 13 Trixie or compatible)

### GNOME Shell Extension

The web dashboard requires `focus-tracker@olbrasoft.cz` extension to be installed and enabled.

**Installation:**
```bash
# Extension files should be at:
~/.local/share/gnome-shell/extensions/focus-tracker@olbrasoft.cz/

# Enable extension
gnome-extensions enable focus-tracker@olbrasoft.cz

# Verify extension is running
gnome-extensions info focus-tracker@olbrasoft.cz

# Verify D-Bus service is available
busctl --user list | grep olbrasoft
# Should show: org.olbrasoft.Desktop
```

For detailed extension documentation, see:
```
~/.local/share/gnome-shell/extensions/focus-tracker@olbrasoft.cz/README.md
```

### NuGet Packages

The following packages are automatically restored during build:

- **Tmds.DBus** 0.22.0 - D-Bus client library for .NET
- **Microsoft.AspNetCore.SignalR** (built-in with ASP.NET Core) - Real-time WebSocket communication

**Project references:**
- **LinuxDesktop.DBus** - Shared D-Bus abstractions

No manual package installation required - `dotnet restore` handles everything.

## Installation

```bash
cd ~/Olbrasoft/LinuxDesktop
dotnet build src/LinuxDesktop.Monitor.Web/LinuxDesktop.Monitor.Web.csproj
```

## Usage

### Development

```bash
cd ~/Olbrasoft/LinuxDesktop/src/LinuxDesktop.Monitor.Web
dotnet run
```

The server will start on **port 5051** and listen on all network interfaces.

### Production (systemd service)

The application runs as a systemd user service.

**Service management:**
```bash
# Check status
systemctl --user status desktop-monitor-web.service

# Start service
systemctl --user start desktop-monitor-web.service

# Stop service
systemctl --user stop desktop-monitor-web.service

# Restart service
systemctl --user restart desktop-monitor-web.service

# View logs
journalctl --user -u desktop-monitor-web.service -f

# View recent logs
journalctl --user -u desktop-monitor-web.service -n 50
```

**Deployment path:**
- Binaries: `/opt/olbrasoft/desktop-monitor-web/app/`
- Service file: `~/.config/systemd/user/desktop-monitor-web.service`

**Manual deployment:**
```bash
# Publish to deployment directory
cd ~/Olbrasoft/LinuxDesktop
dotnet publish src/LinuxDesktop.Monitor.Web/LinuxDesktop.Monitor.Web.csproj \
  -c Release -o /opt/olbrasoft/desktop-monitor-web/app --no-self-contained

# Restart service
systemctl --user restart desktop-monitor-web.service
```

### Access the dashboard

**Local access:**
```
http://localhost:5051
```

**Network access (from other devices):**
```
http://192.168.0.182:5051
```

Replace `192.168.0.182` with your actual IP address (check with `hostname -I`).

## Configuration

### Change port

Edit `Program.cs`:
```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5051); // Change port here
});
```

### Logging

Edit `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Olbrasoft.LinuxDesktop.Monitor.Web": "Debug"
    }
  }
}
```

## Project Structure

```
LinuxDesktop.Monitor.Web/
├── Hubs/
│   └── DesktopStateHub.cs       # SignalR hub for real-time events
├── Services/
│   └── DesktopStateMonitorService.cs  # D-Bus listener background service
├── wwwroot/
│   └── index.html               # Web dashboard UI
├── Program.cs                   # Application configuration
└── appsettings.json            # Configuration
```

## How It Works

1. **DesktopStateMonitorService** (BackgroundService):
   - Connects to D-Bus session bus
   - Creates proxy to `org.olbrasoft.Desktop`
   - Subscribes to `WorkspaceChanged` and `FocusChanged` signals
   - Forwards events to SignalR hub

2. **DesktopStateHub** (SignalR):
   - Broadcasts events to all connected web clients
   - Provides real-time communication

3. **Web UI** (index.html):
   - Connects via SignalR
   - Displays live stats (workspace, window, app)
   - Shows scrollable log of all events
   - Auto-reconnects on disconnection

## Troubleshooting

### Application won't start

**Error: "Address already in use"**
```
Port 5051 is occupied. Check:
ss -tulpn | grep :5051

Then change port in Program.cs
```

### No events showing

1. Check extension is enabled:
   ```bash
   gnome-extensions list | grep focus-tracker
   ```

2. Check D-Bus service is running:
   ```bash
   busctl --user list | grep olbrasoft
   ```

3. Check application logs:
   ```bash
   # In the terminal where dotnet run is running
   ```

### Can't connect from other devices

1. Check firewall:
   ```bash
   sudo ufw status
   sudo ufw allow 5051/tcp
   ```

2. Verify IP address:
   ```bash
   hostname -I
   ```

3. Ensure both devices are on same network

## Development

### Dependencies

- **Microsoft.AspNetCore.SignalR** - Real-time communication (built-in with ASP.NET Core)
- **Tmds.DBus** - D-Bus protocol for .NET
- **LinuxDesktop.DBus** - Project reference (D-Bus abstractions)

### Adding new events

1. Add signal handler in `DesktopStateMonitorService.cs`
2. Add broadcast method in `DesktopStateHub.cs`
3. Add JavaScript handler in `index.html`

## License

GPL-2.0-or-later (matches GNOME Shell extension license)
