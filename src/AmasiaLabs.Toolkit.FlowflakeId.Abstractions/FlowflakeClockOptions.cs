using System.ComponentModel.DataAnnotations;

namespace AmasiaLabs.Toolkit.FlowflakeId.Abstractions;

/// <summary>
/// Options for Flowflake clock configuration.
/// </summary>
public sealed class FlowflakeClockOptions
{
    /// <summary>
    /// Epoch (starting point) for timestamp calculations. Must be explicitly set by the application.
    /// </summary>
    [Required]
    public required DateTime Epoch { get; init; }

    /// <summary>
    /// Time semantics for computing seconds from an epoch.
    /// Default is UTC-normalized for stable distributed behavior.
    /// Set to <see cref="FlowflakeTimeSemantics.LegacyUnspecifiedEpoch"/> to preserve historical behavior based on DateTime difference without UTC normalization.
    /// </summary>
    public FlowflakeTimeSemantics TimeSemantics { get; init; } = FlowflakeTimeSemantics.UtcNormalized;
}