# AGENTS.md

Instructions for AI agents (Claude Code, GitHub Copilot, etc.) working with this repository.

## Project Overview

LinuxDesktop is a .NET library for Linux desktop integration using D-Bus communication.
It provides APIs for window management, focus detection, notifications, and power management.

## Build Commands

```bash
dotnet build
dotnet test
dotnet publish -c Release -o ./publish
```

## Code Style

- **Framework:** .NET 10
- **Testing:** xUnit + Moq
- **Naming:** Microsoft C# conventions
- **Namespaces:** `Olbrasoft.LinuxDesktop.{Layer}`

## Project Structure

| Folder | Namespace | Purpose |
|--------|-----------|--------|
| `src/LinuxDesktop.Core/` | `Olbrasoft.LinuxDesktop.Core` | Interfaces, models |
| `src/LinuxDesktop.DBus/` | `Olbrasoft.LinuxDesktop.DBus` | D-Bus implementations |
| `tests/LinuxDesktop.Core.Tests/` | `LinuxDesktop.Core.Tests` | Unit tests |

## Key Dependencies

```xml
<PackageReference Include="Tmds.DBus.Protocol" Version="0.21.2" />
<PackageReference Include="Tmds.DBus.SourceGenerator" Version="0.0.21" />
```

## D-Bus Code Generation

```bash
# Install tool
dotnet tool install -g Tmds.DBus.Tool

# List available services
dotnet dbus list services --bus session
dotnet dbus list services --bus system

# Generate code for a service
dotnet dbus codegen --protocol-api --bus session --service org.gnome.Shell
```

## Important D-Bus Services

| Bus | Service | Purpose |
|-----|---------|--------|
| session | `org.gnome.Shell` | Window/workspace info |
| session | `org.gnome.SessionManager` | Session state |
| system | `org.freedesktop.login1` | Power management |
| session | `org.freedesktop.Notifications` | Notifications |

## Secrets

This library doesn't require secrets. It uses system D-Bus which doesn't need authentication.

## Testing on Different DEs

- **GNOME:** Full support planned
- **KDE:** Partial support (StatusNotifierItem works)
- **Others:** Basic D-Bus features only

## Related Repositories

- [VirtualAssistant](https://github.com/Olbrasoft/VirtualAssistant) - Will consume this library
- [engineering-handbook](https://github.com/Olbrasoft/engineering-handbook) - Coding standards
