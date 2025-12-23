# AT-SPI Research for .NET Integration on Linux

## Executive Summary

AT-SPI (Assistive Technology Service Provider Interface) is the core accessibility framework for Linux desktop environments. Version 2.0 uses D-Bus for inter-process communication, replacing the older CORBA-based implementation. This document summarizes research findings for implementing AT-SPI focus detection in .NET/C#.

**Key Finding:** There are **NO existing .NET/C# AT-SPI libraries** on NuGet or GitHub. Implementation will require using **Tmds.DBus** to create custom D-Bus bindings from AT-SPI XML interface definitions.

---

## 1. AT-SPI Architecture Overview

### 1.1 What is AT-SPI?

AT-SPI provides a standardized way for assistive technologies (screen readers, magnifiers, etc.) to query and control UI elements in applications. It works across different UI toolkits (GTK, Qt, etc.) on Linux.

**Key Components:**
- **at-spi2-core**: D-Bus interface definitions (XML), registry daemon, core library
- **at-spi2-atk**: Bridge between GTK/ATK and AT-SPI D-Bus
- **Applications**: Expose accessibility tree via D-Bus
- **Assistive Technologies**: Query accessible objects and listen for events

### 1.2 AT-SPI 2.0 vs 1.0

| Feature | AT-SPI 1.x | AT-SPI 2.x |
|---------|-----------|-----------|
| **IPC Protocol** | CORBA (ORBit/Bonobo) | D-Bus |
| **Reference Counting** | Remote reference counting | Weak references only |
| **Method Calls** | Synchronous | Mostly asynchronous signals |
| **Caching** | Limited | Aggressive client-side caching |
| **Performance** | Slower | Significantly faster |

---

## 2. D-Bus Architecture

### 2.1 AT-SPI D-Bus Services

AT-SPI uses a **dedicated accessibility bus** separate from the system/session bus:

```bash
# Find the accessibility bus address
echo $AT_SPI_BUS_ADDRESS

# Example: unix:abstract=/tmp/dbus-6fNHBTP2lK,guid=9d17aaa3ca64ff4b66f0c5985442999e
```

**Bus Configuration:**
- **Bus Name Pattern**: `org.a11y.atspi.Registry`, application-specific names
- **Object Path Pattern**: `/org/a11y/atspi/accessible/<object_id>`
- **Interface Prefix**: `org.a11y.atspi.*`

### 2.2 Enabling AT-SPI

Applications check the `org.a11y.Status.IsEnabled` property on the accessibility bus to determine if they should activate AT-SPI support.

---

## 3. Key D-Bus Interfaces

Based on Ubuntu Desktop documentation, AT-SPI exposes these interfaces:

### 3.1 Core Interfaces

| Interface | Purpose |
|-----------|---------|
| **org.a11y.atspi.Accessible** | Base interface for all accessible objects |
| **org.a11y.atspi.Registry** | Central registry for applications and AT |
| **org.a11y.atspi.Cache** | Cached accessibility tree data |

### 3.2 Object Property Interfaces

| Interface | Purpose |
|-----------|---------|
| **org.a11y.atspi.Component** | Position, size, layer info |
| **org.a11y.atspi.Text** | Text content and attributes |
| **org.a11y.atspi.EditableText** | Text editing operations |
| **org.a11y.atspi.Value** | Numeric values (sliders, etc.) |
| **org.a11y.atspi.Action** | Available actions on object |
| **org.a11y.atspi.Selection** | Selection operations |
| **org.a11y.atspi.Table** | Table structure and cells |

### 3.3 Event Interfaces (Signals)

| Interface | Purpose |
|-----------|---------|
| **org.a11y.atspi.Event.Focus** | Focus change events |
| **org.a11y.atspi.Event.Object** | State/property changes |
| **org.a11y.atspi.Event.Window** | Window events |
| **org.a11y.atspi.Event.Keyboard** | Keyboard events |
| **org.a11y.atspi.Event.Mouse** | Mouse events |

