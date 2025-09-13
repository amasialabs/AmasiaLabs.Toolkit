namespace AmasiaLabs.Toolkit.FlowflakeId;

/// <summary>
/// Controls how time is interpreted when computing Flowflake timestamps.
/// </summary>
public enum FlowflakeTimeSemantics
{
    /// <summary>
    /// Normalize both the Epoch and input time to UTC and compute seconds in UTC. Recommended for services and gRPC.
    /// </summary>
    UtcNormalized = 0,

    /// <summary>
    /// Legacy mode: compute seconds as a raw DateTime difference without normalizing to UTC.
    /// Preserves historical behavior with an Unspecified epoch and DateTime.Now.
    /// </summary>
    LegacyUnspecifiedEpoch = 1
}

