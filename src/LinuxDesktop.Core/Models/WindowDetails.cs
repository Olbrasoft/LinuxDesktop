namespace Olbrasoft.LinuxDesktop.Core.Models;

/// <summary>
/// Detailed window information including position and capabilities.
/// </summary>
public class WindowDetails : WindowInfo
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Monitor { get; set; }
    public int Layer { get; set; }
    public int Maximized { get; set; }
    public string? Role { get; set; }
    public bool CanClose { get; set; }
    public bool CanMaximize { get; set; }
    public bool CanMinimize { get; set; }
    public bool IsMoveable { get; set; }
    public bool IsResizeable { get; set; }
}
