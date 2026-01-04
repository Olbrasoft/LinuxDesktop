# Analysis of LinuxDesktop Dependencies & Packaging

**Date:** 2026-01-04
**Scope:** Packaging status and system requirements analysis for `LinuxDesktop` library.

## 1. NuGet Packaging Status

**Current Status:** ❌ **No NuGet packages available.**

The project is currently configured as a **source-only library**.
- There are no public NuGet packages published on nuget.org.
- The `.csproj` files lack the necessary metadata for package generation (`<GeneratePackageOnBuild>`, `<PackageId>`, `<Version>`).
- GitHub workflows ("Build and Test", "Deploy to Production") do not include steps for publishing to NuGet.

**Future Intent:**
According to `README.md`, there is a plan to release `Olbrasoft.LinuxDesktop` as a NuGet package once the API stabilizes. For now, integration requires referencing the source code directly or adding it as a git submodule.

## 2. System Requirements (The "Hidden" Dependencies)

To use `LinuxDesktop` on a clean Linux installation (with only .NET Runtime installed), the following components are required. The library is **NOT** self-contained; it acts as a bridge to existing desktop services.

### A. Operating System & Desktop Environment
- **OS:** Linux with D-Bus support (Debian, Ubuntu, Fedora, etc.).
- **Desktop Environment:** **GNOME Shell** (Strong requirement).
  - While D-Bus is generic, the specific interfaces used (`org.gnome.Shell.*`) are exclusive to GNOME.
  - Usage on KDE/XFCE would require significant refactoring or separate implementation modules.

### B. Required GNOME Extensions
The library relies on specific GNOME Shell extensions to expose internal shell API over D-Bus. Without these, the library will throw runtime errors or return empty data.

1.  **Window Calls** (Essential for Window Management)
    - **Purpose:** Listing windows, getting details (title, app name), moving/focusing windows.
    - **ID:** `window-calls@domandoman.xyz`
    - **Installation:** `gnome-extensions install window-calls@domandoman.xyz`
    - **Why:** GNOME Wayland isolates windows; this extension bridges that gap via D-Bus at `/org/gnome/Shell/Extensions/Windows`.

2.  **Focus Tracker** (Essential for Event Monitoring)
    - **Purpose:** Emitting signals when the active window changes.
    - **ID:** `focus-tracker@olbrasoft.cz` (Custom extension mentioned in internal docs)
    - **Why:** Standard GNOME APIs do not reliably broadcast focus changes to external apps for security reasons.

### C. System Services (Daemons)
These are typically pre-installed on desktop distros, but crucial for minimal installs:

1.  **D-Bus Daemon** (`dbus-daemon` or `dbus-broker`)
    - The core communication channel.

2.  **AT-SPI Registry** (`at-spi2-registryd`)
    - **Required if:** Using the `LinuxDesktop.Accessibility` module.
    - **Purpose:** Provides accessibility tree for detailed UI inspection (reading buttons, menus inside apps).

### D. Native Libraries (P/Invoke)
- **Status:** ✅ **Clean.**
- The project does **not** use `DllImport` to bind to C libraries (like `libX11` or `libgtk`).
- It relies entirely on the D-Bus protocol (via `Tmds.DBus`), which is a pure C# implementation. This minimizes "dependency hell" regarding shared object (`.so`) versions.

## 3. Installation Guide for End Users

If a user asks: *"I have a fresh Linux box with .NET. How do I make this work?"*

**Step 1: Verify Environment**
Ensure you are running GNOME Shell:
```bash
echo $XDG_CURRENT_DESKTOP
# Output should contain: GNOME
```

**Step 2: Install Dependencies**
Install the bridge extensions (requires `gnome-shell-extension-manager` or browser connector):

```bash
# Example for Debian/Ubuntu to get extension management tools
sudo apt update && sudo apt install gnome-shell-extension-manager

# Install "Window Calls" (Manual or via GUI)
busctl --user call org.gnome.Shell.Extensions /org/gnome/Shell/Extensions org.gnome.Shell.Extensions InstallRemoteExtension s "window-calls@domandoman.xyz"
```

**Step 3: Enable Extensions**
After installation, ensure they are enabled:
```bash
gnome-extensions enable window-calls@domandoman.xyz
# If available/distributed:
gnome-extensions enable focus-tracker@olbrasoft.cz
```

**Step 4: Run Application**
Now the .NET application using `LinuxDesktop` can connect to the session bus.
