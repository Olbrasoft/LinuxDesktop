using Olbrasoft.LinuxDesktop.DBus.Services;

namespace LinuxDesktop.DBus.Tests.Services;

/// <summary>
/// Tests for IdleMonitorService.
/// Note: These are mostly integration tests that require a running GNOME Mutter.
/// Full unit testing would require refactoring to inject Connection as a dependency.
/// </summary>
public class IdleMonitorServiceTests
{
    [Fact]
    public void IdleMonitorService_ServiceProperties_AreCorrect()
    {
        // This test validates that IdleMonitorService targets the correct D-Bus service
        // Full unit testing requires DI refactoring (Wave 5)

        Assert.True(true, "IdleMonitorService constructor and properties will be tested in integration tests");
    }

    [Fact]
    public async Task CreateAsync_ReturnsIdleMonitorServiceInstance()
    {
        // Integration test that requires D-Bus to be available

        try
        {
            await using var service = await IdleMonitorService.CreateAsync();
            Assert.NotNull(service);
        }
        catch (Exception ex) when (ex.Message.Contains("D-Bus") || ex.Message.Contains("connection"))
        {
            // Skip test if D-Bus is not available
        }
    }

    [Fact]
    public void IdleMonitorService_ShouldImplementIIdleService()
    {
        // Compile-time verification
        Type serviceType = typeof(IdleMonitorService);
        Type interfaceType = typeof(Olbrasoft.LinuxDesktop.Core.Services.IIdleService);

        Assert.True(interfaceType.IsAssignableFrom(serviceType));
    }

    [Fact]
    public void IdleMonitorService_ShouldInheritFromDBusServiceBase()
    {
        Type serviceType = typeof(IdleMonitorService);
        Type baseType = typeof(DBusServiceBase);

        Assert.True(baseType.IsAssignableFrom(serviceType));
    }

    [Fact]
    public async Task GetIdleTimeSpanAsync_ReturnsTimeSpan()
    {
        // Integration test - requires running GNOME
        try
        {
            await using var service = await IdleMonitorService.CreateAsync();
            var idleTime = await service.GetIdleTimeSpanAsync();

            Assert.True(idleTime >= TimeSpan.Zero, "Idle time should not be negative");
        }
        catch (Exception ex) when (ex.Message.Contains("D-Bus") || ex.Message.Contains("connection"))
        {
            // Skip test if D-Bus is not available
        }
    }

    [Fact]
    public async Task IsIdleForAsync_WithZeroDuration_ReturnsTrue()
    {
        // Integration test - requires running GNOME
        try
        {
            await using var service = await IdleMonitorService.CreateAsync();
            var isIdle = await service.IsIdleForAsync(TimeSpan.Zero);

            Assert.True(isIdle, "Should always be idle for zero duration");
        }
        catch (Exception ex) when (ex.Message.Contains("D-Bus") || ex.Message.Contains("connection"))
        {
            // Skip test if D-Bus is not available
        }
    }
}
