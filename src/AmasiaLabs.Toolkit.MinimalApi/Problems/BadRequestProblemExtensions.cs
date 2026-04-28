using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

public static class BadRequestProblemExtensions
{
    public static IApplicationBuilder UseProblemBadRequest(this IApplicationBuilder app)
        => app.UseProblemBadRequest(configure: null);

    // ReSharper disable once MemberCanBePrivate.Global
    public static IApplicationBuilder UseProblemBadRequest(this IApplicationBuilder app, Action<ProblemDetails>? configure)
        => app.UseProblemForStatus(StatusCodes.Status400BadRequest, "Bad request", configure);
}
