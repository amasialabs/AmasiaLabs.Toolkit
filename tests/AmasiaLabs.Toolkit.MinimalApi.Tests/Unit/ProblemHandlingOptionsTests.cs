using AmasiaLabs.Toolkit.MinimalApi.Problems;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AmasiaLabs.Toolkit.MinimalApi.Tests.Unit;

public class ProblemHandlingOptionsTests
{
    [Fact]
    public void Default_Status_Messages_Should_Contain_Common_Statuses()
    {
        var opts = new ProblemHandlingOptions();

        opts.GetMessage(StatusCodes.Status400BadRequest).Should().NotBeNullOrWhiteSpace();
        opts.GetMessage(StatusCodes.Status401Unauthorized).Should().NotBeNullOrWhiteSpace();
        opts.GetMessage(StatusCodes.Status403Forbidden).Should().NotBeNullOrWhiteSpace();
        opts.GetMessage(StatusCodes.Status404NotFound).Should().NotBeNullOrWhiteSpace();
        opts.GetMessage(StatusCodes.Status405MethodNotAllowed).Should().NotBeNullOrWhiteSpace();
        opts.GetMessage(StatusCodes.Status409Conflict).Should().NotBeNullOrWhiteSpace();
        opts.GetMessage(StatusCodes.Status422UnprocessableEntity).Should().NotBeNullOrWhiteSpace();
        opts.GetMessage(StatusCodes.Status429TooManyRequests).Should().NotBeNullOrWhiteSpace();
        opts.GetMessage(StatusCodes.Status500InternalServerError).Should().NotBeNullOrWhiteSpace();
    }
}

