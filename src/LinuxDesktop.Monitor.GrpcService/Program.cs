using Olbrasoft.LinuxDesktop.Monitor.GrpcService.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on all network interfaces
builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP/2 for gRPC on port 5054
    options.ListenAnyIP(5054, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

// Add services
builder.Services.AddGrpc();
builder.Services.AddSingleton<DesktopStateCache>();
builder.Services.AddHostedService<DesktopStateMonitorService>();

var app = builder.Build();

// Map gRPC service
app.MapGrpcService<DesktopStateServiceImpl>();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
