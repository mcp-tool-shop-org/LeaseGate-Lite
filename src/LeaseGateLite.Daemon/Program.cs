using LeaseGateLite.Contracts;
using LeaseGateLite.Daemon;
using Microsoft.AspNetCore.Http.Timeouts;

const string MutexName = "Local\\LeaseGateLite.Daemon.Singleton";

var cliArgs = args.Select(a => a.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

if (cliArgs.Contains("--install-autostart"))
{
    var result = AutostartManager.SetEnabled(true);
    Console.WriteLine(result.Message);
    return;
}

if (cliArgs.Contains("--uninstall-autostart"))
{
    var result = AutostartManager.SetEnabled(false);
    Console.WriteLine(result.Message);
    return;
}

if (cliArgs.Contains("--status"))
{
    var autostart = AutostartManager.GetStatus();
    var running = Mutex.TryOpenExisting(MutexName, out var existing);
    existing?.Dispose();

    Console.WriteLine($"running={running}");
    Console.WriteLine($"autostartSupported={autostart.Supported}");
    Console.WriteLine($"autostartEnabled={autostart.Enabled}");
    Console.WriteLine($"autostartMechanism={autostart.Mechanism}");
    Console.WriteLine($"autostartCommand={autostart.Command}");
    return;
}

if (cliArgs.Count > 0 && !cliArgs.Contains("--run"))
{
    Console.WriteLine("Unknown mode. Supported: --run, --install-autostart, --uninstall-autostart, --status");
    return;
}

using var singleInstance = new SingleInstanceGuard(MutexName);
if (!singleInstance.Acquired)
{
    Console.WriteLine("daemon already running; refusing second instance");
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(5)
    };
});
builder.Services.AddSingleton<DaemonState>();
builder.WebHost.UseUrls("http://localhost:5177");

var app = builder.Build();

var daemon = app.Services.GetRequiredService<DaemonState>();
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(daemon.NotifyHostStopping);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRequestTimeouts();

app.Use(async (context, next) =>
{
    var daemonState = context.RequestServices.GetRequiredService<DaemonState>();
    var clientAppId = context.Request.Headers["X-Client-AppId"].ToString();
    var processName = context.Request.Headers["X-Process-Name"].ToString();
    var signature = context.Request.Headers["X-Client-Signature"].ToString();
    daemonState.RegisterClient(clientAppId, processName, signature);
    await next();
});

app.MapGet("/status", (DaemonState daemon) => Results.Ok(daemon.GetStatus()));

app.MapGet("/config", (DaemonState daemon) => Results.Ok(daemon.GetConfig()));

app.MapGet("/config/defaults", (DaemonState daemon) => Results.Ok(daemon.GetDefaults()));

app.MapPost("/config", (LiteConfig config, DaemonState daemon) =>
{
    var result = daemon.ApplyConfig(config);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/config/reset", (bool? apply, DaemonState daemon) => Results.Ok(daemon.ResetConfig(apply ?? false)));

app.MapPost("/service/start", (DaemonState daemon) => Results.Ok(daemon.Start()));

app.MapPost("/service/stop", (DaemonState daemon) => Results.Ok(daemon.Stop()));

app.MapPost("/service/restart", (DaemonState daemon) => Results.Ok(daemon.Restart()));
app.MapPost("/service/pause-background", (bool? paused, DaemonState daemon) => Results.Ok(daemon.SetBackgroundPause(paused ?? true)));

app.MapPost("/service/exit", (DaemonState daemon, IHostApplicationLifetime hostLifetime) =>
{
    daemon.NotifyHostStopping();
    hostLifetime.StopApplication();
    return Results.Ok(new ServiceCommandResponse { Success = true, Message = "daemon exiting" });
});

app.MapGet("/autostart/status", () => Results.Ok(AutostartManager.GetStatus()));

app.MapPost("/autostart", (AutostartUpdateRequest request) =>
{
    var result = AutostartManager.SetEnabled(request.Enabled);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapGet("/notifications", (DaemonState daemon) => Results.Ok(daemon.GetNotificationsSettings()));

app.MapPost("/notifications", (NotificationsUpdateRequest request, DaemonState daemon) =>
{
    var result = daemon.SetNotificationsEnabled(request.Enabled);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/diagnostics/export", (bool? includePaths, DaemonState daemon) => Results.Ok(daemon.ExportDiagnostics(includePaths ?? false)));

app.MapGet("/events/tail", (int? n, DaemonState daemon) => Results.Ok(daemon.GetEvents(n ?? 200)));

app.MapGet("/events/stream", (long? sinceId, int? timeoutMs, DaemonState daemon) =>
    Results.Ok(daemon.GetEventsSince(sinceId ?? 0, timeoutMs ?? 2500)));
    
app.MapGet("/presets", (DaemonState daemon) => Results.Ok(daemon.GetPresets()));
    
app.MapPost("/preset/preview", (PresetApplyRequest request, DaemonState daemon) =>
    Results.Ok(daemon.PreviewPreset(request.Name)));
    
app.MapPost("/preset/apply", (PresetApplyRequest request, DaemonState daemon) =>
{
    var result = daemon.ApplyPreset(request.Name);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapGet("/profiles", (DaemonState daemon) => Results.Ok(daemon.GetProfiles()));

app.MapPost("/profiles/apply", (SetAppProfileRequest request, DaemonState daemon) =>
{
    var result = daemon.SetAppProfile(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/simulate/pressure", (SimulatePressureRequest request, DaemonState daemon) =>
    Results.Ok(daemon.SetPressureMode(request.Mode)));

app.MapPost("/simulate/flood", (SimulateFloodRequest request, DaemonState daemon) =>
    Results.Ok(daemon.SimulateFlood(request)));

app.Run();
