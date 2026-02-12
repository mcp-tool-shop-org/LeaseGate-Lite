namespace LeaseGateLite.Contracts;

public enum LeaseGateLiteMode
{
    Embedded,
    Daemon
}

public enum HeatState
{
    Calm,
    Warm,
    Spicy
}

public enum CooldownBehavior
{
    Off,
    Mild,
    Aggressive
}

public enum OverflowBehavior
{
    TrimOldest,
    Deny,
    QueueOnly
}

public enum ThrottleReason
{
    None,
    CpuPressure,
    MemoryPressure,
    Cooldown,
    RateLimit,
    ManualClamp
}

public enum EventCategory
{
    Lease,
    Pressure,
    Config,
    Service,
    Diagnostics,
    Preset,
    Audit
}

public enum PressureMode
{
    Normal,
    Spiky
}
