using Olbrasoft.LinuxDesktop.DBus.Services;

namespace LinuxDesktop.DBus.Tests.Services;

/// <summary>
/// Tests for PointerService.
/// Note: These are mostly integration tests that require a running GNOME Shell extension.
/// Full unit testing would require refactoring to inject Connection as a dependency.
/// </summary>
public class PointerServiceTests
{
    [Fact]
    public void PointerService_ShouldImplementIPointerService()
    {
        // Compile-time verification
        Type serviceType = typeof(PointerService);
        Type interfaceType = typeof(Olbrasoft.LinuxDesktop.Core.Services.IPointerService);

        Assert.True(interfaceType.IsAssignableFrom(serviceType));
    }

    [Fact]
    public void PointerService_ShouldInheritFromDBusServiceBase()
    {
        Type serviceType = typeof(PointerService);
        Type baseType = typeof(DBusServiceBase);

        Assert.True(baseType.IsAssignableFrom(serviceType));
    }

    [Fact]
    public async Task CreateAsync_ReturnsPointerServiceInstance()
    {
        // Integration test that requires D-Bus and GNOME Shell extension
        try
        {
            await using var service = await PointerService.CreateAsync();
            Assert.NotNull(service);
        }
        catch (Exception ex) when (ex.Message.Contains("D-Bus") || ex.Message.Contains("connection"))
        {
            // Skip test if D-Bus is not available
        }
    }

    [Fact]
    public async Task GetPointerPositionAsync_ReturnsPosition()
    {
        // Integration test - requires running GNOME Shell extension
        try
        {
            await using var service = await PointerService.CreateAsync();
            var position = await service.GetPointerPositionAsync();

            // Position may be null if extension is not available, which is valid
            if (position.HasValue)
            {
                Assert.True(position.Value.X >= 0, "X coordinate should be non-negative");
                Assert.True(position.Value.Y >= 0, "Y coordinate should be non-negative");
            }
        }
        catch (Exception ex) when (ex.Message.Contains("D-Bus") || ex.Message.Contains("connection"))
        {
            // Skip test if D-Bus is not available
        }
    }

    [Fact]
    public async Task GetActiveWindowGeometryAsync_ReturnsGeometry()
    {
        // Integration test - requires running GNOME Shell extension
        try
        {
            await using var service = await PointerService.CreateAsync();
            var geometry = await service.GetActiveWindowGeometryAsync();

            // Geometry may be null if no window is focused or extension not available
            if (geometry.HasValue)
            {
                // Window dimensions should be positive (X/Y can be negative for multi-monitor setups)
                Assert.True(geometry.Value.Width >= 0, "Width should be non-negative");
                Assert.True(geometry.Value.Height >= 0, "Height should be non-negative");
            }
        }
        catch (Exception ex) when (ex.Message.Contains("D-Bus") || ex.Message.Contains("connection"))
        {
            // Skip test if D-Bus is not available
        }
    }
}
