using Olbrasoft.LinuxDesktop.Monitor.Web.Hubs;
using Olbrasoft.LinuxDesktop.Monitor.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on all network interfaces
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5051); // Listen on all network interfaces on port 5051
});

// Add services
builder.Services.AddSignalR();
builder.Services.AddHostedService<DesktopStateMonitorService>();

var app = builder.Build();

// Enable static files
app.UseStaticFiles();

// Map SignalR hub
app.MapHub<DesktopStateHub>("/hub/desktopstate");

// Serve index.html as default
app.MapGet("/", () => Results.Redirect("/index.html"));

app.Run();
