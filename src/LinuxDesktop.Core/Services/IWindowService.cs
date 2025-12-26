namespace Olbrasoft.LinuxDesktop.Core.Services;

/// <summary>
/// Composite service for interacting with desktop windows.
/// Combines all window-related operations following Interface Segregation Principle (ISP).
/// Clients can depend on specific interfaces (IWindowQueryService, IWindowActionService, etc.)
/// instead of this full interface when they only need a subset of functionality.
/// </summary>
public interface IWindowService : IWindowQueryService, IWindowActionService, IWindowLayoutService, IWindowWorkspaceService
{
    // This interface inherits all methods from:
    // - IWindowQueryService: GetWindowsAsync, GetWindowDetailsAsync, GetFocusedWindowAsync, GetWindowTitleAsync
    // - IWindowActionService: ActivateWindowAsync, CloseWindowAsync, MaximizeWindowAsync, MinimizeWindowAsync, UnmaximizeWindowAsync, UnminimizeWindowAsync
    // - IWindowLayoutService: MoveWindowAsync, ResizeWindowAsync
    // - IWindowWorkspaceService: MoveWindowToWorkspaceAsync
}
