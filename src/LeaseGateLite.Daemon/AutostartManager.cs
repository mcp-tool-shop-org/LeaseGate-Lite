using System.Diagnostics;
using Microsoft.Win32;
using LeaseGateLite.Contracts;

namespace LeaseGateLite.Daemon;

public static class AutostartManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "LeaseGateLite.Daemon";

    public static AutostartStatusResponse GetStatus()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new AutostartStatusResponse
            {
                Supported = false,
                Enabled = false,
                Mechanism = "none",
                Message = "autostart is only supported on Windows"
            };
        }

        var command = BuildLaunchCommand();
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        var current = key?.GetValue(ValueName) as string;

        return new AutostartStatusResponse
        {
            Supported = true,
            Enabled = !string.IsNullOrWhiteSpace(current),
            Mechanism = "registry-run",
            Command = command,
            Message = string.IsNullOrWhiteSpace(current) ? "autostart disabled" : "autostart enabled"
        };
    }

    public static ServiceCommandResponse SetEnabled(bool enabled)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new ServiceCommandResponse
            {
                Success = false,
                Message = "autostart is only supported on Windows"
            };
        }

        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (key is null)
        {
            return new ServiceCommandResponse
            {
                Success = false,
                Message = "unable to open HKCU run key"
            };
        }

        if (enabled)
        {
            key.SetValue(ValueName, BuildLaunchCommand());
            return new ServiceCommandResponse { Success = true, Message = "autostart enabled" };
        }

        key.DeleteValue(ValueName, false);
        return new ServiceCommandResponse { Success = true, Message = "autostart disabled" };
    }

    public static string BuildLaunchCommand()
    {
        var processPath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        var assemblyPath = Environment.ProcessPath ?? string.Empty;

        if (string.IsNullOrWhiteSpace(processPath) || string.IsNullOrWhiteSpace(assemblyPath))
        {
            return string.Empty;
        }

        if (processPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
        {
            return $"\"{processPath}\" \"{assemblyPath}\" --run";
        }

        return $"\"{processPath}\" --run";
    }
}
