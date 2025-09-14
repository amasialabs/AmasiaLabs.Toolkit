using System.ComponentModel.DataAnnotations;

namespace AmasiaLabs.Toolkit.FlowflakeId.Grpc;

/// <summary>
/// Server-side options for the Flowflake gRPC service.
/// </summary>
public sealed class FlowflakeIdServerOptions
{
    public const string DefaultSectionPath = "Amasia:Toolkit:FlowflakeId:Grpc";
    public const int DefaultMaxBatchSize = 10_000;

    /// <summary>
    /// Maximum number of IDs that can be requested in a single batch call.
    /// </summary>
    [Range(1, 2_00_000)]
    public int MaxBatchSize { get; init; } = DefaultMaxBatchSize;
}

