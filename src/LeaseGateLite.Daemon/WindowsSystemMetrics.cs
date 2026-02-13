using System.Runtime.InteropServices;

namespace LeaseGateLite.Daemon;

/// <summary>
/// Windows-native system metrics via GlobalMemoryStatusEx and performance counters.
/// Returns accurate CPU and RAM data that matches Task Manager.
/// </summary>
public sealed class WindowsSystemMetrics
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    private readonly object _lock = new();
    private System.Diagnostics.PerformanceCounter? _cpuCounter;
    private DateTime _lastCpuSample = DateTime.MinValue;
    private int _cachedCpuPercent;

    /// <summary>
    /// Get system-wide CPU usage percentage (0-100).
    /// Cached for 1 second to avoid excessive sampling.
    /// Uses Performance Counter for accurate system-wide CPU measurement.
    /// </summary>
    public int GetCpuPercent()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            if (now - _lastCpuSample < TimeSpan.FromSeconds(1))
            {
                return _cachedCpuPercent;
            }

            try
            {
                if (_cpuCounter is null)
                {
                    _cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
                    // First call to NextValue() always returns 0, so we need to call it once to initialize
                    _cpuCounter.NextValue();
                    _lastCpuSample = now;
                    return _cachedCpuPercent; // Return cached value on initialization
                }

                var cpuValue = _cpuCounter.NextValue();
                _cachedCpuPercent = (int)Math.Round(cpuValue);
                _cachedCpuPercent = Math.Clamp(_cachedCpuPercent, 0, 100);
                _lastCpuSample = now;

                return _cachedCpuPercent;
            }
            catch
            {
                // Fallback to moderate value on any failure
                // This can happen if Performance Counters are disabled
                return 50;
            }
        }
    }

    /// <summary>
    /// Get available RAM percentage (0-100).
    /// Uses GlobalMemoryStatusEx - matches Task Manager exactly.
    /// Formula: 100 * ullAvailPhys / ullTotalPhys
    /// </summary>
    public int GetAvailableRamPercent()
    {
        try
        {
            var memStatus = new MEMORYSTATUSEX();
            if (!GlobalMemoryStatusEx(memStatus))
            {
                // P/Invoke failed - return conservative value
                return 35;
            }

            // This is the "can't lie" calculation
            // ullAvailPhys = bytes of physical RAM available right now
            // ullTotalPhys = total physical RAM installed
            var availablePercent = (int)Math.Round(100.0 * memStatus.ullAvailPhys / memStatus.ullTotalPhys);
            return Math.Clamp(availablePercent, 0, 100);
        }
        catch
        {
            // Fallback on any exception
            return 35;
        }
    }

    /// <summary>
    /// Get memory pressure percentage (0-100).
    /// This is the inverse of available RAM - higher means more pressure.
    /// Formula: 100 - availableRamPercent
    /// </summary>
    public int GetMemoryPressurePercent()
    {
        return 100 - GetAvailableRamPercent();
    }

    /// <summary>
    /// Get detailed memory info for diagnostics.
    /// </summary>
    public (ulong TotalPhysicalGB, ulong AvailablePhysicalGB, int AvailablePercent, int UsedPercent) GetMemoryDetails()
    {
        try
        {
            var memStatus = new MEMORYSTATUSEX();
            if (!GlobalMemoryStatusEx(memStatus))
            {
                return (0, 0, 0, 0);
            }

            var totalGB = memStatus.ullTotalPhys / (1024UL * 1024 * 1024);
            var availGB = memStatus.ullAvailPhys / (1024UL * 1024 * 1024);
            var availPercent = (int)Math.Round(100.0 * memStatus.ullAvailPhys / memStatus.ullTotalPhys);
            var usedPercent = 100 - availPercent;

            return (totalGB, availGB, availPercent, usedPercent);
        }
        catch
        {
            return (0, 0, 0, 0);
        }
    }
}
