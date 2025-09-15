extern alias GrpcClient;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net;
using AmasiaLabs.Toolkit.FlowflakeId;
using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;
using AmasiaLabs.Toolkit.FlowflakeId.Extensions;
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
    private static (WebApplication App, GrpcClient::AmasiaLabs.Toolkit.FlowflakeId.Grpc.FlowflakeIds.FlowflakeIdsClient Client) BuildApp()
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
            ["Amasia:Toolkit:FlowflakeId:FlowflakeClock:Epoch"] = epoch.ToString("O"),
            ["Amasia:Toolkit:FlowflakeId:FlowflakeClock:TimeSemantics"] = "UtcNormalized"
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

        var client = new GrpcClient::AmasiaLabs.Toolkit.FlowflakeId.Grpc.FlowflakeIds.FlowflakeIdsClient(channel);
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
            var resp = await client.GetIdAsync(new Empty(), cancellationToken: TestContext.Current.CancellationToken);
            long id = resp.Id;

            // Assert
            id.Should().BeGreaterThan(0);
        }
        finally
        {
            await app.StopAsync(TestContext.Current.CancellationToken);
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
            var info = await client.GetServerInfoAsync(new Empty(), cancellationToken: TestContext.Current.CancellationToken);

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
            await app.StopAsync(TestContext.Current.CancellationToken);
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
            var resp = await client.GetIdForDateAsync(new GrpcClient::AmasiaLabs.Toolkit.FlowflakeId.Grpc.DateRequest { Timestamp = when }, cancellationToken: TestContext.Current.CancellationToken);

            // Assert (timestamp bits equal to provided)
            long id = resp.Id;
            var epochSeconds = 10L; // from above timestamp
            var ts = id >> (int)FlowflakeLayout.Default.TimestampShift;
            ts.Should().Be(epochSeconds);
        }
        finally
        {
            await app.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task GetBatch_Should_Return_N_Ids_In_Ascending_Order()
    {
        // Arrange
        var (app, client) = BuildApp();

        try
        {
            // Act
            var size = 10;
            var resp = await client.GetBatchAsync(new GrpcClient::AmasiaLabs.Toolkit.FlowflakeId.Grpc.BatchRequest { Size = size }, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            resp.Ids.Count.Should().Be(size);
            resp.Ids.Should().BeInAscendingOrder();
        }
        finally
        {
            await app.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task GetBatch_Should_Reject_TooLarge_Size_By_Default()
    {
        // Arrange
        var (app, client) = BuildApp();

        try
        {
            // Act
            Func<Task> act = async () => await client.GetBatchAsync(new GrpcClient::AmasiaLabs.Toolkit.FlowflakeId.Grpc.BatchRequest { Size = 10_001 }, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            await act.Should().ThrowAsync<global::Grpc.Core.RpcException>();
        }
        finally
        {
            await app.StopAsync(TestContext.Current.CancellationToken);
        }
    }
}
