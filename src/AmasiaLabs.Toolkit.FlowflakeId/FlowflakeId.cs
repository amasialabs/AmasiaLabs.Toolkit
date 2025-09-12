using Microsoft.Extensions.Options;

namespace AmasiaLabs.Toolkit.FlowflakeId;

/// <summary>
/// Snowflake-like generator.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public sealed partial class FlowflakeId(
    IOptions<FlowflakeIdOptions> options, 
    IBase62Codec base62,
    TimeProvider timeProvider) : IFlowflakeId
{
    private const int MinValue = 1;
    private const int MaxValue = 2_097_151; // 2^21 - 1 (legacy cap; leaves one spare bit in [0..21])

    private readonly DateTime _startingPoint = options.Value.Epoch;
    private readonly bool _useUtc = options.Value.UseUtcNow;
    private readonly int _instanceId = options.Value.InstanceId;
    private readonly int? _failoverInstanceId = options.Value.FailoverInstanceId;

    private readonly Lock _sync = new();
    private int _sequenceId = MinValue;
    private long _lastSeconds = -1L;

    public long Generate()
    {
        var now = timeProvider.GetUtcNow();
        var date = _useUtc ? now.UtcDateTime : now.LocalDateTime;
        return GenerateForDate(date);
    }

    public long GenerateForDate(DateTime date)
    {
        var seconds = (long)date.Subtract(_startingPoint).TotalSeconds;
        var last = Volatile.Read(ref _lastSeconds);
        var useFailover = _failoverInstanceId.HasValue && seconds < last;

        var index = Interlocked.Increment(ref _sequenceId);

        if (index > MaxValue)
        {
            lock (_sync)
            {
                if (_sequenceId > MaxValue)
                {
                    Interlocked.Exchange(ref _sequenceId, MinValue);
                }

                index = Interlocked.Increment(ref _sequenceId);
            }
        }

        // Advance last-seen seconds if needed (lock-free max update)
        UpdateLastSeconds(seconds);

        var id = seconds << 31;
        var instance = useFailover ? _failoverInstanceId!.Value : _instanceId;
        id += (long)instance << 22;
        id += index;

        return id;
    }

    public int GetInstanceId() => _instanceId;

    public DateTime GetDateTime(long id) => _startingPoint.Add(TimeSpan.FromSeconds(id >> 31));

    public int GetInstanceIdFromGlobalId(long id) => (int)((id - (id >> 31 << 31)) >> 22);

    public string ToBase62(long id) => base62.Encode(id);

    public long FromBase62(string id) => base62.Decode(id);

    private void UpdateLastSeconds(long seconds)
    {
        var spinner = new SpinWait();
        while (true)
        {
            var observed = Volatile.Read(ref _lastSeconds);
            if (seconds <= observed) 
                break;
            
            if (Interlocked.CompareExchange(ref _lastSeconds, seconds, observed) == observed) 
                break;
            
            spinner.SpinOnce();
        }
    }
}

public sealed partial class FlowflakeId
{
    // Convenience ctor for manual usage without DI: uses TimeProvider.System
    public FlowflakeId(IOptions<FlowflakeIdOptions> options, IBase62Codec base62)
        : this(options, base62, TimeProvider.System)
    {
    }
}
