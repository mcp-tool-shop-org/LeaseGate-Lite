using System.Text.Json;
using LeaseGateLite.Contracts;

namespace LeaseGateLite.Daemon;

public sealed class DaemonState
{
    private readonly object _lock = new();
    private readonly Random _random = new();
    private readonly List<EventEntry> _events = new();
    private readonly string _runtimeDirectory;
    private readonly string _configPath;
    private readonly string _diagnosticsDirectory;
    private LiteConfig _config;
    private bool _running;
    private DateTimeOffset _startedAtUtc;
    private int _activeCalls;
    private int _interactiveQueueDepth;
    private int _backgroundQueueDepth;
    private int _effectiveConcurrency;
    private int _cpuPercent;
    private int _availableRamPercent;
    private string _lastThrottleReason = "none";
    private bool _adaptiveClampActive;

    public DaemonState()
    {
        _runtimeDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LeaseGateLite");
        _configPath = Path.Combine(_runtimeDirectory, "leasegatelite.config.json");
        _diagnosticsDirectory = Path.Combine(_runtimeDirectory, "diagnostics");
        Directory.CreateDirectory(_runtimeDirectory);
        Directory.CreateDirectory(_diagnosticsDirectory);

        _config = LoadConfig() ?? new LiteConfig();
        _running = true;
        _startedAtUtc = DateTimeOffset.UtcNow;
        _effectiveConcurrency = _config.MaxConcurrency;

        AddEvent("info", "daemon started");
    }

    public StatusSnapshot GetStatus()
    {
        lock (_lock)
        {
            SimulateTick();

            return new StatusSnapshot
            {
                Connected = true,
                DaemonRunning = _running,
                DaemonVersion = "0.1.0-lite",
                Uptime = _running ? DateTimeOffset.UtcNow - _startedAtUtc : TimeSpan.Zero,
                Endpoint = "http://localhost:5177",
                ConfigFilePath = _configPath,
                HeatState = DeriveHeatState(),
                ActiveCalls = _activeCalls,
                InteractiveQueueDepth = _interactiveQueueDepth,
                BackgroundQueueDepth = _backgroundQueueDepth,
                EffectiveConcurrency = _effectiveConcurrency,
                CpuPercent = _cpuPercent,
                AvailableRamPercent = _availableRamPercent,
                LastThrottleReason = _lastThrottleReason,
                AdaptiveClampActive = _adaptiveClampActive
            };
        }
    }

    public LiteConfig GetConfig()
    {
        lock (_lock)
        {
            return Clone(_config);
        }
    }

    public ServiceCommandResponse ApplyConfig(LiteConfig incoming)
    {
        lock (_lock)
        {
            _config = Clone(incoming);
            PersistConfig();
            _effectiveConcurrency = Math.Clamp(_effectiveConcurrency, 1, Math.Max(1, _config.MaxConcurrency));
            AddEvent("info", "configuration applied");
            return new ServiceCommandResponse { Success = true, Message = "configuration applied" };
        }
    }

    public ServiceCommandResponse ResetConfig()
    {
        lock (_lock)
        {
            _config = new LiteConfig();
            PersistConfig();
            AddEvent("info", "configuration reset to defaults");
            return new ServiceCommandResponse { Success = true, Message = "defaults restored" };
        }
    }

    public ServiceCommandResponse Start()
    {
        lock (_lock)
        {
            if (_running)
            {
                return new ServiceCommandResponse { Success = true, Message = "daemon already running" };
            }

            _running = true;
            _startedAtUtc = DateTimeOffset.UtcNow;
            AddEvent("info", "daemon started");
            return new ServiceCommandResponse { Success = true, Message = "daemon started" };
        }
    }

    public ServiceCommandResponse Stop()
    {
        lock (_lock)
        {
            if (!_running)
            {
                return new ServiceCommandResponse { Success = true, Message = "daemon already stopped" };
            }

            _running = false;
            _activeCalls = 0;
            _interactiveQueueDepth = 0;
            _backgroundQueueDepth = 0;
            _effectiveConcurrency = 0;
            _lastThrottleReason = "daemon stopped";
            AddEvent("warn", "daemon stopped");
            return new ServiceCommandResponse { Success = true, Message = "daemon stopped" };
        }
    }

    public ServiceCommandResponse Restart()
    {
        lock (_lock)
        {
            _running = true;
            _startedAtUtc = DateTimeOffset.UtcNow;
            _lastThrottleReason = "none";
            AddEvent("info", "daemon restarted");
            return new ServiceCommandResponse { Success = true, Message = "daemon restarted" };
        }
    }

    public EventTailResponse GetEvents(int n)
    {
        lock (_lock)
        {
            var limit = Math.Clamp(n, 1, 1000);
            var items = _events.TakeLast(limit).ToList();
            return new EventTailResponse { Events = items };
        }
    }

