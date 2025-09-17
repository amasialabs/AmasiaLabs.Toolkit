using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

public static class UnprocessableContentProblemExtensions
{
    public static IApplicationBuilder UseProblemUnprocessableContent(this IApplicationBuilder app)
        => app.UseProblemUnprocessableContent(configure: null);

    // ReSharper disable once MemberCanBePrivate.Global
    public static IApplicationBuilder UseProblemUnprocessableContent(this IApplicationBuilder app, Action<ProblemDetails>? configure)
    {
        return app.Use(async (ctx, next) =>
        {
            await next();

            if (ctx.Response.HasStarted)
                return;

            if (ctx.Response.StatusCode != StatusCodes.Status422UnprocessableEntity)
                return;

            var status = StatusCodes.Status422UnprocessableEntity;
            var pd = new ProblemDetails { Status = status, Title = "Unprocessable content" };

            configure?.Invoke(pd);

            var pds = ctx.RequestServices.GetRequiredService<IProblemDetailsService>();
            var context = new ProblemDetailsContext
            {
                HttpContext = ctx,
                ProblemDetails = pd
            };
            await pds.WriteAsync(context);
        });
    }
}
