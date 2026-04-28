using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

public static class MethodNotAllowedProblemExtensions
{
    public static IApplicationBuilder UseProblemMethodNotAllowed(this IApplicationBuilder app)
        => app.UseProblemMethodNotAllowed(configure: null);

    // ReSharper disable once MemberCanBePrivate.Global
    public static IApplicationBuilder UseProblemMethodNotAllowed(this IApplicationBuilder app, Action<ProblemDetails>? configure)
        => app.UseProblemForStatus(StatusCodes.Status405MethodNotAllowed, "Method not allowed", configure);
}
