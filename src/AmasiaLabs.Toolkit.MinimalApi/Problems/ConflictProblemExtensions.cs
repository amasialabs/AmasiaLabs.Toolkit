using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

public static class ConflictProblemExtensions
{
    public static IApplicationBuilder UseProblemConflict(this IApplicationBuilder app)
        => app.UseProblemConflict(configure: null);

    // ReSharper disable once MemberCanBePrivate.Global
    public static IApplicationBuilder UseProblemConflict(this IApplicationBuilder app, Action<ProblemDetails>? configure)
        => app.UseProblemForStatus(StatusCodes.Status409Conflict, "Conflict", configure);
}