    public DiagnosticsExportResponse ExportDiagnostics()
    {
        lock (_lock)
        {
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
            var outputPath = Path.Combine(_diagnosticsDirectory, $"leasegatelite-diagnostics-{timestamp}.json");
            var payload = new
            {
                generatedAtUtc = DateTimeOffset.UtcNow,
                status = GetStatus(),
                config = _config,
                events = _events.TakeLast(200).ToList()
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(outputPath, json);

            var bytes = new FileInfo(outputPath).Length;
            AddEvent("info", $"diagnostics exported: {outputPath}");

            return new DiagnosticsExportResponse
            {
                Exported = true,
                OutputPath = outputPath,
                BytesWritten = bytes,
                Message = "diagnostics exported"
            };
        }
    }

    private void SimulateTick()
    {
        if (!_running)
        {
            return;
        }

        _cpuPercent = Math.Clamp(_cpuPercent + _random.Next(-8, 9), 18, 98);
        _availableRamPercent = Math.Clamp(_availableRamPercent + _random.Next(-7, 8), 8, 95);

        if (_cpuPercent == 0)
        {
            _cpuPercent = _random.Next(28, 65);
        }

        if (_availableRamPercent == 0)
        {
            _availableRamPercent = _random.Next(35, 80);
        }

        _activeCalls = Math.Clamp(_activeCalls + _random.Next(-2, 3), 0, _config.MaxConcurrency + 8);
        _interactiveQueueDepth = Math.Clamp(_interactiveQueueDepth + _random.Next(-2, 3), 0, 25);
        _backgroundQueueDepth = Math.Clamp(_backgroundQueueDepth + _random.Next(-2, 4), 0, 40);

        var pressure = Math.Max(_cpuPercent, 100 - _availableRamPercent);
        var target = _config.MaxConcurrency;

        if (pressure >= _config.HardThresholdPercent)
        {
            target = Math.Max(1, _config.MaxConcurrency / 2);
            _adaptiveClampActive = true;
            _lastThrottleReason = "clamped due to high CPU pressure";
        }
        else if (pressure >= _config.SoftThresholdPercent)
        {
            target = Math.Max(1, _config.MaxConcurrency - Math.Max(1, _config.MaxConcurrency / 3));
            _adaptiveClampActive = true;
            _lastThrottleReason = "soft throttle due to pressure";
        }
        else
        {
            _adaptiveClampActive = false;
            _lastThrottleReason = "none";
        }

        var smoothingFactor = Math.Clamp(_config.SmoothingPercent, 5, 95) / 100.0;
        _effectiveConcurrency = (int)Math.Round((_effectiveConcurrency * smoothingFactor) + (target * (1.0 - smoothingFactor)));
        _effectiveConcurrency = Math.Clamp(_effectiveConcurrency, _running ? 1 : 0, _config.MaxConcurrency);
    }

    private HeatState DeriveHeatState()
    {
        var pressure = Math.Max(_cpuPercent, 100 - _availableRamPercent);
        if (pressure >= _config.HardThresholdPercent)
        {
            return HeatState.Spicy;
        }

        if (pressure >= _config.SoftThresholdPercent)
        {
            return HeatState.Warm;
        }

        return HeatState.Calm;
    }

    private void PersistConfig()
    {
        var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }

    private LiteConfig? LoadConfig()
    {
        if (!File.Exists(_configPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<LiteConfig>(json);
        }
        catch
        {
            return null;
        }
    }

    private static LiteConfig Clone(LiteConfig config)
    {
        return new LiteConfig
        {
            MaxConcurrency = config.MaxConcurrency,
            InteractiveReserve = config.InteractiveReserve,
            BackgroundCap = config.BackgroundCap,
            CooldownBehavior = config.CooldownBehavior,
            SoftThresholdPercent = config.SoftThresholdPercent,
            HardThresholdPercent = config.HardThresholdPercent,
            RecoveryRatePercent = config.RecoveryRatePercent,
            SmoothingPercent = config.SmoothingPercent,
            MaxOutputTokensClamp = config.MaxOutputTokensClamp,
            MaxPromptTokensClamp = config.MaxPromptTokensClamp,
            OverflowBehavior = config.OverflowBehavior,
            MaxRetries = config.MaxRetries,
            RetryBackoffMs = config.RetryBackoffMs,
            RequestsPerMinute = config.RequestsPerMinute,
            TokensPerMinute = config.TokensPerMinute,
            BurstAllowance = config.BurstAllowance
        };
    }

    private void AddEvent(string level, string message)
    {
        _events.Add(new EventEntry
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Level = level,
            Message = message
        });

        if (_events.Count > 2000)
        {
            _events.RemoveRange(0, _events.Count - 2000);
        }
    }
}