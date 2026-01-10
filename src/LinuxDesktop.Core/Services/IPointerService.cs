namespace Olbrasoft.LinuxDesktop.Core.Services;

public interface IPointerService : IAsyncDisposable
{
    Task<(int X, int Y)?> GetPointerPositionAsync(CancellationToken cancellationToken = default);

    Task<(int X, int Y, int Width, int Height)?> GetActiveWindowGeometryAsync(CancellationToken cancellationToken = default);

    Task ShowRecordingOverlayAsync(string text, CancellationToken cancellationToken = default);

    Task HideRecordingOverlayAsync(CancellationToken cancellationToken = default);
}
