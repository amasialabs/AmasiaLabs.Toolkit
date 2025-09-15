using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;

namespace AmasiaLabs.Toolkit.FlowflakeId.Extensions;

/// <summary>
/// Extension methods for parsing and extracting information from Flowflake IDs.
/// </summary>
public static class FlowflakeIdParsingExtensions
{
    /// <summary>
    /// Extracts the instance ID from a Flowflake ID.
    /// </summary>
    /// <param name="id">The Flowflake ID.</param>
    /// <returns>The instance ID encoded in the ID.</returns>
    public static int GetInstanceIdFromFlowflakeId(this long id)
        => (int)((ulong)id >> (int)FlowflakeLayout.Default.InstanceShift & FlowflakeLayout.Default.InstanceMask);

    /// <summary>
    /// Extracts the sequence number from a Flowflake ID.
    /// </summary>
    /// <param name="id">The Flowflake ID.</param>
    /// <returns>The sequence number encoded in the ID.</returns>
    public static int GetSequenceNumberFromFlowflakeId(this long id)
        => (int)((ulong)id & FlowflakeLayout.Default.SequenceMask);

    /// <summary>
    /// Extracts the timestamp (seconds since epoch) from a Flowflake ID.
    /// </summary>
    /// <param name="id">The Flowflake ID.</param>
    /// <returns>The timestamp in seconds since epoch.</returns>
    public static long GetTimestampFromFlowflakeId(this long id)
        => (long)((ulong)id >> (int)FlowflakeLayout.Default.TimestampShift);

    /// <summary>
    /// Decodes the DateTime from a Flowflake ID using the provided clock (epoch + semantics).
    /// </summary>
    /// <param name="id">The Flowflake ID.</param>
    /// <param name="clock">The clock configuration containing epoch and time semantics.</param>
    /// <returns>The DateTime decoded from the ID.</returns>
    public static DateTime GetDateTimeFromFlowflakeId(this long id, in FlowflakeClock clock)
        => clock.NormalizedEpoch.AddSeconds(id.GetTimestampFromFlowflakeId());

    /// <summary>
    /// Decodes the DateTime from a Flowflake ID using explicit epoch and semantics.
    /// </summary>
    /// <param name="id">The Flowflake ID.</param>
    /// <param name="epoch">The epoch date.</param>
    /// <param name="semantics">The time semantics to use.</param>
    /// <returns>The DateTime decoded from the ID.</returns>
    public static DateTime GetDateTimeFromFlowflakeId(this long id, DateTime epoch, FlowflakeTimeSemantics semantics)
        => id.GetDateTimeFromFlowflakeId(new FlowflakeClock(epoch, semantics));

    /// <summary>
    /// Decodes the DateTime from a Flowflake ID using options (epoch + semantics).
    /// </summary>
    /// <param name="id">The Flowflake ID.</param>
    /// <param name="options">The options containing epoch and time semantics.</param>
    /// <returns>The DateTime decoded from the ID.</returns>
    public static DateTime GetDateTimeFromFlowflakeId(this long id, FlowflakeIdOptions options)
        => id.GetDateTimeFromFlowflakeId(options.FlowflakeClock.Epoch, options.FlowflakeClock.TimeSemantics);

    /// <summary>
    /// Decodes the DateTime from a Flowflake ID using clock options.
    /// </summary>
    /// <param name="id">The Flowflake ID.</param>
    /// <param name="clockOptions">The clock options containing epoch and time semantics.</param>
    /// <returns>The DateTime decoded from the ID.</returns>
    public static DateTime GetDateTimeFromFlowflakeId(this long id, FlowflakeClockOptions clockOptions)
        => id.GetDateTimeFromFlowflakeId(clockOptions.Epoch, clockOptions.TimeSemantics);
}