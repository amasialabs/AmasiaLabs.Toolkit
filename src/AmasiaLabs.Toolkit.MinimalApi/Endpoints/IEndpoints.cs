using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi.Endpoints;

public interface IEndpoints
{
    public static abstract void DefineEndPoints(IEndpointRouteBuilder app);
    // Optional no-op default so implementers don't have to define it
    public static void AddEndPointServices(IServiceCollection services, IConfiguration configuration) { }
}
