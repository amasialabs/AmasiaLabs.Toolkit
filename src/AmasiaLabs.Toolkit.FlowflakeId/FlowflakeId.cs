using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;
using Microsoft.Extensions.Options;

namespace AmasiaLabs.Toolkit.FlowflakeId;

/// <summary>
/// Snowflake-like generator.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public sealed partial class FlowflakeId(
    IOptions<FlowflakeIdOptions> options,
    TimeProvider timeProvider) : IFlowflakeId
{
    private static readonly int SequenceMin = FlowflakeLayout.Default.SequenceMin;
    private static readonly int SequenceMax = FlowflakeLayout.Default.SequenceMax;

    private readonly DateTime _epochUtc = EnsureUtc(options.Value.FlowflakeClock.Epoch);
    private readonly bool _useUtc = options.Value.UseUtcNow;
    private readonly FlowflakeTimeSemantics _semantics = options.Value.FlowflakeClock.TimeSemantics;
    private readonly DateTime _epochRaw = options.Value.FlowflakeClock.Epoch;
    private readonly int _instanceId = options.Value.InstanceId;
    private readonly int? _failoverInstanceId = options.Value.FailoverInstanceId;

    private readonly Lock _sync = new();
    private int _sequenceId = SequenceMin;
    private long _lastSeconds = -1L;

    // ReSharper disable once MemberCanBePrivate.Global
    public long Generate()
    {
        var now = timeProvider.GetUtcNow();
        var date = _useUtc ? now.UtcDateTime : now.LocalDateTime;
        return GenerateForDate(date);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public long GenerateForDate(DateTime date)
    {
        long seconds = (_semantics == FlowflakeTimeSemantics.LegacyUnspecifiedEpoch
            ? (DateTime.SpecifyKind(date, DateTimeKind.Unspecified) - DateTime.SpecifyKind(_epochRaw, DateTimeKind.Unspecified)).Ticks
            : (EnsureUtc(date) - _epochUtc).Ticks) / TimeSpan.TicksPerSecond;
        
        var last = Volatile.Read(ref _lastSeconds);
        var useFailover = _failoverInstanceId.HasValue && seconds < last;

        var index = Interlocked.Increment(ref _sequenceId);

        if (index > SequenceMax)
        {
            lock (_sync)
            {
                if (_sequenceId > SequenceMax)
                {
                    Interlocked.Exchange(ref _sequenceId, SequenceMin);
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

    // ReSharper disable once MemberCanBePrivate.Global
    public long[] GenerateBatch(int size)
    {
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        var result = new long[size];
        for (var i = 0; i < size; i++)
        {
            result[i] = Generate();
        }
        return result;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public long[] GenerateBatchForDate(DateTime date, int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        
        var result = new long[size];
        for (var i = 0; i < size; i++)
        {
            result[i] = GenerateForDate(date);
        }
        return result;
    }

    public int InstanceId => _instanceId;

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

    private static DateTime EnsureUtc(DateTime dt)
        => dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
        };
}

public sealed partial class FlowflakeId
{
    // Convenience ctor for manual usage without DI: uses TimeProvider.System
    public FlowflakeId(IOptions<FlowflakeIdOptions> options)
        : this(options, TimeProvider.System)
    {
    }

    // Async interface implementation
    public ValueTask<long> GenerateAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Generate());

    public ValueTask<long> GenerateForDateAsync(DateTime date, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(GenerateForDate(date));

    public ValueTask<long[]> GenerateBatchAsync(int size, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(GenerateBatch(size));

    public ValueTask<long[]> GenerateBatchForDateAsync(DateTime date, int size, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(GenerateBatchForDate(date, size));
}
