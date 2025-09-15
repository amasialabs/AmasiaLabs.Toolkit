using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;
using System.Collections.Concurrent;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;

namespace AmasiaLabs.Toolkit.FlowflakeId.Grpc.Client;

/// <summary>
/// gRPC-based implementation of <see cref="IFlowflakeId"/>.
/// Fetches and caches ServerInfo, performs failover across configured addresses,
/// and decodes timestamps/instance locally.
/// </summary>
public sealed class FlowflakeIdGrpcClient(
    IOptions<FlowflakeIdGrpcClientOptions> options)
    : IFlowflakeId
{
    private readonly FlowflakeIdGrpcClientOptions _opts = options.Value;

    private readonly ConcurrentDictionary<string, FlowflakeIds.FlowflakeIdsClient> _clients = new();
    private int _addrIndex;

    private volatile ServerInfo? _serverInfo;
    private long _epochUtcTicks; // access via Volatile.Read/Write
    private volatile FlowflakeLayout _layout = FlowflakeLayout.Default;

    public async ValueTask<long> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var resp = await CallAsync(static (c, co) => c.GetIdAsync(new Empty(), co), cancellationToken);
        return resp.Id;
    }

    public async ValueTask<long> GenerateForDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var ts = Timestamp.FromDateTime(DateTime.SpecifyKind(date, DateTimeKind.Utc));
        var resp = await CallAsync((c, co) => c.GetIdForDateAsync(new DateRequest { Timestamp = ts }, co), cancellationToken);
        return resp.Id;
    }

    public async ValueTask<long[]> GenerateBatchAsync(int size, CancellationToken cancellationToken = default)
    {
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        var resp = await CallAsync((c, co) => c.GetBatchAsync(new BatchRequest { Size = size }, co), cancellationToken);
        return resp.Ids.ToArray();
    }

    public async ValueTask<long[]> GenerateBatchForDateAsync(DateTime date, int size, CancellationToken cancellationToken = default)
    {
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        // No dedicated RPC yet; fall back to repeated calls.
        var arr = new long[size];
        for (var i = 0; i < size; i++)
        {
            arr[i] = await GenerateForDateAsync(date, cancellationToken);
        }
        return arr;
    }

    public int InstanceId
        => EnsureServerInfoInitialized().InstanceId;

    public DateTime GetDateTime(long id)
    {
        var layout = EnsureServerInfoLayout();
        var seconds = id >> (int)layout.TimestampShift;
        var ticks = Volatile.Read(ref _epochUtcTicks);
        return new DateTime(ticks, DateTimeKind.Utc).AddSeconds(seconds);
    }

    private ServerInfo EnsureServerInfoInitialized()
    {
        var info = _serverInfo;
        if (info is null)
            throw new InvalidOperationException("ServerInfo not initialized. Call any async generation method first.");
        return info;
    }

    private FlowflakeLayout EnsureServerInfoLayout()
    {
        EnsureServerInfoInitialized();
        return _layout;
    }

    private async Task<TResponse> CallAsync<TResponse>(
        Func<FlowflakeIds.FlowflakeIdsClient, CallOptions, AsyncUnaryCall<TResponse>> invoker,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        // Ensure server info (epoch/layout) at least once
        if (_serverInfo is null)
        {
            var fetched = await CallInternalAsync(static (c, co) => c.GetServerInfoAsync(new Empty(), co), cancellationToken);
            InitializeFromServerInfo(fetched);
        }

        return await CallInternalAsync(invoker, cancellationToken);
    }

    private async Task<TResponse> CallInternalAsync<TResponse>(
        Func<FlowflakeIds.FlowflakeIdsClient, CallOptions, AsyncUnaryCall<TResponse>> invoker,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        var addresses = _opts.Addresses;
        var attempts = Math.Min(_opts.MaxAttempts, addresses.Length);
        var startIndex = Math.Abs(Interlocked.Increment(ref _addrIndex)) % addresses.Length;
        RpcException? last = null;
        for (var i = 0; i < attempts; i++)
        {
            var idx = (startIndex + i) % addresses.Length;
            var address = addresses[idx];
            try
            {
                var client = GetClient(address);
                var deadline = DateTime.UtcNow.AddMilliseconds(_opts.DeadlineMs);
                var callOpts = new CallOptions(deadline: deadline, cancellationToken: cancellationToken);
                var call = invoker(client, callOpts);
                return await call.ResponseAsync.ConfigureAwait(false);
            }
            catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
            {
                last = ex;
                // try the next address
            }
        }

        // last attempt (rethrow last error)
        if (last is not null) throw last;
        // Fallback (shouldn't happen): direct call on startIndex
        var fallbackClient = GetClient(addresses[startIndex]);
        var fallbackOpts = new CallOptions(deadline: DateTime.UtcNow.AddMilliseconds(_opts.DeadlineMs), cancellationToken: cancellationToken);
        var fallbackCall = invoker(fallbackClient, fallbackOpts);
        return await fallbackCall.ResponseAsync.ConfigureAwait(false);
    }

    private void InitializeFromServerInfo(ServerInfo fetched)
    {
        var epoch = fetched.Epoch?.ToDateTime() ?? throw new InvalidOperationException("Server epoch missing");
        var epochUtc = epoch.Kind switch
        {
            DateTimeKind.Utc => epoch,
            DateTimeKind.Local => epoch.ToUniversalTime(),
            _ => DateTime.SpecifyKind(epoch, DateTimeKind.Utc)
        };
        Volatile.Write(ref _epochUtcTicks, epochUtc.Ticks);
        _layout = new FlowflakeLayout(
            fetched.TimestampShift,
            fetched.InstanceShift,
            fetched.SequenceMask,
            fetched.InstanceMask,
            FlowflakeLayout.Default.SequenceMin,
            FlowflakeLayout.Default.SequenceMax);

        _serverInfo = fetched;
    }

    private FlowflakeIds.FlowflakeIdsClient GetClient(string address)
    {
        return _clients.GetOrAdd(address, static addr =>
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                KeepAlivePingDelay = TimeSpan.FromSeconds(20),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(5),
                EnableMultipleHttp2Connections = true
            };

            var channel = GrpcChannel.ForAddress(addr, new GrpcChannelOptions
            {
                HttpHandler = handler
            });
            return new FlowflakeIds.FlowflakeIdsClient(channel);
        });
    }
}
