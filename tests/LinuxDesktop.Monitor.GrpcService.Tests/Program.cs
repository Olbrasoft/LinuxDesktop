using Grpc.Net.Client;
using Olbrasoft.LinuxDesktop.Monitor.GrpcService;

Console.WriteLine("Desktop State gRPC Client Test");
Console.WriteLine("===============================\n");

// Create gRPC channel
using var channel = GrpcChannel.ForAddress("http://localhost:5054");
var client = new DesktopStateService.DesktopStateServiceClient(channel);

Console.WriteLine("Testing GetState RPC...");
try
{
    var stateRequest = new StateRequest();
    var state = await client.GetStateAsync(stateRequest);

    Console.WriteLine($"✓ GetState succeeded:");
    Console.WriteLine($"  Current Workspace: {state.CurrentWorkspace}");
    Console.WriteLine($"  Total Workspaces: {state.TotalWorkspaces}");
    Console.WriteLine($"  Active Window: {state.ActiveWindow}");
    Console.WriteLine($"  Active Application: {state.ActiveApplication}");
    Console.WriteLine($"  Timestamp: {DateTimeOffset.FromUnixTimeSeconds(state.TimestampUnix)}\n");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ GetState failed: {ex.Message}\n");
}

Console.WriteLine("Testing StreamState RPC (5 seconds)...");
try
{
    var streamRequest = new StreamRequest();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var stream = client.StreamState(streamRequest, cancellationToken: cts.Token);

    var changeCount = 0;
    while (await stream.ResponseStream.MoveNext(cts.Token))
    {
        var change = stream.ResponseStream.Current;
        changeCount++;
        Console.WriteLine($"Change #{changeCount}: {change.Type}");
        Console.WriteLine($"  Workspace: {change.CurrentState.CurrentWorkspace}/{change.CurrentState.TotalWorkspaces}");
        Console.WriteLine($"  Window: {change.CurrentState.ActiveWindow}");
        Console.WriteLine($"  App: {change.CurrentState.ActiveApplication}");

        if (change.Type == ChangeType.WorkspaceChanged && change.WorkspaceEvent != null)
        {
            Console.WriteLine($"  → Workspace changed to {change.WorkspaceEvent.NewIndex}");
        }
        else if (change.Type == ChangeType.FocusChanged && change.FocusEvent != null)
        {
            Console.WriteLine($"  → Focus changed: {change.FocusEvent.WindowTitle}");
        }
        Console.WriteLine();
    }

    Console.WriteLine($"✓ StreamState succeeded ({changeCount} changes received)");
}
catch (OperationCanceledException)
{
    Console.WriteLine("✓ StreamState test completed (timeout reached)");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ StreamState failed: {ex.Message}");
}

Console.WriteLine("\nAll tests completed!");
