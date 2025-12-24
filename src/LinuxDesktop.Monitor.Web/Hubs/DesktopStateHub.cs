using Microsoft.AspNetCore.SignalR;

namespace Olbrasoft.LinuxDesktop.Monitor.Web.Hubs;

public class DesktopStateHub : Hub
{
    public async Task SendWorkspaceChanged(int newIndex, int totalWorkspaces)
    {
        await Clients.All.SendAsync("WorkspaceChanged", newIndex, totalWorkspaces);
    }

    public async Task SendFocusChanged(string windowTitle, string appId, string wmClass)
    {
        await Clients.All.SendAsync("FocusChanged", windowTitle, appId, wmClass);
    }

    public async Task SendLogMessage(string message)
    {
        await Clients.All.SendAsync("LogMessage", message);
    }
}
