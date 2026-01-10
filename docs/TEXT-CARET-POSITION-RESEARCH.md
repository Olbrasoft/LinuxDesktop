# Text Caret (Cursor) Position Research for Linux

Research summary: Methods for obtaining text cursor (caret) position in Linux desktop environments.

**Goal:** Position UI overlays (e.g., recording indicator, autocomplete popup) near the text caret, not the mouse cursor.

**Last Updated:** 2026-01-10

---

## Executive Summary

There are **3 main approaches** to getting caret position on Linux, each with different coverage and trade-offs:

| Method | Coverage | Precision | Complexity | Best For |
|--------|----------|-----------|------------|----------|
| **InputMethod/IBus** | ~20% | High | Low | IME-enabled apps |
| **AT-SPI2 Accessibility** | ~80% | Medium-High | Medium | Most apps |
| **FocusCaretTracker** (GNOME) | ~80% | High | Low | GNOME only |

**Recommendation:** Use **FocusCaretTracker** for GNOME (already implemented), investigate **AT-SPI2** for .NET direct access and KDE support.

---

## CRITICAL: Wayland Coordinate Conversion

**Key insight from Orca and GNOME magnifier.js:**

On Wayland, AT-SPI `get_character_extents()` with `SCREEN` coordinates often returns `(0, 0, 0, 0)` because applications cannot know their absolute screen position (Wayland security model).

**Solution (used by Orca and GNOME magnifier):**
1. Request extents in `WINDOW` coordinates (relative to application window)
2. Get window position from Mutter via `focusWindow.get_client_content_rect()`
3. Apply HiDPI scale factor
4. Manually compute screen coordinates

```javascript
// From magnifier.js _updateCaret
const windowExtents = text.get_character_extents(offset, Atspi.CoordType.WINDOW);
const focusWindow = global.display.focus_window;
const windowRect = focusWindow.get_client_content_rect();
const scaleFactor = St.ThemeContext.get_for_stage(global.stage).scale_factor;

const screenX = windowRect.x + (scaleFactor * windowExtents.x);
const screenY = windowRect.y + (scaleFactor * windowExtents.y);
```

**This works for ALL applications including terminals on Wayland** because:
- AT-SPI provides reliable window-relative coordinates
- Mutter (compositor) knows the actual window position on screen

---

## Current Implementation (GNOME Extension)

Our GNOME Shell extension (`gnome-extension/src/extension.ts`) implements caret tracking using **FocusCaretTracker** with proper Wayland coordinate conversion:

```typescript
import * as FocusCaretTracker from 'resource:///org/gnome/shell/ui/focusCaretTracker.js';

this._focusCaretTracker = new FocusCaretTracker.FocusCaretTracker();
this._focusCaretTracker.connect('caret-moved', this._onCaretMoved.bind(this));
this._focusCaretTracker.registerCaretListener();

private _onCaretMoved(_tracker: unknown, event: AtspiCaretEvent): void {
    const text = event.source.get_text_iface();
    const caretOffset = text.get_caret_offset();
    
    // Use WINDOW coordinates (works on Wayland!)
    const windowExtents = text.get_character_extents(caretOffset, Atspi.CoordType.WINDOW);
    
    // Convert to screen space using Mutter's window position
    const screenExtents = this._convertExtentsToScreenSpace(event.source, windowExtents);
}
```

**D-Bus Method:** `GetCaretPosition() → JSON { available, x, y, width, height, source }`

---

## Method 1: InputMethod / IBus Protocol

### How It Works
- Input Method Framework (IBus, Fcitx) receives cursor location from applications
- GNOME Shell's `Main.inputMethod` exposes `cursor-location-changed` signal
- Works on both X11 and Wayland

### Coverage (~20%)
**Works with:**
- GTK 3/4 applications with Input Method enabled
- Some Qt applications
- Applications actively using IME for text input

**Does NOT work with:**
- Terminal emulators (gnome-terminal, kitty, Alacritty)
- Electron apps (VS Code, Discord, Slack)
- Firefox/Chrome (partial, only in text inputs)
- Most native applications not using IME

### Implementation
```javascript
// GNOME Shell extension
const inputMethod = Main.inputMethod;
inputMethod.connect('cursor-location-changed', (im) => {
    const [x, y, w, h] = inputMethod.cursorLocation;
});
```

### Verdict
**Not recommended as primary solution** - too limited coverage. Only useful as fallback for IME-heavy workflows.

---

## Method 2: AT-SPI2 Accessibility API

