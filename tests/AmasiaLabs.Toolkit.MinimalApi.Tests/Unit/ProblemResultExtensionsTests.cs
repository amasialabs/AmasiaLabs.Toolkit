using System.Text;
using System.Text.Json;
using AmasiaLabs.Toolkit.MinimalApi.Problems;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AmasiaLabs.Toolkit.MinimalApi.Tests.Unit;

public class ProblemResultExtensionsTests
{
    [Fact]
    public async Task Unprocessable_Should_Write_ProblemDetails()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGlobalExceptionHandling();
        var sp = services.BuildServiceProvider();

        var ctx = new DefaultHttpContext
        {
            RequestServices = sp,
            Response = { Body = new MemoryStream() }
        };

        // Act
        var result = ctx.Unprocessable(detail: "Validation failed");
        await result.ExecuteAsync(ctx);

        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(
                ctx.Response.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024, leaveOpen: true)
            .ReadToEndAsync(TestContext.Current.CancellationToken);

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        ctx.Response.ContentType.Should().StartWith("application/problem+json");
        root.GetProperty("status").GetInt32().Should().Be(422);
        root.GetProperty("title").GetString().Should().Be("Unprocessable content");
        root.TryGetProperty("detail", out var detailProp).Should().BeTrue();
        detailProp.GetString().Should().Be("Validation failed");
        root.GetProperty("type").GetString().Should().Contain("422");
        root.GetProperty("instance").GetString().Should().NotBeNull();
        root.TryGetProperty("traceId", out var traceId).Should().BeTrue();
        traceId.GetString().Should().NotBeNullOrWhiteSpace();
    }
}