---

## 4. Focus Detection Strategy

### 4.1 Focus Event Signal

**Signal Name:** `org.a11y.atspi.Event.Focus::Focus`

**Signature:**
```dbus
Focus (
  unnamed_arg0 s,    # Detail string
  unnamed_arg1 i,    # Detail1 (usually 0)
  unnamed_arg2 i,    # Detail2 (usually 0)
  unnamed_arg3 v,    # Variant data
  properties a{sv}   # Property dictionary
)
```

**Signal Path:** Emitted from the focused accessible object path

### 4.2 Implementation Approach

**Option 1: Listen to Focus Events (Recommended)**
```
1. Connect to accessibility bus
2. Register signal handler for "org.a11y.atspi.Event.Focus::Focus"
3. When signal received:
   - Extract sender (D-Bus unique name)
   - Extract object path from signal
   - Query org.a11y.atspi.Accessible interface for object details
   - Get Name, Description, Role, etc.
```

**Option 2: Poll Focused Object**
```
1. Connect to accessibility bus
2. Call org.a11y.atspi.Registry.GetState to find focused element
3. Query focused element details
4. Poll periodically (NOT recommended - inefficient)
```

---

## 5. Available .NET Libraries

### 5.1 D-Bus Libraries for .NET

| Library | Status | Notes |
|---------|--------|-------|
| **Tmds.DBus** | ✅ Active | Official D-Bus library for .NET Core/5+ on Linux |
| **dbus-sharp** | ⚠️ Unmaintained | Older library, not recommended |

**Tmds.DBus:** https://github.com/tmds/Tmds.DBus

### 5.2 AT-SPI Libraries for .NET

**Result:** ❌ **NONE FOUND**

Extensive search revealed:
- **NuGet**: No AT-SPI packages exist
- **GitHub**: No C#/F#/.NET AT-SPI wrappers
- **Community**: No evidence of .NET AT-SPI implementations

**Conclusion:** We must build custom bindings using Tmds.DBus.

---

## 6. Tmds.DBus Code Generator

Tmds.DBus provides a code generator to create C# interfaces from D-Bus XML introspection data.

### 6.1 Installation

```bash
dotnet tool install -g Tmds.DBus.Tool
```

### 6.2 XML Interface Sources

AT-SPI interface definitions are available in the **at-spi2-core** repository:

**Repository:** https://gitlab.gnome.org/GNOME/at-spi2-core

**XML Files Location:**
```
at-spi2-core/
├── xml/
│   ├── Accessible.xml
│   ├── Registry.xml
│   ├── Cache.xml
│   ├── Component.xml
│   ├── Event.Focus.xml
│   ├── Event.Object.xml
│   └── ...
```

**Ubuntu Documentation:** https://documentation.ubuntu.com/desktop/en/latest/reference/accessibility/dbus/

### 6.3 Code Generation Process

```bash
# 1. Clone at-spi2-core repository
git clone https://gitlab.gnome.org/GNOME/at-spi2-core.git

# 2. Generate C# interfaces (example)
dotnet dbus codegen \
  --bus session \
  --service org.a11y.atspi.Registry \
  --object /org/a11y/atspi/registry \
  --introspect-xml at-spi2-core/xml/Accessible.xml \
  --output AccessibleInterfaces.cs
```

**Note:** The generator may need adjustments for the accessibility bus vs session bus.

---

## 7. Python Reference Implementations

While no .NET implementations exist, Python has mature AT-SPI support we can reference:

### 7.1 pyatspi2

**Package:** `python3-pyatspi`

**Example: Listening to Focus Events**
```python
import pyatspi

def on_focus(event):
    print(f"Focus changed to: {event.source.name}")
    print(f"Role: {event.source.getRoleName()}")
    print(f"Application: {event.source.getApplication().name}")

# Register event listener
pyatspi.Registry.registerEventListener(on_focus, "focus:")

# Start event loop
pyatspi.Registry.start()
```

