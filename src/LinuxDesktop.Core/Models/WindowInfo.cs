namespace Olbrasoft.LinuxDesktop.Core.Models;

/// <summary>
/// Basic window information from window list.
/// </summary>
public class WindowInfo
{
    public uint Id { get; set; }
    public string? Title { get; set; }
    public string? WmClass { get; set; }
    public string? WmClassInstance { get; set; }
    public int Pid { get; set; }
    public bool InCurrentWorkspace { get; set; }
    public bool HasFocus { get; set; }
    public int FrameType { get; set; }
    public int WindowType { get; set; }
}
