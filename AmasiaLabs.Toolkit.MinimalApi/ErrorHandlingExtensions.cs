using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi;

public static class ErrorHandlingExtensions
{
    public static IServiceCollection AddGlobalExceptionHandling(this IServiceCollection services, Action<ProblemHandlingOptions>? configure = null)
    {
        var opts = new ProblemHandlingOptions();
        configure?.Invoke(opts);
        services.AddSingleton(opts);
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        => app.UseExceptionHandler();
}

