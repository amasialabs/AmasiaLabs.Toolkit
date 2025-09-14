namespace AmasiaLabs.Toolkit.FlowflakeId;

/// <summary>
/// Generates globally unique, time-ordered identifiers.
/// </summary>
public interface IFlowflakeId
{
    ValueTask<long> GenerateAsync(CancellationToken cancellationToken = default);
    ValueTask<long> GenerateForDateAsync(DateTime date, CancellationToken cancellationToken = default);
    /// <summary>
    /// Generates a batch of IDs using the default time source.
    /// </summary>
    /// <param name="size">Number of IDs to generate. Must be > 0.</param>
    ValueTask<long[]> GenerateBatchAsync(int size, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a batch of IDs for the specified timestamp.
    /// </summary>
    /// <param name="date">Timestamp to use for ID generation.</param>
    /// <param name="size">Number of IDs to generate. Must be > 0.</param>
    ValueTask<long[]> GenerateBatchForDateAsync(DateTime date, int size, CancellationToken cancellationToken = default);
    int GetInstanceId();
    int GetInstanceIdFromGlobalId(long id);
    DateTime GetDateTime(long id);
}
