namespace Olbrasoft.LinuxDesktop.Core.Models;

/// <summary>
/// Basic window information from window list.
/// </summary>
public record WindowInfo
{
    public required uint Id { get; init; }
    public string? Title { get; init; }
    public string? WmClass { get; init; }
    public string? WmClassInstance { get; init; }
    public int Pid { get; init; }
    public bool InCurrentWorkspace { get; init; }
    public bool HasFocus { get; init; }
    public int FrameType { get; init; }
    public int WindowType { get; init; }
}
