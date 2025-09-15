using System.ComponentModel.DataAnnotations;

namespace AmasiaLabs.Toolkit.FlowflakeId.Grpc.Client;

/// <summary>
/// Options for Flowflake gRPC client SDK.
/// </summary>
public sealed class FlowflakeIdGrpcClientOptions
{
    /// <summary>
    /// Default configuration section path.
    /// </summary>
    public const string DefaultSectionPath = "Amasia:Toolkit:FlowflakeId:Grpc:Client";

    /// <summary>
    /// List of server addresses (e.g., http://host:port or https://host:port).
    /// </summary>
    [Required]
    [MinLength(1)]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public required string[] Addresses { get; init; }

    /// <summary>
    /// Per-RPC deadline in milliseconds.
    /// </summary>
    [Range(50, 60000)]
    public int DeadlineMs { get; init; } = 200;

    /// <summary>
    /// Max failover attempts across addresses (inclusive of the first attempt).
    /// </summary>
    [Range(1, 10)]
    public int MaxAttempts { get; init; } = 2;
}

