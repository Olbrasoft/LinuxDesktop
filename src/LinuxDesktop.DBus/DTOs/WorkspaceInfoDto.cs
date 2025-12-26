using System.Text.Json.Serialization;
using Olbrasoft.LinuxDesktop.Core.Models;

namespace Olbrasoft.LinuxDesktop.DBus.DTOs;

/// <summary>
/// Data transfer object for workspace information from D-Bus JSON responses.
/// Maps snake_case JSON properties to PascalCase C# properties.
/// </summary>
public record WorkspaceInfoDto
{
    [JsonPropertyName("index")]
    public int Index { get; init; }

    [JsonPropertyName("active")]
    public bool Active { get; init; }

    [JsonPropertyName("windows")]
    public int Windows { get; init; }

    /// <summary>
    /// Converts DTO to domain model.
    /// </summary>
    public WorkspaceInfo ToWorkspaceInfo() => new()
    {
        Index = Index,
        IsActive = Active,
        WindowCount = Windows
    };
}
