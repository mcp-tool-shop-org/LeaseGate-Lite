namespace LeaseGateLite.Daemon;

/// <summary>
/// Abstraction for system metrics (CPU, RAM) to enable testing without real hardware.
/// </summary>
public interface ISystemMetrics
{
    /// <summary>Get system-wide CPU usage percentage (0-100).</summary>
    int GetCpuPercent();

    /// <summary>Get available RAM percentage (0-100).</summary>
    int GetAvailableRamPercent();
}
