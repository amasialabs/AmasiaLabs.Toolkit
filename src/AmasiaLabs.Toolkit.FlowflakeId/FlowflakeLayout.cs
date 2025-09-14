namespace AmasiaLabs.Toolkit.FlowflakeId;

/// <summary>
/// Immutable description of the Flowflake bit layout and ranges.
/// Centralizes shifts/masks so other packages (e.g., gRPC) don't duplicate them.
/// </summary>
public sealed record FlowflakeLayout(
    uint TimestampShift,
    uint InstanceShift,
    ulong SequenceMask,
    ulong InstanceMask,
    int SequenceMin,
    int SequenceMax)
{
    public static readonly FlowflakeLayout Default = new(
        TimestampShift: 31,
        InstanceShift: 22,
        SequenceMask: (1UL << 22) - 1UL, // 0x3F_FFFF
        InstanceMask: 0x1FF,            // 511
        SequenceMin: 1,
        SequenceMax: 4_194_303          // 2^22 - 1
    );
}
