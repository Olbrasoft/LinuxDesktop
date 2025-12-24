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

- GNOME Shell with `focus-tracker@olbrasoft.cz` extension enabled
- .NET 10.0
- D-Bus session bus access

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
