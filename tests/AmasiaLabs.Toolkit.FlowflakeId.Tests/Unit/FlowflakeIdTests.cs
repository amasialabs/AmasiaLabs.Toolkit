using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace AmasiaLabs.Toolkit.FlowflakeId.Tests.Unit;

public class FlowflakeIdTests
{
    private static FlowflakeId Create(FlowflakeIdOptions opts, FakeTimeProvider time)
        => new(Options.Create(opts), time);

    [Fact]
    public async Task Generate_ReturnsIds_WithExpectedLayout()
    {
        // Arrange
        var epoch = new DateTime(2023, 02, 15, 0, 0, 0, DateTimeKind.Utc);
        var start = new DateTimeOffset(epoch).AddSeconds(123);
        var time = new FakeTimeProvider(start);
        var opts = new FlowflakeIdOptions { InstanceId = 42, UseUtcNow = true, Epoch = epoch };
        var gen = Create(opts, time);

        // Act
        var id = await gen.GenerateAsync(TestContext.Current.CancellationToken);
        var dt = gen.GetDateTime(id);
        var instance = gen.GetInstanceIdFromFlowflakeId(id);

        // Assert
        dt.Should().Be(epoch.AddSeconds(123));
        instance.Should().Be(42);
        gen.GetInstanceId().Should().Be(42);
    }

    [Fact]
    public async Task Generate_InSameSecond_IncrementsSequence()
    {
        // Arrange
        var epoch = new DateTime(2023, 02, 15, 0, 0, 0, DateTimeKind.Utc);
        var start = new DateTimeOffset(epoch).AddSeconds(10);
        var time = new FakeTimeProvider(start);
        var opts = new FlowflakeIdOptions { InstanceId = 1, UseUtcNow = true, Epoch = epoch };
        var gen = Create(opts, time);

        // Act
        var ids = new List<long>();
        for (int i = 0; i < 5; i++) ids.Add(await gen.GenerateAsync(TestContext.Current.CancellationToken));
        var arr = ids.ToArray();

        // Assert
        arr.Should().BeInAscendingOrder();
        // Legacy behavior: the first sequence starts at 2 (initial value is 1, then Interlocked.Increment).
        arr.Select(id => id & ((1L << 22) - 1)).Should().ContainInOrder(2, 3, 4, 5, 6);
    }

    [Fact]
    public async Task Generate_WhenClockRollsBack_UsesFailoverInstance()
    {
        // Arrange
        var epoch = new DateTime(2023, 02, 15, 0, 0, 0, DateTimeKind.Utc);
        var start = new DateTimeOffset(epoch).AddSeconds(1000);
        var time = new FakeTimeProvider(start);
        var opts = new FlowflakeIdOptions { InstanceId = 10, FailoverInstanceId = 11, UseUtcNow = true, Epoch = epoch };
        var gen = Create(opts, time);
        var earlier = epoch.AddSeconds(998);

        // Act
        var id1 = await gen.GenerateAsync(TestContext.Current.CancellationToken); // at t=1000, instance=10
        var id2 = await gen.GenerateForDateAsync(earlier, TestContext.Current.CancellationToken); // should use instance=11
        
        var id3 = await gen.GenerateAsync(TestContext.Current.CancellationToken);
        var inst1 = gen.GetInstanceIdFromFlowflakeId(id1);
        var inst2 = gen.GetInstanceIdFromFlowflakeId(id2);
        var inst3 = gen.GetInstanceIdFromFlowflakeId(id3);

        // Assert
        inst1.Should().Be(10);
        inst2.Should().Be(11);
        inst3.Should().Be(10);
        id3.Should().BeGreaterThan(id1);
        id3.Should().BeGreaterThan(id2);
    }
}
