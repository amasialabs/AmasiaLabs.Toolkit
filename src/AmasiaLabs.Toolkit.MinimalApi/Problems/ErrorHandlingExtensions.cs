using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

public static class ErrorHandlingExtensions
{
    public static IServiceCollection AddGlobalExceptionHandling(this IServiceCollection services, Action<ProblemHandlingOptions>? configure = null)
    {
        var opts = new ProblemHandlingOptions();
        configure?.Invoke(opts);
        services.AddSingleton(opts);
        services.AddProblemDetails(o =>
        {
            o.CustomizeProblemDetails = ctx =>
            {
                var http = ctx.HttpContext;
                var options = http.RequestServices.GetRequiredService<ProblemHandlingOptions>();
                var pd = ctx.ProblemDetails;

                var status = pd.Status ?? http.Response.StatusCode;
                pd.Status ??= status;
                pd.Instance ??= http.Request.Path;
                // Force our type URI to keep consistent semantics with ProblemHandlingOptions
                pd.Type = options.TypeUriFactory(status);

                if (!pd.Extensions.ContainsKey("traceId"))
                    pd.Extensions["traceId"] = http.TraceIdentifier;

                if (ctx.Exception is not null && options.IncludeExceptionDetails)
                    pd.Detail ??= ctx.Exception.Message;

                pd.Detail ??= options.GetMessage(status);
            };
        });
        // After the framework's own writer, which AddProblemDetails registers: a request that
        // accepts JSON is still answered by that writer, and what the framework writes through
        // IProblemDetailsService for any other request is answered as application/problem+json
        // instead of failing or falling back to the framework's bare JSON or plain text.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProblemDetailsWriter, JsonFallbackProblemDetailsWriter>());
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        => app.UseExceptionHandler();
}
