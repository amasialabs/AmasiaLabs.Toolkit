using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AmasiaLabs.Toolkit.MinimalApi.Endpoints;

// The whole facility is reflection-based (it scans assembly types and invokes IEndpoints
// members), so the trim/AOT warning applies to every member. Declared once at class level
// rather than repeated per-member: RequiresUnreferencedCode on a type flows the same IL2026
// to consumer call sites of every member, and any member added later is covered automatically.
[RequiresUnreferencedCode(TrimmerWarning)]
public static class EndpointExtensions
{
    private const string TrimmerWarning =
        "Endpoint discovery scans assembly types and invokes IEndpoints members via reflection, " +
        "which is not compatible with trimming or Native AOT: the trimmer may remove the " +
        "IEndpoints implementations, causing endpoints to silently disappear. Reference the " +
        "endpoint types statically, or do not trim this path.";

    public static IServiceCollection AddEndpoints<TMarker>(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddEndpoints(configuration, typeof(TMarker).Assembly);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddEndpoints(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddEndpoints(configuration, ResolveDefaultAssembly(Assembly.GetCallingAssembly()));
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static IServiceCollection AddEndpoints(this IServiceCollection services, IConfiguration configuration, Assembly assembly)
    {
        foreach (var t in GetEndpointTypes(assembly))
            InvokeAddServices(t, services, configuration);
        return services;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IHostApplicationBuilder AddEndpoints(this IHostApplicationBuilder builder)
    {
        builder.Services.AddEndpoints(builder.Configuration, ResolveDefaultAssembly(Assembly.GetCallingAssembly()));
        return builder;
    }

    public static WebApplication UseEndpoints<TMarker>(this WebApplication app)
    {
        ((IEndpointRouteBuilder)app).UseEndpoints(typeof(TMarker).Assembly);
        return app;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static WebApplication UseEndpoints(this WebApplication app)
    {
        ((IEndpointRouteBuilder)app).UseEndpoints(ResolveDefaultAssembly(Assembly.GetCallingAssembly()));
        return app;
    }

    public static WebApplication UseEndpoints(this WebApplication app, Assembly assembly)
    {
        ((IEndpointRouteBuilder)app).UseEndpoints(assembly);
        return app;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IEndpointRouteBuilder UseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.UseEndpoints(ResolveDefaultAssembly(Assembly.GetCallingAssembly()));
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static IEndpointRouteBuilder UseEndpoints(this IEndpointRouteBuilder endpoints, Assembly assembly)
    {
        foreach (var t in GetEndpointTypes(assembly))
            InvokeDefineEndpoints(t, endpoints);
        return endpoints;
    }

    // Resolve which assembly to scan when the caller did not pass one explicitly.
    // Prefer the process entry assembly; when it is null (common under test hosts and
    // some generic-host scenarios) fall back to the assembly that invoked the public API.
    //
    // The calling assembly MUST be captured at the public entry point and threaded in
    // (hence [MethodImpl(NoInlining)] on those entry points): calling
    // Assembly.GetCallingAssembly() from inside this helper would resolve to THIS toolkit
    // assembly — the caller of ResolveDefaultAssembly — which contains no IEndpoints
    // implementations, so the fallback would silently register nothing.
    private static Assembly ResolveDefaultAssembly(Assembly callingAssembly)
    {
        return Assembly.GetEntryAssembly() ?? callingAssembly;
    }

    private static IEnumerable<TypeInfo> GetEndpointTypes(Assembly assembly)
    {
        return assembly.DefinedTypes.Where(x =>
            x is { IsAbstract: false, IsInterface: false } &&
            typeof(IEndpoints).IsAssignableFrom(x));
    }

    private static void InvokeAddServices(TypeInfo type, IServiceCollection services, IConfiguration configuration)
    {
        // AddEndPointServices is a concrete, optional member (the interface supplies a no-op
        // default and it cannot be implemented explicitly), so a null lookup is expected and skipped.
        var mi = type.GetMethod(nameof(IEndpoints.AddEndPointServices), BindingFlags.Public | BindingFlags.Static);
        mi?.Invoke(null, [services, configuration]);
    }

    private static void InvokeDefineEndpoints(TypeInfo type, IEndpointRouteBuilder endpoints)
    {
        // DefineEndPoints is a required static-abstract member. When GetMethod returns null the
        // type implemented it explicitly (a private, name-mangled member), so fall back to the
        // interface map — otherwise those routes would be silently dropped with no diagnostic.
        var mi = type.GetMethod(nameof(IEndpoints.DefineEndPoints), BindingFlags.Public | BindingFlags.Static)
                 ?? ResolveExplicitStaticImpl(type, nameof(IEndpoints.DefineEndPoints));
        mi?.Invoke(null, [endpoints]);
    }

    // Resolves an explicit interface implementation of a static IEndpoints member, which is
    // private and name-mangled in metadata (so a plain GetMethod misses it).
    private static MethodInfo? ResolveExplicitStaticImpl(TypeInfo type, string interfaceMethodName)
    {
        var map = type.GetInterfaceMap(typeof(IEndpoints));
        for (var i = 0; i < map.InterfaceMethods.Length; i++)
            if (map.InterfaceMethods[i].Name == interfaceMethodName)
                return map.TargetMethods[i];
        return null;
    }
}
