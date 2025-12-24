using System.Reactive.Subjects;

namespace Olbrasoft.LinuxDesktop.Monitor.GrpcService.Services;

public class DesktopStateCache
{
    private readonly Subject<DesktopStateChange> _stateChanges = new();
    private DesktopState _currentState = new();
    private readonly object _lock = new();

    public IObservable<DesktopStateChange> StateChanges => _stateChanges;

    public DesktopState GetCurrentState()
    {
        lock (_lock)
        {
            return new DesktopState
            {
                CurrentWorkspace = _currentState.CurrentWorkspace,
                TotalWorkspaces = _currentState.TotalWorkspaces,
                ActiveWindow = _currentState.ActiveWindow,
                ActiveApplication = _currentState.ActiveApplication,
                TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }
    }

    public void UpdateWorkspace(int newIndex, int totalWorkspaces)
    {
        lock (_lock)
        {
            _currentState.CurrentWorkspace = newIndex;
            _currentState.TotalWorkspaces = totalWorkspaces;
            _currentState.TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            _stateChanges.OnNext(new DesktopStateChange
            {
                Type = ChangeType.WorkspaceChanged,
                CurrentState = new DesktopState
                {
                    CurrentWorkspace = _currentState.CurrentWorkspace,
                    TotalWorkspaces = _currentState.TotalWorkspaces,
                    ActiveWindow = _currentState.ActiveWindow,
                    ActiveApplication = _currentState.ActiveApplication,
                    TimestampUnix = _currentState.TimestampUnix
                },
                WorkspaceEvent = new WorkspaceChangedEvent
                {
                    NewIndex = newIndex,
                    TotalWorkspaces = totalWorkspaces
                },
                TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });
        }
    }

    public void UpdateFocus(string windowTitle, string appId, string wmClass)
    {
        lock (_lock)
        {
            _currentState.ActiveWindow = windowTitle;
            _currentState.ActiveApplication = appId;
            _currentState.TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            _stateChanges.OnNext(new DesktopStateChange
            {
                Type = ChangeType.FocusChanged,
                CurrentState = new DesktopState
                {
                    CurrentWorkspace = _currentState.CurrentWorkspace,
                    TotalWorkspaces = _currentState.TotalWorkspaces,
                    ActiveWindow = _currentState.ActiveWindow,
                    ActiveApplication = _currentState.ActiveApplication,
                    TimestampUnix = _currentState.TimestampUnix
                },
                FocusEvent = new FocusChangedEvent
                {
                    WindowTitle = windowTitle,
                    AppId = appId,
                    WmClass = wmClass
                },
                TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });
        }
    }

    public void SetInitialState(int workspace, int totalWorkspaces, string window, string app)
    {
        lock (_lock)
        {
            _currentState = new DesktopState
            {
                CurrentWorkspace = workspace,
                TotalWorkspaces = totalWorkspaces,
                ActiveWindow = window,
                ActiveApplication = app,
                TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }
    }
}
