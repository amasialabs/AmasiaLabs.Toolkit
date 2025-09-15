using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AmasiaLabs.Toolkit.FlowflakeId.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Flowflake clock configuration without ID generation capabilities.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="sectionPath">The configuration section path. Defaults to "Amasia:Toolkit:FlowflakeId:FlowflakeClock".</param>
    public static IServiceCollection AddFlowflakeClock(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionPath = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var path = sectionPath ?? $"{FlowflakeIdOptions.DefaultSectionPath}:FlowflakeClock";

        services
            .AddOptionsWithValidateOnStart<FlowflakeClockOptions>()
            .Bind(configuration.GetSection(path))
            .ValidateDataAnnotations();

        return services;
    }

    /// <summary>
    /// Registers Flowflake ID services and binds options from a configuration section.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static IServiceCollection AddFlowflakeId(
        this IServiceCollection services,
        IConfigurationSection section,
        Action<FlowflakeIdOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(section);

        var optionsBuilder = services
            .AddOptionsWithValidateOnStart<FlowflakeIdOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .Validate(static o => o.FlowflakeClock.Epoch > DateTime.MinValue, "FlowflakeId: FlowflakeClock.Epoch must be set")
            .Validate(static o => o.FailoverInstanceId is null || o.FailoverInstanceId.Value != o.InstanceId,
                "FlowflakeId: FailoverInstanceId must differ from InstanceId");

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IFlowflakeId, FlowflakeId>();
        return services;
    }

    /// <summary>
    /// Registers Flowflake ID using configuration root and a section path.
    /// If <paramref name="sectionPath"/> is null, uses the default path "Amasia:Toolkit:FlowflakeId".
    /// </summary>
    public static IServiceCollection AddFlowflakeId(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionPath = null,
        Action<FlowflakeIdOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var path = sectionPath ?? FlowflakeIdOptions.DefaultSectionPath;
        var section = configuration.GetSection(path);
        return services.AddFlowflakeId(section, configure);
    }

    /// <summary>
    /// Registers Flowflake ID using only code-based configuration.
    /// </summary>
    public static IServiceCollection AddFlowflakeId(
        this IServiceCollection services,
        Action<FlowflakeIdOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services
            .AddOptionsWithValidateOnStart<FlowflakeIdOptions>()
            .Configure(configure)
            .ValidateDataAnnotations()
            .Validate(static o => o.FlowflakeClock.Epoch > DateTime.MinValue, "FlowflakeId: FlowflakeClock.Epoch must be set")
            .Validate(static o => o.FailoverInstanceId is null || o.FailoverInstanceId.Value != o.InstanceId,
                "FlowflakeId: FailoverInstanceId must differ from InstanceId");

        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IFlowflakeId, FlowflakeId>();
        return services;
    }
}
