using System.Text.Json.Serialization;
using Olbrasoft.LinuxDesktop.Core.Models;

namespace Olbrasoft.LinuxDesktop.DBus.DTOs;

/// <summary>
/// Data transfer object for detailed window information from D-Bus JSON responses.
/// Inherits from WindowInfoDto and adds position, size, and capability properties.
/// </summary>
public record WindowDetailsDto : WindowInfoDto
{
    [JsonPropertyName("x")]
    public int X { get; init; }

    [JsonPropertyName("y")]
    public int Y { get; init; }

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }

    [JsonPropertyName("monitor")]
    public int Monitor { get; init; }

    [JsonPropertyName("layer")]
    public int Layer { get; init; }

    [JsonPropertyName("maximized")]
    public int Maximized { get; init; }

    [JsonPropertyName("role")]
    public string? Role { get; init; }

    [JsonPropertyName("canclose")]
    public bool CanClose { get; init; }

    [JsonPropertyName("canmaximize")]
    public bool CanMaximize { get; init; }

    [JsonPropertyName("canminimize")]
    public bool CanMinimize { get; init; }

    [JsonPropertyName("moveable")]
    public bool Moveable { get; init; }

    [JsonPropertyName("resizeable")]
    public bool Resizeable { get; init; }

    /// <summary>
    /// Converts DTO to domain model with window details.
    /// </summary>
    public WindowDetails ToWindowDetails() => new()
    {
        Id = Id,
        Title = Title,
        WmClass = WmClass,
        WmClassInstance = WmClassInstance,
        Pid = Pid,
        InCurrentWorkspace = InCurrentWorkspace,
        HasFocus = Focus,
        FrameType = FrameType,
        WindowType = WindowType,
        X = X,
        Y = Y,
        Width = Width,
        Height = Height,
        Monitor = Monitor,
        Layer = Layer,
        Maximized = Maximized,
        Role = Role,
        CanClose = CanClose,
        CanMaximize = CanMaximize,
        CanMinimize = CanMinimize,
        IsMoveable = Moveable,
        IsResizeable = Resizeable
    };
}
