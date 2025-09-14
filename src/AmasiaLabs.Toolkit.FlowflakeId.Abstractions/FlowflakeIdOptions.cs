using System.ComponentModel.DataAnnotations;

namespace AmasiaLabs.Toolkit.FlowflakeId.Abstractions;

/// <summary>
/// Options for Flowflake ID generation.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class FlowflakeIdOptions
{
    /// <summary>
    /// Default configuration section path used by AddFlowflakeId(IConfiguration).
    /// </summary>
    public const string DefaultSectionPath = "Amasia:Toolkit:FlowflakeId";

    /// <summary>
    /// Unique instance/node identifier. Must be in range [1..511].
    /// </summary>
    [Required]
    [Range(1, 511, ErrorMessage = "InstanceId must be between 1 and 511")]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public required int InstanceId { get; init; }

    /// <summary>
    /// Use DateTime.UtcNow as the default time source when calling Generate().
    /// </summary>
    public bool UseUtcNow { get; init; } = true;

    /// <summary>
    /// Epoch (starting point) for timestamp calculations. Must be explicitly set by the application.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public required DateTime Epoch { get; init; }

    /// <summary>
    /// Optional failover instance id used when the system clock moves backward relatively to the last seen timestamp.
    /// When set, IDs generated during a clock rollback will use this instance id to avoid collisions across time windows.
    /// Must be in [1..511] and different from <see cref="InstanceId"/>.
    /// </summary>
    [Range(1, 511, ErrorMessage = "FailoverInstanceId must be between 1 and 511 if set")]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public int? FailoverInstanceId { get; init; }

    /// <summary>
    /// Time semantics for computing seconds from an epoch.
    /// Default is UTC-normalized for stable distributed behavior.
    /// Set to <see cref="FlowflakeTimeSemantics.LegacyUnspecifiedEpoch"/> to preserve historical behavior based on DateTime difference without UTC normalization.
    /// </summary>
    public FlowflakeTimeSemantics TimeSemantics { get; init; } = FlowflakeTimeSemantics.UtcNormalized;
}
