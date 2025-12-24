namespace Olbrasoft.LinuxDesktop.Accessibility;

/// <summary>
/// Represents an accessible UI widget (control/element) exposed via AT-SPI.
/// </summary>
/// <param name="Name">Human-readable, localized name of the widget</param>
/// <param name="Role">The role/type of the widget (e.g., Entry, Button, Text)</param>
/// <param name="ApplicationName">Name of the application owning this widget</param>
/// <param name="Description">Optional detailed description of the widget</param>
/// <param name="ObjectPath">Internal D-Bus object path (for debugging)</param>
public record AccessibleWidget(
    string Name,
    AccessibleRole Role,
    string ApplicationName,
    string? Description = null,
    string? ObjectPath = null);
