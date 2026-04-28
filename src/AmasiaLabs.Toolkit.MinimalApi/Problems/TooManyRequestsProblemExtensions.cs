using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

public static class TooManyRequestsProblemExtensions
{
    public static IApplicationBuilder UseProblemTooManyRequests(this IApplicationBuilder app)
        => app.UseProblemTooManyRequests(configure: null);

    // ReSharper disable once MemberCanBePrivate.Global
    public static IApplicationBuilder UseProblemTooManyRequests(this IApplicationBuilder app, Action<ProblemDetails>? configure)
        => app.UseProblemForStatus(StatusCodes.Status429TooManyRequests, "Too many requests", configure);
}
