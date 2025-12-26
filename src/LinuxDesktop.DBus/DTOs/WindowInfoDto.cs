using System.Text.Json.Serialization;
using Olbrasoft.LinuxDesktop.Core.Models;

namespace Olbrasoft.LinuxDesktop.DBus.DTOs;

/// <summary>
/// Data transfer object for window information from D-Bus JSON responses.
/// Maps snake_case JSON properties to PascalCase C# properties.
/// </summary>
public record WindowInfoDto
{
    [JsonPropertyName("id")]
    public uint Id { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("wm_class")]
    public string? WmClass { get; init; }

    [JsonPropertyName("wm_class_instance")]
    public string? WmClassInstance { get; init; }

    [JsonPropertyName("pid")]
    public int Pid { get; init; }

    [JsonPropertyName("in_current_workspace")]
    public bool InCurrentWorkspace { get; init; }

    [JsonPropertyName("focus")]
    public bool Focus { get; init; }

    [JsonPropertyName("frame_type")]
    public int FrameType { get; init; }

    [JsonPropertyName("window_type")]
    public int WindowType { get; init; }

    /// <summary>
    /// Converts DTO to domain model.
    /// </summary>
    public WindowInfo ToWindowInfo() => new()
    {
        Id = Id,
        Title = Title,
        WmClass = WmClass,
        WmClassInstance = WmClassInstance,
        Pid = Pid,
        InCurrentWorkspace = InCurrentWorkspace,
        HasFocus = Focus,
        FrameType = FrameType,
        WindowType = WindowType
    };
}
