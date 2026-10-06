using System.Text.Json.Serialization;

namespace LeaseGateLite.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LeaseGateLiteMode
{
    Embedded,
    Daemon
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HeatState
{
    Calm,
    Warm,
    Spicy
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CooldownBehavior
{
    Off,
    Mild,
    Aggressive
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OverflowBehavior
{
    TrimOldest,
    Deny,
    QueueOnly
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ThrottleReason
{
    None,
    CpuPressure,
    MemoryPressure,
    Cooldown,
    RateLimit,
    ManualClamp
}

[JsonConverter(typeof(JsonStringEnumConverter))]
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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PressureMode
{
    Normal,
    Spiky
}
