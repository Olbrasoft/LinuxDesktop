using Olbrasoft.LinuxDesktop.DBus.Services;

namespace LinuxDesktop.DBus.Tests;

public class DBusExceptionTests
{
    [Fact]
    public void DBusException_ContainsErrorName()
    {
        var exception = new DBusException("org.freedesktop.DBus.Error.ServiceUnknown", "Service not found");

        Assert.Equal("org.freedesktop.DBus.Error.ServiceUnknown", exception.ErrorName);
        Assert.Contains("Service not found", exception.Message);
        Assert.Contains("org.freedesktop.DBus.Error.ServiceUnknown", exception.Message);
    }
}