### How It Works
AT-SPI2 (Assistive Technology Service Provider Interface) is Linux's accessibility framework. Applications expose their UI tree via D-Bus, including text caret position.

### Key D-Bus Interfaces
- **Bus:** Accessibility bus (`$AT_SPI_BUS_ADDRESS`)
- **Interface:** `org.a11y.atspi.Text`
- **Method:** `GetCaretOffset() → i` - Returns caret position in text
- **Method:** `GetCharacterExtents(offset, coordType) → (i, i, i, i)` - Returns (x, y, width, height)
- **Signal:** `object:text-caret-moved` - Emitted when caret moves

### Coverage (~80%)
**Works with:**
- GTK 3/4 applications (gedit, GNOME apps)
- Qt/KDE applications (Kate, Konsole)
- Firefox (with accessibility enabled)
- Chromium/Electron (with accessibility enabled)
- LibreOffice
- Most terminal emulators

**Does NOT work with:**
- Applications with accessibility disabled
- Some games and custom UI toolkits
- Proprietary applications without a11y support

### Requirements
1. **Accessibility must be enabled:**
   ```bash
   gsettings set org.gnome.desktop.interface toolkit-accessibility true
   ```
2. **AT-SPI2 daemon running:**
   ```bash
   # Usually automatic with accessibility enabled
   /usr/libexec/at-spi2-registryd
   ```

### Python Example (pyatspi2)
```python
import pyatspi

def on_caret_moved(event):
    text = event.source.queryText()
    offset = text.caretOffset
    extents = text.getCharacterExtents(offset, pyatspi.SCREEN_COORDS)
    print(f"Caret at: ({extents.x}, {extents.y}) {extents.width}x{extents.height}")

pyatspi.Registry.registerEventListener(on_caret_moved, "object:text-caret-moved")
pyatspi.Registry.start()
```

### .NET Implementation Path
No existing .NET AT-SPI library exists. Options:
1. **Tmds.DBus** - Generate C# interfaces from AT-SPI XML definitions
2. **P/Invoke** - Call libatspi directly
3. **Process wrapper** - Shell out to Python/accerciser

See: [AT-SPI-RESEARCH.md](./AT-SPI-RESEARCH.md) for detailed implementation roadmap.

### Verdict
**Best cross-DE solution** - Wide coverage, standardized API. Higher implementation effort for .NET.

---

## Method 3: GNOME FocusCaretTracker

### How It Works
GNOME Shell's `FocusCaretTracker` module wraps AT-SPI2 and provides a clean JavaScript API. It's used internally by GNOME's magnifier (accessibility zoom).

### Source Code Reference
```
/usr/share/gnome-shell/js/ui/focusCaretTracker.js
```

### API
```javascript
const tracker = new FocusCaretTracker.FocusCaretTracker();
tracker.registerCaretListener();   // Start listening
tracker.deregisterCaretListener(); // Stop listening

// Signals:
// 'caret-moved' - (tracker, event) where event.source is Atspi.Accessible
// 'focus-changed' - (tracker, event) when focused element changes
```

### Getting Caret Position
```javascript
tracker.connect('caret-moved', (tracker, event) => {
    const accessible = event.source;
    const text = accessible.get_text_iface();
    if (text) {
        const offset = text.get_caret_offset();
        const extents = text.get_character_extents(offset, Atspi.CoordType.SCREEN);
        // extents: { x, y, width, height }
    }
});
```

### Coverage
Same as AT-SPI2 (~80%) - FocusCaretTracker is a wrapper around AT-SPI2.

### Limitations
- **GNOME only** - Not available in KDE, XFCE, etc.
- **Shell extension only** - Cannot be used from standalone applications
- Requires GNOME Shell accessibility features

### Verdict
**Best for GNOME** - Already implemented in our extension. Simple API, good coverage.

---

## KDE Plasma Considerations

### Wayland Support (Plasma 6.2+)
KDE Plasma added caret tracking support for Wayland in version 6.2 (July 2024):
- Uses `text-input-v3` Wayland protocol
- AT-SPI2 still works for most applications
- Plasma 6.5 added explicit "Enable text caret tracking" option for Zoom plugin

### Implementation for KDE
KDE does NOT have FocusCaretTracker. Options:
1. **AT-SPI2 directly** - Same D-Bus interface works on KDE
2. **KWin scripting** - Limited, not recommended
3. **Wayland protocols** - Compositor-level, complex

