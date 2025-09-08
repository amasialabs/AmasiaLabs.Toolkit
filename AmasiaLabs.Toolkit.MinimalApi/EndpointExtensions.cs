using System.Reflection;
using Microsoft.AspNetCore.Builder; 
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AmasiaLabs.Toolkit.MinimalApi;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints<TMarker>(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddEndpoints(configuration, typeof(TMarker).Assembly);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static IServiceCollection AddEndpoints(this IServiceCollection services, IConfiguration configuration)
    {
        var asm = ResolveDefaultAssembly();
        foreach (var t in GetEndpointTypes(asm))
            InvokeAddServices(t, services, configuration);
        return services;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static IServiceCollection AddEndpoints(this IServiceCollection services, IConfiguration configuration, Assembly assembly)
    {
        foreach (var t in GetEndpointTypes(assembly))
            InvokeAddServices(t, services, configuration);
        return services;
    }

    public static IHostApplicationBuilder AddEndpoints(this IHostApplicationBuilder builder)
    {
        builder.Services.AddEndpoints(builder.Configuration);
        return builder;
    }

    public static WebApplication UseEndpoints(this WebApplication app)
    {
        ((IEndpointRouteBuilder)app).UseEndpoints(ResolveDefaultAssembly());
        return app;
    }

    public static WebApplication UseEndpoints(this WebApplication app, Assembly assembly)
    {
        ((IEndpointRouteBuilder)app).UseEndpoints(assembly);
        return app;
    }

    public static IEndpointRouteBuilder UseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.UseEndpoints(ResolveDefaultAssembly());
    }

    public static IEndpointRouteBuilder UseEndpoints(this IEndpointRouteBuilder endpoints, Assembly assembly)
    {
        foreach (var t in GetEndpointTypes(assembly))
            InvokeDefineEndpoints(t, endpoints);
        return endpoints;
    }

    private static Assembly ResolveDefaultAssembly()
    {
        return Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
    }

    private static IEnumerable<TypeInfo> GetEndpointTypes(Assembly assembly)
    {
        return assembly.DefinedTypes.Where(x =>
            x is { IsAbstract: false, IsInterface: false } &&
            typeof(IEndpoints).IsAssignableFrom(x));
    }

    private static void InvokeAddServices(TypeInfo type, IServiceCollection services, IConfiguration configuration)
    {
        var mi = type.GetMethod(nameof(IEndpoints.AddEndPointServices), BindingFlags.Public | BindingFlags.Static);
        mi?.Invoke(null, [services, configuration]);
    }

    private static void InvokeDefineEndpoints(TypeInfo type, IEndpointRouteBuilder endpoints)
    {
        var mi = type.GetMethod(nameof(IEndpoints.DefineEndPoints), BindingFlags.Public | BindingFlags.Static);
        mi?.Invoke(null, [endpoints]);
    }
}
