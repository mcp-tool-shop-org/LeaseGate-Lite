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

app.MapPost("/config", (LiteConfig config, DaemonState daemon) => Results.Ok(daemon.ApplyConfig(config)));

app.MapPost("/config/reset", (DaemonState daemon) => Results.Ok(daemon.ResetConfig()));

app.MapPost("/service/start", (DaemonState daemon) => Results.Ok(daemon.Start()));

app.MapPost("/service/stop", (DaemonState daemon) => Results.Ok(daemon.Stop()));

app.MapPost("/service/restart", (DaemonState daemon) => Results.Ok(daemon.Restart()));

app.MapPost("/diagnostics/export", (DaemonState daemon) => Results.Ok(daemon.ExportDiagnostics()));

app.MapGet("/events/tail", (int? n, DaemonState daemon) => Results.Ok(daemon.GetEvents(n ?? 200)));

app.Run();