### Current Status
- Debian 13 (Trixie) ships Plasma 6.3.6 - basic caret support should work
- AT-SPI2 is the recommended approach for KDE

---

## Desktop Environment Comparison

| Feature | GNOME | KDE Plasma | XFCE | Others |
|---------|-------|------------|------|--------|
| FocusCaretTracker | ✅ Built-in | ❌ Not available | ❌ Not available | ❌ Not available |
| AT-SPI2 Support | ✅ Full | ✅ Full | ⚠️ Partial | ⚠️ Varies |
| InputMethod | ✅ Full | ✅ Full | ⚠️ Partial | ⚠️ Varies |
| Wayland Caret | ✅ Works | ✅ 6.2+ | ❌ No Wayland | N/A |

---

## Implementation Recommendations

### For GNOME (Current)
**Status:** ✅ Implemented using FocusCaretTracker

The GNOME extension already provides `GetCaretPosition()` D-Bus method. No changes needed.

### For .NET Direct Access
**Status:** 🔲 Not implemented

To use caret position from .NET without the GNOME extension:
1. Connect to AT-SPI accessibility bus
2. Subscribe to `object:text-caret-moved` events
3. Query `org.a11y.atspi.Text.GetCharacterExtents()` for position

### For KDE Support
**Status:** 🔲 Not implemented

Options:
1. Create KDE Plasma extension (JavaScript/QML) exposing D-Bus interface
2. Use AT-SPI2 directly from .NET
3. Create standalone daemon monitoring AT-SPI2

---

## Testing Caret Position

### Test Current GNOME Implementation
```bash
# Get caret position via D-Bus
dbus-send --session --print-reply \
  --dest=org.olbrasoft.Desktop \
  /org/olbrasoft/Desktop \
  org.olbrasoft.Desktop.GetCaretPosition
```

### Test AT-SPI2 Directly
```bash
# Install accerciser (AT-SPI inspector)
sudo apt install accerciser

# Run and navigate to text field - shows caret info in Inspector tab
accerciser
```

### Monitor AT-SPI Events
```bash
# Install python3-pyatspi
sudo apt install python3-pyatspi

# Monitor caret events
python3 -c "
import pyatspi
def on_caret(e): print(f'Caret: {e.source} offset={e.detail1}')
pyatspi.Registry.registerEventListener(on_caret, 'object:text-caret-moved')
pyatspi.Registry.start()
"
```

---

## References

