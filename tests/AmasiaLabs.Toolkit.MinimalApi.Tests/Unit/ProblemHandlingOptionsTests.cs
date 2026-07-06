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
        // Arrange
        var opts = new ProblemHandlingOptions();

        // Act
        // no-op

        // Assert
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

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest)]
    [InlineData(StatusCodes.Status413PayloadTooLarge)]
    public void Resolve_BadHttpRequestException_Should_Use_Its_Own_Status_Code(int statusCode)
    {
        // A malformed request body (e.g. bad chunked framing, oversized payload) escapes as a
        // BadHttpRequestException carrying its own client-error status; Resolve must surface that
        // instead of collapsing it to a 500.
        var ex = new BadHttpRequestException("bad request", statusCode);

        var (status, _, _) = ProblemHandlingOptions.Resolve(ex, new DefaultHttpContext());

        status.Should().Be(statusCode);
    }

    [Fact]
    public void Resolve_Unmapped_Exception_Should_Fall_Back_To_500()
    {
        var (status, _, _) = ProblemHandlingOptions.Resolve(new InvalidOperationException(), new DefaultHttpContext());

        status.Should().Be(StatusCodes.Status500InternalServerError);
    }
}
