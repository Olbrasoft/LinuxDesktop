using Grpc.Core;

namespace Olbrasoft.LinuxDesktop.Monitor.GrpcService.Services;

public class DesktopStateServiceImpl : DesktopStateService.DesktopStateServiceBase
{
    private readonly DesktopStateCache _cache;
    private readonly ILogger<DesktopStateServiceImpl> _logger;

    public DesktopStateServiceImpl(DesktopStateCache cache, ILogger<DesktopStateServiceImpl> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public override Task<DesktopState> GetState(StateRequest request, ServerCallContext context)
    {
        _logger.LogDebug("GetState called from {Peer}", context.Peer);
        var state = _cache.GetCurrentState();
        return Task.FromResult(state);
    }

    public override async Task StreamState(StreamRequest request, IServerStreamWriter<DesktopStateChange> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("StreamState started for {Peer}", context.Peer);

        try
        {
            // Send current state immediately
            var currentState = _cache.GetCurrentState();
            await responseStream.WriteAsync(new DesktopStateChange
            {
                Type = ChangeType.Unknown,
                CurrentState = currentState,
                TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });

            _logger.LogDebug("Sent initial state to {Peer}", context.Peer);

            // Subscribe to state changes and stream them
            using var subscription = _cache.StateChanges.Subscribe(async change =>
            {
                if (!context.CancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await responseStream.WriteAsync(change);
                        _logger.LogDebug("Sent {Type} change to {Peer}", change.Type, context.Peer);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send change to {Peer}", context.Peer);
                    }
                }
            });

            // Wait until client disconnects
            await Task.Delay(Timeout.Infinite, context.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("StreamState cancelled for {Peer}", context.Peer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in StreamState for {Peer}", context.Peer);
            throw;
        }
    }
}
