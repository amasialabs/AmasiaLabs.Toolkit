using System.Net.Http;
using System.Net.Http.Json;
using System.Net;
using AmasiaLabs.Toolkit.FlowflakeId;
using AmasiaLabs.Toolkit.FlowflakeId.Grpc;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AmasiaLabs.Toolkit.FlowflakeId.Grpc.Tests.Integration;

public class FlowflakeGrpcTests
{
    private static (WebApplication App, FlowflakeIds.FlowflakeIdsClient Client) BuildApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();

        // Configure Flowflake via in-memory config
        var epoch = new DateTime(2023, 02, 15, 0, 0, 0, DateTimeKind.Utc);
        var dict = new Dictionary<string, string?>
        {
            ["Amasia:Toolkit:FlowflakeId:InstanceId"] = "5",
            ["Amasia:Toolkit:FlowflakeId:UseUtcNow"] = "true",
            ["Amasia:Toolkit:FlowflakeId:Epoch"] = epoch.ToString("O")
        };
        builder.Configuration.AddInMemoryCollection(dict);

        builder.Services.AddFlowflakeId(builder.Configuration);
        builder.Services.AddGrpc();

        var app = builder.Build();
        app.MapFlowflakeIdGrpc();

        app.StartAsync().GetAwaiter().GetResult();

        var httpClient = app.GetTestClient();
        httpClient.DefaultRequestVersion = new Version(2, 0);
        httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpClient = httpClient
        });

        var client = new FlowflakeIds.FlowflakeIdsClient(channel);
        return (app, client);
    }

    [Fact]
    public async Task GetId_Should_Return_Valid_Id()
    {
        // Arrange
        var (app, client) = BuildApp();

        try
        {
            // Act
            var resp = await client.GetIdAsync(new Empty());
            long id = resp.Id;

            // Assert
            id.Should().BeGreaterThan(0);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task GetServerInfo_Should_Return_Layout_And_Options()
    {
        // Arrange
        var (app, client) = BuildApp();

        try
        {
            // Act
            var info = await client.GetServerInfoAsync(new Empty());

            // Assert
            info.InstanceId.Should().Be(5);
            info.UseUtcNow.Should().BeTrue();
            info.TimestampShift.Should().Be(FlowflakeLayout.Default.TimestampShift);
            info.InstanceShift.Should().Be(FlowflakeLayout.Default.InstanceShift);
            info.SequenceMask.Should().Be(FlowflakeLayout.Default.SequenceMask);
            info.InstanceMask.Should().Be(FlowflakeLayout.Default.InstanceMask);
            info.Epoch.Should().NotBeNull();
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task GetIdForDate_Should_Use_Provided_Timestamp()
    {
        // Arrange
        var (app, client) = BuildApp();
        var when = Timestamp.FromDateTime(DateTime.SpecifyKind(new DateTime(2023, 02, 15, 0, 0, 10), DateTimeKind.Utc));

        try
        {
            // Act
            var resp = await client.GetIdForDateAsync(new DateRequest { Timestamp = when });

            // Assert (timestamp bits equal to provided)
            long id = resp.Id;
            var epochSeconds = 10L; // from above timestamp
            var ts = id >> (int)FlowflakeLayout.Default.TimestampShift;
            ts.Should().Be(epochSeconds);
        }
        finally
        {
            await app.StopAsync();
        }
    }
}

