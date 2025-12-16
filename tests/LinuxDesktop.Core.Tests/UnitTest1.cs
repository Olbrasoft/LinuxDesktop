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
}