### Official Documentation
- [AT-SPI2 D-Bus Specification](https://gitlab.gnome.org/GNOME/at-spi2-core)
- [Ubuntu Desktop AT-SPI Reference](https://documentation.ubuntu.com/desktop/en/latest/reference/accessibility/dbus/)
- [GNOME Magnifier Source](https://gitlab.gnome.org/GNOME/gnome-shell/-/blob/main/js/ui/magnifier.js)
- [KDE Plasma 6.2 Release Notes](https://kde.org/announcements/plasma/6/6.2.0/)

### Related Research Documents
- [AT-SPI-RESEARCH.md](./AT-SPI-RESEARCH.md) - Detailed AT-SPI implementation for .NET
- [AT-SPI-WAYLAND-ANALYSIS.md](./AT-SPI-WAYLAND-ANALYSIS.md) - Wayland-specific considerations

### Tools
- **accerciser** - AT-SPI accessibility inspector (GTK)
- **pyatspi** - Python AT-SPI bindings
- **Orca** - GNOME screen reader (uses AT-SPI extensively)

---

## Appendix: Application Coverage Testing

### Tested Applications (GNOME + FocusCaretTracker)

| Application | Caret Works | Notes |
|-------------|-------------|-------|
| gedit | ✅ Yes | GTK4, reliable |
| GNOME Text Editor | ✅ Yes | GTK4, reliable |
| gnome-terminal | ✅ Yes | VTE-based, full AT-SPI |
| GNOME Console | ✅ Yes | VTE-based (GTK4), best accessibility |
| Firefox | ⚠️ Partial | Only in text inputs, not URL bar |
| VS Code | ⚠️ Partial | Electron, needs accessibility enabled |
| kitty | ❌ No | No AT-SPI implementation (maintainer confirmed) |
| Alacritty | ❌ No | No AT-SPI implementation (open issue since 2022) |
| LibreOffice Writer | ✅ Yes | Good support |
| Kate (KDE) | ✅ Yes | Qt, needs testing on GNOME |

### Enabling Accessibility for Electron Apps
```bash
# VS Code
code --enable-features=UseOzonePlatform --ozone-platform=wayland --force-renderer-accessibility

# Generic Electron
electron-app --force-renderer-accessibility
```

---

**Document Version:** 1.1  
**Author:** LinuxDesktop research for VirtualAssistant integration

---

## Appendix B: Orca Screen Reader Analysis

**Date:** 2026-01-10

### Background

Initial research incorrectly stated that terminals like kitty and Alacritty don't support caret tracking. User pointed out that **Orca screen reader works in terminals on Wayland**, proving that a solution exists.

### Analysis of Orca Source Code

**Repository:** https://gitlab.gnome.org/GNOME/orca

#### Key Finding: AT-SPI Works Identically on X11 and Wayland

Orca uses AT-SPI2 Text interface for caret tracking. **There is NO Wayland-specific code** for caret tracking - it works the same on both display servers.

**Primary API (from `src/orca/ax_text.py`):**
```python
@staticmethod
def get_caret_offset(obj: Atspi.Accessible) -> int:
    """Returns the caret offset of obj."""
    if not AXObject.supports_text(obj):
        return -1
    try:
        offset = Atspi.Text.get_caret_offset(obj)
    except GLib.GError as error:
        return -1
    return offset
```

**Terminal script (from `src/orca/scripts/terminal/script.py`):**
```python
def on_text_inserted(self, event: Atspi.Event) -> bool:
    offset = AXText.get_caret_offset(event.source)
    focus_manager.get_manager().set_last_cursor_position(event.source, offset)
    return True
```

#### Why AT-SPI Works on Wayland

AT-SPI2 communication happens via **D-Bus**, not X11 or Wayland protocols. The accessibility bus is independent of the display server.

The only Wayland limitation in Orca is for **mouse review** (screen coordinates for mouse position), not caret tracking:
```python
# From mouse_review.py
if os.environ.get("XDG_SESSION_TYPE", "").lower() != "wayland":
    # Mouse review requires X11 for screen coordinates
```

### The Real Problem: SCREEN vs WINDOW Coordinates

The issue is not X11 vs Wayland, but how to get **pixel coordinates** from caret offset.

**`get_character_extents(offset, coordType)` behavior:**

| CoordType | X11 | Wayland |
|-----------|-----|---------|
| `SCREEN` | ✅ Returns absolute screen position | ❌ Often returns (0,0,0,0) |
| `WINDOW` | ✅ Returns window-relative position | ✅ Returns window-relative position |

**Solution from GNOME magnifier.js:**
```javascript
// 1. Get WINDOW coordinates (always works)
const windowExtents = text.get_character_extents(offset, Atspi.CoordType.WINDOW);

// 2. Get window position from Mutter (compositor knows this)
const focusWindow = global.display.focus_window;
const windowRect = focusWindow.get_client_content_rect();

// 3. Apply HiDPI scale factor
const scaleFactor = St.ThemeContext.get_for_stage(global.stage).scale_factor;

// 4. Compute screen coordinates manually
const screenX = windowRect.x + (scaleFactor * windowExtents.x);
const screenY = windowRect.y + (scaleFactor * windowExtents.y);
```

### VTE Terminals and AT-SPI

VTE-based terminals (gnome-terminal, MATE Terminal, Tilix) expose AT-SPI Text interface. Orca has dedicated terminal support in `src/orca/scripts/terminal/`.

**Key events monitored:**
- `object:text-caret-moved` - caret position changed
- `object:text-changed:insert` - text inserted
- `object:text-changed:delete` - text deleted

### Why kitty/Alacritty Don't Work

These terminals **do not implement AT-SPI** (Assistive Technology Service Provider Interface).

**kitty maintainer statement (Nov 2025):**
> "kitty contains no code to integrate with screen readers."
> — [GitHub Discussion #9202](https://github.com/kovidgoyal/kitty/discussions/9202)

**Alacritty:**
- Open accessibility issue since March 2022, still unimplemented
- [GitHub Issue #5933](https://github.com/alacritty/alacritty/issues/5933)

This is **not a Wayland limitation** - it's a deliberate choice by terminal developers not to implement accessibility APIs.

**Terminals that DO work** (VTE-based, implement AT-SPI):
- gnome-terminal
- GNOME Console (Ptyxis)
- MATE Terminal
- XFCE Terminal
- Tilix

### Implementation Changes Made

Based on this analysis, we updated `gnome-extension/src/extension.ts`:

**Before (broken on Wayland):**
```typescript
const extents = text.get_character_extents(caretOffset, Atspi.CoordType.SCREEN);
// Often returns (0, 0, 0, 0) on Wayland
```

**After (works on Wayland):**
```typescript
// Use WINDOW coordinates
const windowExtents = text.get_character_extents(caretOffset, Atspi.CoordType.WINDOW);

// Convert to screen space using Mutter's window geometry
const screenExtents = this._convertExtentsToScreenSpace(event.source, windowExtents);
```

### Conclusion

1. **AT-SPI works on Wayland** - Orca proves this
2. **The problem was coordinate conversion** - SCREEN coords unreliable on Wayland
3. **Solution: WINDOW coords + Mutter window position** - Same as magnifier.js
4. **VTE terminals work** - gnome-terminal, not kitty/Alacritty (GPU rendering)

### References

- [Orca ax_text.py](https://gitlab.gnome.org/GNOME/orca/-/blob/main/src/orca/ax_text.py)
- [Orca terminal script](https://gitlab.gnome.org/GNOME/orca/-/blob/main/src/orca/scripts/terminal/script.py)
- [GNOME magnifier.js _updateCaret](https://gitlab.gnome.org/GNOME/gnome-shell/-/blob/main/js/ui/magnifier.js)
- [GNOME focusCaretTracker.js](https://gitlab.gnome.org/GNOME/gnome-shell/-/blob/main/js/ui/focusCaretTracker.js)

---

## Appendix C: kitty Terminal Cursor Position

**Date:** 2026-01-10

### Background

kitty terminal does not implement AT-SPI, so standard accessibility APIs don't work. However, kitty has its own powerful remote control protocol that might provide cursor position.

### Available Methods

#### Method 1: ANSI CPR (Cursor Position Report)

Standard terminal escape sequence, works in any terminal including kitty.

```bash
# Send query, read response
printf '\e[6n'; read -sdR REPLY; echo "${REPLY#*[}"
# Output: 5;12R  (row 5, column 12, 1-based)
```

**Limitation:** Returns row/column (cell position), NOT pixel coordinates.

**Implementation in kitty:** `kitty/screen.c` lines ~206-217

#### Method 2: KITTY_PIPE_DATA Environment Variable

Available when using kitty's launch mechanism with stdin piping.

```
Format: {scrolled_by}:{cursor_x},{cursor_y}:{lines},{columns}
Example: 0:5,12:24,80
```

**Source:** `kitty/boss.py` and `kitty/window.py::pipe_data()`

#### Method 3: kitten @ ls (Remote Control)

```bash
# Requires allow_remote_control=yes in kitty.conf
kitten @ ls --match recent:0
```

Returns JSON with window information. Cursor position available indirectly.

### Converting Cell Position to Pixels

kitty does NOT directly expose pixel coordinates. Manual calculation required:

```
pixel_x = cursor_column * cell_width
pixel_y = cursor_row * cell_height

Where:
- cell_width = window_width_pixels / columns
- cell_height = window_height_pixels / rows
```

**Problem:** Cell dimensions vary based on font, DPI, spacing settings.

### Potential Lead: IME Cursor Position

In `kitty/glfw.c` there is `get_ime_cursor_position()` function that provides **pixel coordinates** for Input Method Editor.

This suggests kitty internally tracks pixel position of cursor for IME purposes. This could potentially be:
1. Exposed via new remote control command
2. Used by a kitty kitten/extension
3. Queried via kitty's graphics protocol

**Status:** Not yet investigated in detail.

### Summary

| Method | Returns | Pixel Position |
|--------|---------|----------------|
| ANSI CPR `\e[6n` | row, column | ❌ No |
| KITTY_PIPE_DATA | cursor_x, cursor_y, dimensions | ❌ No (cells) |
| kitten @ ls | window info JSON | ❌ No |
| IME internal API | pixel x, y | ✅ Yes (internal only) |

### Conclusion

For kitty terminal, there is **no direct API for pixel-level cursor position**. Options:

1. **Use cell position + calculate pixels** - Requires knowing cell dimensions
2. **Investigate IME cursor API** - kitty has this internally, might be exposable
3. **Request feature from maintainer** - Add cursor pixel position to remote control

### References

- [kitty Remote Control Documentation](https://sw.kovidgoyal.net/kitty/remote-control/)
- [kitty Graphics Protocol](https://sw.kovidgoyal.net/kitty/graphics-protocol/)
- kitty source: `screen.c`, `glfw.c`, `window.py`, `boss.py`
