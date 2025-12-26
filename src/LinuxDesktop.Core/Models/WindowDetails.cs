namespace Olbrasoft.LinuxDesktop.Core.Models;

/// <summary>
/// Detailed window information including position and capabilities.
/// </summary>
public record WindowDetails : WindowInfo
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public int Monitor { get; init; }
    public int Layer { get; init; }
    public int Maximized { get; init; }
    public string? Role { get; init; }
    public bool CanClose { get; init; }
    public bool CanMaximize { get; init; }
    public bool CanMinimize { get; init; }
    public bool IsMoveable { get; init; }
    public bool IsResizeable { get; init; }
}
