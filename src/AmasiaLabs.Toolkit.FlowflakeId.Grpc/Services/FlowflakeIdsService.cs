using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace AmasiaLabs.Toolkit.FlowflakeId.Grpc.Services;

public sealed class FlowflakeIdsService(
    IFlowflakeId ids,
    IOptions<FlowflakeIdOptions> options) : FlowflakeIds.FlowflakeIdsBase
{
    private readonly FlowflakeIdOptions _opts = options.Value;

    public override Task<IdResponse> GetId(Empty request, ServerCallContext context)
        => Task.FromResult(new IdResponse { Id = ids.Generate() });

    public override Task<IdResponse> GetIdForDate(DateRequest request, ServerCallContext context)
    {
        if (request.Timestamp is null)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "timestamp required"));

        // Normalize to UTC irrespective of incoming DateTime.Kind
        var dto = request.Timestamp.ToDateTimeOffset();
        var dtUtc = dto.UtcDateTime;

        var id = ids.GenerateForDate(dtUtc);
        return Task.FromResult(new IdResponse { Id = id });
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
