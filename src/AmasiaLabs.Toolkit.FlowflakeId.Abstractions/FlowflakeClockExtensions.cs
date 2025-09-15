namespace AmasiaLabs.Toolkit.FlowflakeId.Abstractions;

/// <summary>
/// Extension methods for FlowflakeClock conversions.
/// </summary>
public static class FlowflakeClockExtensions
{
    /// <summary>
    /// Converts FlowflakeClockOptions to FlowflakeClock.
    /// </summary>
    public static FlowflakeClock ToFlowflakeClock(this FlowflakeClockOptions options)
        => new(options.Epoch, options.TimeSemantics);

    /// <summary>
    /// Converts FlowflakeIdOptions to FlowflakeClock.
    /// </summary>
    public static FlowflakeClock ToFlowflakeClock(this FlowflakeIdOptions options)
        => new(options.FlowflakeClock.Epoch, options.FlowflakeClock.TimeSemantics);
}