using AmasiaLabs.Toolkit.FlowflakeId.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.FlowflakeId.Grpc.Hosting;

public static class ServerBootstrap
{
    public static void AddFlowflakeServer(this WebApplicationBuilder builder, IConfiguration? cfg = null)
    {
        var configuration = cfg ?? builder.Configuration;

        builder.Services.AddFlowflakeId(configuration);
        builder.Services.AddGrpc();

        builder.Services.AddOptionsWithValidateOnStart<FlowflakeIdServerOptions>()
            .Bind(configuration.GetSection(FlowflakeIdServerOptions.DefaultSectionPath))
            .ValidateDataAnnotations();
    }

    public static void MapFlowflakeServer(this WebApplication app)
    {
        app.MapFlowflakeIdGrpc();
    }
}
