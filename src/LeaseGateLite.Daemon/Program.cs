using LeaseGateLite.Contracts;
using LeaseGateLite.Daemon;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<DaemonState>();
builder.WebHost.UseUrls("http://localhost:5177");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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

app.MapPost("/diagnostics/export", (DaemonState daemon) => Results.Ok(daemon.ExportDiagnostics()));

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

app.Run();
