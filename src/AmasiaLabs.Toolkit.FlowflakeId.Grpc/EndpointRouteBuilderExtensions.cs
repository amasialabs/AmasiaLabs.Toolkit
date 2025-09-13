using AmasiaLabs.Toolkit.FlowflakeId.Grpc.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AmasiaLabs.Toolkit.FlowflakeId.Grpc;

public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the Flowflake gRPC service.
    /// </summary>
    public static IEndpointRouteBuilder MapFlowflakeIdGrpc(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGrpcService<FlowflakeIdsService>();
        return endpoints;
    }
}