### 7.2 Orca Screen Reader

**Repository:** https://gitlab.gnome.org/GNOME/orca

Orca is the GNOME screen reader and provides extensive AT-SPI usage examples in Python.

**Key Files:**
- `src/orca/script_manager.py`: Event listener registration
- `src/orca/focus_tracking_presenter.py`: Focus tracking logic

### 7.3 Accerciser

**Repository:** https://gitlab.gnome.org/GNOME/accerciser

Accerciser is an accessibility inspector tool, similar to what we might build for debugging.

---

## 8. Implementation Roadmap

### Phase 1: Basic D-Bus Connection
1. Install Tmds.DBus NuGet package
2. Connect to accessibility bus (parse `$AT_SPI_BUS_ADDRESS`)
3. Verify connection by listing services

### Phase 2: Generate C# Interfaces
1. Clone at-spi2-core repository
2. Extract necessary XML files:
   - `Accessible.xml`
   - `Event.Focus.xml`
   - `Registry.xml`
3. Use Tmds.DBus.Tool to generate C# interfaces
4. Adjust code generation for accessibility bus

### Phase 3: Focus Event Listener
1. Implement signal subscription for `org.a11y.atspi.Event.Focus::Focus`
2. Parse signal arguments
3. Query focused object details via `org.a11y.atspi.Accessible`
4. Extract Name, Role, Application info

### Phase 4: Testing
1. Test with various GTK applications (Firefox, gedit, etc.)
2. Test with Qt applications
3. Handle edge cases (focus lost, app crashes, etc.)
4. Performance testing (event frequency)

### Phase 5: Integration
1. Integrate with LinuxDesktop.Focus project
2. Provide clean C# API abstracting AT-SPI complexity
3. Documentation and examples

---

## 9. D-Bus Interfaces Needed

### 9.1 Minimum Implementation

```csharp
[DBusInterface("org.a11y.atspi.Accessible")]
interface IAccessible
{
    // Properties
    Task<string> GetNameAsync();
    Task<string> GetDescriptionAsync();
    Task<(string, uint)> GetRoleAsync();
    Task<ObjectPath> GetParentAsync();
    Task<int> GetChildCountAsync();

    // Methods
    Task<ObjectPath> GetChildAtIndexAsync(int index);
    Task<uint[]> GetStateAsync();
}

[DBusInterface("org.a11y.atspi.Component")]
interface IComponent
{
    Task<(int, int, int, int)> GetExtentsAsync(uint coordType);
    Task<int> GetLayerAsync();
}

[DBusInterface("org.a11y.atspi.Application")]
interface IApplication
{
    Task<string> GetToolkitNameAsync();
    Task<string> GetVersionAsync();
}
```

### 9.2 Event Subscription

```csharp
// Subscribe to focus events
var connection = new Connection(accessibilityBusAddress);
await connection.ConnectAsync();

await connection.AddMatchAsync(
    "type='signal',interface='org.a11y.atspi.Event.Focus',member='Focus'"
);

connection.Subscribe<(string, int, int, object, IDictionary<string, object>)>(
    "org.a11y.atspi.Event.Focus",
    OnFocusChanged
);
```

---

## 10. Challenges & Considerations

### 10.1 Known Issues

1. **Accessibility Bus Discovery**
   - Must parse `$AT_SPI_BUS_ADDRESS` environment variable
   - May differ per desktop session
   - Need fallback mechanism

2. **Object Lifetime**
   - AT-SPI uses weak references
   - Objects may disappear between queries
   - Need robust error handling

3. **Event Frequency**
   - Focus events can fire rapidly during navigation
   - Need debouncing/throttling
   - Consider performance impact

4. **Application Compatibility**
   - Not all apps implement AT-SPI correctly
   - Qt apps may use different object structure than GTK
   - Electron/Chrome apps have deep accessibility trees

