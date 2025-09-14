using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace AmasiaLabs.Toolkit.FlowflakeId.Grpc.Services;

public sealed class FlowflakeIdsService(
    IFlowflakeId ids,
    IOptions<FlowflakeIdOptions> options,
    IOptions<FlowflakeIdServerOptions>? serverOptions = null) : FlowflakeIds.FlowflakeIdsBase
{
    private readonly FlowflakeIdOptions _opts = options.Value;
    private readonly int _maxBatch = (serverOptions?.Value?.MaxBatchSize).GetValueOrDefault(FlowflakeIdServerOptions.DefaultMaxBatchSize);

    public override async Task<IdResponse> GetId(Empty request, ServerCallContext context)
    {
        var id = await ids.GenerateAsync(context.CancellationToken);
        return new IdResponse { Id = id };
    }

    public override async Task<IdResponse> GetIdForDate(DateRequest request, ServerCallContext context)
    {
        if (request.Timestamp is null)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "timestamp required"));

        // Normalize to UTC irrespective of incoming DateTime.Kind
        var dto = request.Timestamp.ToDateTimeOffset();
        var dtUtc = dto.UtcDateTime;

        var id = await ids.GenerateForDateAsync(dtUtc, context.CancellationToken);
        return new IdResponse { Id = id };
    }

    public override async Task<BatchResponse> GetBatch(BatchRequest request, ServerCallContext context)
    {
        var size = request.Size;
        if (size <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "size must be > 0"));

        // Guardrail: prevent unbounded allocations
        if (size > _maxBatch)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"size too large (max {_maxBatch})"));

        var arr = await ids.GenerateBatchAsync(size, context.CancellationToken);
        var resp = new BatchResponse();
        resp.Ids.AddRange(arr);
        return resp;
    }

    public override Task<ServerInfo> GetServerInfo(Empty request, ServerCallContext context)
    {
        var epoch = _opts.Epoch.Kind switch
        {
            DateTimeKind.Utc => _opts.Epoch,
            DateTimeKind.Local => _opts.Epoch.ToUniversalTime(),
            _ => DateTime.SpecifyKind(_opts.Epoch, DateTimeKind.Utc)
        };
        var layout = FlowflakeLayout.Default;
        var info = new ServerInfo
        {
            Epoch = Timestamp.FromDateTime(epoch),
            UseUtcNow = _opts.UseUtcNow,
            InstanceId = _opts.InstanceId,
            FailoverInstanceId = _opts.FailoverInstanceId ?? 0,
            TimestampShift = layout.TimestampShift,
            InstanceShift = layout.InstanceShift,
            SequenceMask = layout.SequenceMask,
            InstanceMask = layout.InstanceMask
        };
        return Task.FromResult(info);
    }
}
