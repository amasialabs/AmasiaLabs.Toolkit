using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.FlowflakeId.Grpc.Client;

public static class FlowflakeGrpcClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers gRPC-based FlowflakeId client and binds options from configuration.
    /// </summary>
    public static IServiceCollection AddFlowflakeIdGrpcClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionPath = null,
        Action<FlowflakeIdRpcOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var path = sectionPath ?? FlowflakeIdRpcOptions.DefaultSectionPath;
        var section = configuration.GetSection(path);
        return services.AddFlowflakeIdGrpcClient(section, configure);
    }

    /// <summary>
    /// Registers gRPC-based FlowflakeId client with an options section.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static IServiceCollection AddFlowflakeIdGrpcClient(
        this IServiceCollection services,
        IConfigurationSection section,
        Action<FlowflakeIdRpcOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(section);

        var optionsBuilder = services
            .AddOptionsWithValidateOnStart<FlowflakeIdRpcOptions>()
            .Bind(section)
            .ValidateDataAnnotations();

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        // gRPC client-based IFlowflakeId
        services.AddSingleton<IFlowflakeId, FlowflakeGrpcClient>();
        return services;
    }
}