### 10.2 Security Considerations

- AT-SPI can read **all UI content** system-wide
- Requires permissions to access accessibility bus
- Consider privacy implications
- Document security requirements

---

## 11. References

### Official Documentation
- **AT-SPI2 GitLab:** https://gitlab.gnome.org/GNOME/at-spi2-core
- **Ubuntu AT-SPI D-Bus Reference:** https://documentation.ubuntu.com/desktop/en/latest/reference/accessibility/dbus/
- **FreeDesktop AT-SPI2 Wiki:** https://www.freedesktop.org/wiki/Accessibility/AT-SPI2/

### Libraries & Tools
- **Tmds.DBus:** https://github.com/tmds/Tmds.DBus
- **pyatspi Documentation:** https://lazka.github.io/pgi-docs/Atspi-2.0/
- **Orca Screen Reader:** https://gitlab.gnome.org/GNOME/orca
- **Accerciser Inspector:** https://gitlab.gnome.org/GNOME/accerciser

### D-Bus Resources
- **D-Bus Specification:** https://dbus.freedesktop.org/doc/dbus-specification.html
- **D-Bus Tutorial:** https://dbus.freedesktop.org/doc/dbus-tutorial.html

---

## 12. Next Steps

1. **Proof of Concept:**
   - Create minimal .NET console app
   - Connect to accessibility bus
   - Subscribe to focus events
   - Print focused element name

2. **Interface Generation:**
   - Download at-spi2-core XML files
   - Generate C# interfaces with Tmds.DBus.Tool
   - Create NuGet package for AT-SPI interfaces

3. **Production Implementation:**
   - Build robust focus tracking service
   - Implement caching layer
   - Add error recovery
   - Performance optimization

4. **Documentation:**
   - API documentation
   - Usage examples
   - Troubleshooting guide
   - Security best practices

---

## Appendix A: Sample Code Skeleton

```csharp
using System;
using System.Threading.Tasks;
using Tmds.DBus;

namespace LinuxDesktop.Accessibility
{
    public class AtSpiMonitor
    {
        private Connection _connection;

        public async Task StartAsync()
        {
            // 1. Get accessibility bus address
            var busAddress = Environment.GetEnvironmentVariable("AT_SPI_BUS_ADDRESS");
            if (string.IsNullOrEmpty(busAddress))
            {
                throw new InvalidOperationException("AT-SPI bus not available");
            }

            // 2. Connect to bus
            _connection = new Connection(busAddress);
            await _connection.ConnectAsync();

            // 3. Subscribe to focus events
            await _connection.AddMatchAsync(
                "type='signal',interface='org.a11y.atspi.Event.Focus',member='Focus'"
            );

            // 4. Handle events
            _connection.Subscribe<AtSpiFocusEventArgs>(
                "org.a11y.atspi.Event.Focus",
                OnFocusChanged
            );

            Console.WriteLine("AT-SPI monitor started");
        }

        private async Task OnFocusChanged(Message message, AtSpiFocusEventArgs args)
        {
            try
            {
                // Query accessible object
                var objectPath = message.Path;
                var sender = message.Sender;

                var accessible = _connection.CreateProxy<IAccessible>(sender, objectPath);
                var name = await accessible.GetNameAsync();
                var role = await accessible.GetRoleAsync();

                Console.WriteLine($"Focus: {name} (Role: {role})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling focus event: {ex.Message}");
            }
        }
    }

    // Signal event args (will be generated from XML)
    public class AtSpiFocusEventArgs
    {
        public string Detail { get; set; }
        public int Detail1 { get; set; }
        public int Detail2 { get; set; }
        public object Data { get; set; }
        public IDictionary<string, object> Properties { get; set; }
    }
}
```

---

**Document Version:** 1.0
**Last Updated:** 2025-12-24
**Author:** Research compiled for LinuxDesktop.Focus integration
