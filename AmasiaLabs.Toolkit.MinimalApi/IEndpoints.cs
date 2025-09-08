using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi;

public interface IEndpoints
{
    public static abstract void DefineEndPoints(IEndpointRouteBuilder app);
    public static abstract void AddEndPointServices(IServiceCollection services, IConfiguration configuration);
}