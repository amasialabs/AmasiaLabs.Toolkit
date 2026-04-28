using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmasiaLabs.Toolkit.MinimalApi.Problems;

public static class UnprocessableContentProblemExtensions
{
    public static IApplicationBuilder UseProblemUnprocessableContent(this IApplicationBuilder app)
        => app.UseProblemUnprocessableContent(configure: null);

    // ReSharper disable once MemberCanBePrivate.Global
    public static IApplicationBuilder UseProblemUnprocessableContent(this IApplicationBuilder app, Action<ProblemDetails>? configure)
        => app.UseProblemForStatus(StatusCodes.Status422UnprocessableEntity, "Unprocessable content", configure);
}
