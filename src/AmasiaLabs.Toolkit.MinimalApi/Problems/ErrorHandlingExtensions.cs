using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        => app.UseExceptionHandler();
}
