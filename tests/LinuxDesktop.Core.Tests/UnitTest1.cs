using Olbrasoft.LinuxDesktop.Core.Models;

namespace LinuxDesktop.Core.Tests;

public class WindowInfoTests
{
    [Fact]
    public void WindowInfo_CanBeCreated()
    {
        var windowInfo = new WindowInfo
        {
            Id = 12345,
            Title = "Test Window",
            WmClass = "test-app",
            Pid = 1000,
            HasFocus = true,
            InCurrentWorkspace = true
        };

        Assert.Equal(12345u, windowInfo.Id);
        Assert.Equal("Test Window", windowInfo.Title);
        Assert.Equal("test-app", windowInfo.WmClass);
        Assert.Equal(1000, windowInfo.Pid);
        Assert.True(windowInfo.HasFocus);
        Assert.True(windowInfo.InCurrentWorkspace);
    }

    [Fact]
    public void WindowInfo_IsImmutable()
    {
        var window1 = new WindowInfo { Id = 1, Title = "Window 1" };
        var window2 = window1 with { Title = "Window 2" };

        Assert.NotEqual(window1, window2);
        Assert.Equal("Window 1", window1.Title);
        Assert.Equal("Window 2", window2.Title);
    }
}

public class WindowDetailsTests
{
    [Fact]
    public void WindowDetails_InheritsFromWindowInfo()
    {
        var details = new WindowDetails
        {
            Id = 12345,
            Title = "Test Window",
            X = 100,
            Y = 200,
            Width = 800,
            Height = 600,
            CanClose = true,
            CanMaximize = true
        };

        Assert.Equal(12345u, details.Id);
        Assert.Equal("Test Window", details.Title);
        Assert.Equal(100, details.X);
        Assert.Equal(200, details.Y);
        Assert.Equal(800, details.Width);
        Assert.Equal(600, details.Height);
        Assert.True(details.CanClose);
        Assert.True(details.CanMaximize);
    }

    [Fact]
    public void WindowDetails_IsImmutable()
    {
        var details1 = new WindowDetails { Id = 1, X = 100, Y = 200 };
        var details2 = details1 with { X = 300 };

        Assert.NotEqual(details1, details2);
        Assert.Equal(100, details1.X);
        Assert.Equal(300, details2.X);
    }
}

public class WorkspaceInfoTests
{
    [Fact]
    public void WorkspaceInfo_CanBeCreated()
    {
        var workspace = new WorkspaceInfo
        {
            Index = 2,
            IsActive = true,
            WindowCount = 5
        };

        Assert.Equal(2, workspace.Index);
        Assert.True(workspace.IsActive);
        Assert.Equal(5, workspace.WindowCount);
    }

    [Fact]
    public void WorkspaceInfo_IsImmutable()
    {
        var ws1 = new WorkspaceInfo { Index = 1, IsActive = true };
        var ws2 = ws1 with { IsActive = false };

        Assert.NotEqual(ws1, ws2);
        Assert.True(ws1.IsActive);
        Assert.False(ws2.IsActive);
    }
}
