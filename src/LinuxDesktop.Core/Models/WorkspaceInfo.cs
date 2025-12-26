namespace Olbrasoft.LinuxDesktop.Core.Models;

/// <summary>
/// Workspace information.
/// </summary>
public record WorkspaceInfo
{
    public required int Index { get; init; }
    public bool IsActive { get; init; }
    public int WindowCount { get; init; }
}
