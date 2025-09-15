using AmasiaLabs.Toolkit.FlowflakeId.Extensions;
using FluentAssertions;
using Grpc.Core;
using Xunit;

namespace AmasiaLabs.Toolkit.FlowflakeId.Grpc.Tests.Integration;

public class FlowflakeGrpcTests(GrpcHostFixture fx) : IClassFixture<GrpcHostFixture>
{
    [Fact]
    public async Task GetServerInfo_Should_Return_Layout_And_Options()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var known = new DateTime(2023, 02, 15, 0, 0, 10, DateTimeKind.Utc);
        
        // Act
        var id = await fx.Client.GenerateForDateAsync(known, ct);

        // Assert
        id.GetTimestampFromFlowflakeId().Should().Be(10L);
        id.GetDateTimeFromFlowflakeId(GrpcHostFixture.EpochUtc, GrpcHostFixture.Semantics).Should().Be(known);
        id.GetInstanceIdFromFlowflakeId().Should().Be(GrpcHostFixture.InstanceId);
    }
    
    [Fact]
    public async Task GetId_Should_Return_Valid_Id()
    {
        // Arrange
        // Act
        var id = await fx.Client.GenerateAsync(TestContext.Current.CancellationToken);

        // Assert
        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetBatch_Should_Return_N_Ids_In_Ascending_Order()
    {
        // Arrange
        
        // Act
        var ids = await fx.Client.GenerateBatchAsync(10, TestContext.Current.CancellationToken);

        // Assert
        ids.Length.Should().Be(10);
        ids.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetBatch_Should_Reject_TooLarge_Size_By_Default()
    {
        var act = async () => await fx.Client.GenerateBatchAsync(10_001, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<RpcException>();
    }
}
