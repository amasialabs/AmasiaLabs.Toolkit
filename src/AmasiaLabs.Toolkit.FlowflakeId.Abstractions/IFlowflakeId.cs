namespace AmasiaLabs.Toolkit.FlowflakeId.Abstractions;

/// <summary>
/// Generates globally unique, time-ordered identifiers.
/// </summary>
public interface IFlowflakeId
{
    /// <summary>
    /// Generates a single ID using the default time source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<long> GenerateAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Generates a single ID for the specified timestamp.
    /// </summary>
    /// <param name="date">Timestamp to use for ID generation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<long> GenerateForDateAsync(DateTime date, CancellationToken cancellationToken = default);
    /// <summary>
    /// Generates a batch of IDs using the default time source.
    /// </summary>
    /// <param name="size">Number of IDs to generate. Must be > 0.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<long[]> GenerateBatchAsync(int size, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a batch of IDs for the specified timestamp.
    /// </summary>
    /// <param name="date">Timestamp to use for ID generation.</param>
    /// <param name="size">Number of IDs to generate. Must be > 0.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<long[]> GenerateBatchForDateAsync(DateTime date, int size, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets the instance ID of this generator.
    /// </summary>
    int GetInstanceId();

    /// <summary>
    /// Extracts the instance ID from a generated ID.
    /// </summary>
    /// <param name="id">The generated ID.</param>
    int GetInstanceIdFromFlowflakeId(long id);

    /// <summary>
    /// Extracts the timestamp from a generated ID.
    /// </summary>
    /// <param name="id">The generated ID.</param>
    DateTime GetDateTime(long id);
}
