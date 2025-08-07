using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi;

public static class EndpointExtensions
{
    public static void AddEndpoints<TMarker>(this IServiceCollection services, IConfiguration configuration)
    {
        var endPointTypes = GetEndPointTypesFromAssembly(typeof(TMarker));

        foreach (var endPointType in endPointTypes)
        {
            endPointType.GetMethod(nameof(IEndpoints.AddEndPointServices))!.Invoke(null, [services, configuration]);
        }
    }
    
    public static void UseEndpoints<TMarker>(this IApplicationBuilder app)
    {
        var endPointTypes = GetEndPointTypesFromAssembly(typeof(TMarker));
        
        foreach (var endPointType in endPointTypes)
        {
            endPointType.GetMethod(nameof(IEndpoints.DefineEndPoints))!.Invoke(null, [app]);
        }
    }
    
    private static IEnumerable<TypeInfo> GetEndPointTypesFromAssembly(Type typeMarker)
    {
        return typeMarker.Assembly.DefinedTypes
            .Where(x => x is { IsAbstract: false, IsInterface: false } && typeof(IEndpoints).IsAssignableFrom(x));
    }
}