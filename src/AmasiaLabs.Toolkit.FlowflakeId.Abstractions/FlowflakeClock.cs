namespace AmasiaLabs.Toolkit.FlowflakeId.Abstractions;

/// <summary>
/// Represents the time-related configuration for Flowflake ID operations.
/// </summary>
public readonly record struct FlowflakeClock(DateTime Epoch, FlowflakeTimeSemantics Semantics)
{
    /// <summary>
    /// Gets the normalized epoch based on the time semantics.
    /// </summary>
    public DateTime NormalizedEpoch =>
        Semantics == FlowflakeTimeSemantics.UtcNormalized
            ? EnsureUtc(Epoch)
            : DateTime.SpecifyKind(Epoch, DateTimeKind.Unspecified);

    private static DateTime EnsureUtc(DateTime dt) => dt.Kind switch
    {
        DateTimeKind.Utc => dt,
        DateTimeKind.Local => dt.ToUniversalTime(),
        _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
    };
}