using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.FlowflakeId.Grpc.Client;

public static class FlowflakeIdGrpcClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers gRPC-based FlowflakeId client and binds options from configuration.
    /// </summary>
    public static IServiceCollection AddFlowflakeIdGrpcClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionPath = null,
        Action<FlowflakeIdGrpcClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var path = sectionPath ?? FlowflakeIdGrpcClientOptions.DefaultSectionPath;
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
        Action<FlowflakeIdGrpcClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(section);

        var optionsBuilder = services
            .AddOptionsWithValidateOnStart<FlowflakeIdGrpcClientOptions>()
            .Bind(section)
            .ValidateDataAnnotations();

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        // gRPC client-based IFlowflakeId
        services.AddSingleton<IFlowflakeId, FlowflakeIdGrpcClient>();
        return services;
    }
}
