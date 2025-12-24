# LinuxDesktop.Monitor.GrpcService

Fast gRPC service providing real-time access to GNOME desktop state (workspace, active window, and application).

## Overview

This service connects to the GNOME Shell D-Bus extension (`org.olbrasoft.Desktop`) and exposes desktop state through a high-performance gRPC API with Protocol Buffers serialization.

## Features

- **Fast API**: gRPC with Protobuf for minimal latency
- **Real-time updates**: Stream desktop state changes as they happen
- **Query current state**: Get snapshot of current desktop state
- **Network accessible**: Listen on all interfaces (configurable port)

## API Endpoints

### GetState
Get current desktop state (one-time query).

**Request:** `StateRequest` (empty)

**Response:** `DesktopState`
```protobuf
message DesktopState {
  int32 current_workspace = 1;
  int32 total_workspaces = 2;
  string active_window = 3;
  string active_application = 4;
  int64 timestamp_unix = 5;
}
```

### StreamState
Subscribe to real-time desktop state changes (streaming).

**Request:** `StreamRequest` (empty)

**Response:** Stream of `DesktopStateChange`
```protobuf
message DesktopStateChange {
  ChangeType type = 1;
  DesktopState current_state = 2;
  WorkspaceChangedEvent workspace_event = 3;
  FocusChangedEvent focus_event = 4;
  int64 timestamp_unix = 5;
}

enum ChangeType {
  UNKNOWN = 0;
  WORKSPACE_CHANGED = 1;
  FOCUS_CHANGED = 2;
}
```

## Configuration

Default port: **5054** (HTTP/2 only)

To change port, edit `Program.cs`:
```csharp
options.ListenAnyIP(5054, listenOptions =>
{
    listenOptions.Protocols = HttpProtocols.Http2;
});
```

## Running the Service

### Development
```bash
cd ~/Olbrasoft/LinuxDesktop/src/LinuxDesktop.Monitor.GrpcService
dotnet run
```

### Production (systemd service)

The service runs as a systemd user service.

**Service management:**
```bash
# Check status
systemctl --user status desktop-monitor-grpc.service

# Start service
systemctl --user start desktop-monitor-grpc.service

# Stop service
systemctl --user stop desktop-monitor-grpc.service

# Restart service
systemctl --user restart desktop-monitor-grpc.service

# View logs
journalctl --user -u desktop-monitor-grpc.service -f

# View recent logs
journalctl --user -u desktop-monitor-grpc.service -n 50
```

**Deployment path:**
- Binaries: `/opt/olbrasoft/desktop-monitor-grpc/app/`
- Service file: `~/.config/systemd/user/desktop-monitor-grpc.service`

**Manual deployment:**
```bash
# Publish to deployment directory
cd ~/Olbrasoft/LinuxDesktop
dotnet publish src/LinuxDesktop.Monitor.GrpcService/LinuxDesktop.Monitor.GrpcService.csproj \
  -c Release -o /opt/olbrasoft/desktop-monitor-grpc/app --no-self-contained

# Restart service
systemctl --user restart desktop-monitor-grpc.service
```

## Client Usage Example

See `tests/LinuxDesktop.Monitor.GrpcService.Tests/` for complete example.

```csharp
using Grpc.Net.Client;
using Olbrasoft.LinuxDesktop.Monitor.GrpcService;

// Create channel
using var channel = GrpcChannel.ForAddress("http://localhost:5054");
var client = new DesktopStateService.DesktopStateServiceClient(channel);

// Get current state
var state = await client.GetStateAsync(new StateRequest());
Console.WriteLine($"Workspace: {state.CurrentWorkspace}/{state.TotalWorkspaces}");
Console.WriteLine($"Window: {state.ActiveWindow}");
Console.WriteLine($"App: {state.ActiveApplication}");

// Stream changes
var stream = client.StreamState(new StreamRequest());
await foreach (var change in stream.ResponseStream.ReadAllAsync())
{
    Console.WriteLine($"Change: {change.Type}");
    // Process change...
}
```

## Integration Examples

### PushToTalk Context Awareness
The gRPC service enables applications like PushToTalk to query desktop state and provide context-aware features:

```csharp
var state = await client.GetStateAsync(new StateRequest());

// Example: Remind Claude Code to check engineering handbook
if (state.ActiveApplication.Contains("terminator") &&
    state.ActiveWindow.Contains("Claude Code"))
{
    prompt += "\nReminder: Check engineering handbook for best practices.";
}
```

## Requirements

### System Requirements

- **GNOME Shell** (tested on 48.4)
- **D-Bus session bus**
- **.NET 10.0 SDK**
- **Linux** (Debian 13 Trixie or compatible)

### GNOME Shell Extension

The gRPC service requires `focus-tracker@olbrasoft.cz` extension to be installed and enabled.

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

- **Grpc.AspNetCore** 2.64.0 - gRPC framework for ASP.NET Core
- **Tmds.DBus** 0.22.0 - D-Bus client library for .NET
- **System.Reactive** 6.1.0 - Reactive Extensions for observable state management

No manual package installation required - `dotnet restore` handles everything.

## Dependencies

**Runtime dependencies:**
- GNOME Extension: `focus-tracker@olbrasoft.cz` (must be running)
- D-Bus Service: `org.olbrasoft.Desktop` at `/org/olbrasoft/Desktop`

**Development dependencies:**
- .NET 10.0 SDK
- NuGet packages (listed above)

## Architecture

```
GNOME Extension (focus-tracker)
    ↓ D-Bus signals
DesktopStateMonitorService
    ↓ Updates
DesktopStateCache (Observable)
    ↓ Provides
DesktopStateServiceImpl (gRPC)
    ↓ Serves
gRPC Clients (any application)
```

## Troubleshooting

### Port already in use
Check what's using the port:
```bash
ss -tulpn | grep :5054
```

Kill the process if needed:
```bash
kill <PID>
```

### Service not receiving D-Bus events
Verify GNOME extension is running:
```bash
gnome-extensions list --enabled | grep focus-tracker
```

Test D-Bus service:
```bash
busctl --user introspect org.olbrasoft.Desktop /org/olbrasoft/Desktop
```

### Connection refused
Ensure service is running:
```bash
ps aux | grep LinuxDesktop.Monitor.GrpcService
```

Check logs:
```bash
journalctl --user -u desktop-monitor-grpc -f  # if running as systemd service
```
