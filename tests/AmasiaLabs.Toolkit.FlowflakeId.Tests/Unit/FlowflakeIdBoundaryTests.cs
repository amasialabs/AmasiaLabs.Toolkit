using System.Reflection;
using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace AmasiaLabs.Toolkit.FlowflakeId.Tests.Unit;

public class FlowflakeIdBoundaryTests
{
    private static FlowflakeId Create(FlowflakeIdOptions opts, FakeTimeProvider time)
        => new(Options.Create(opts), time);

    [Fact]
    public void Should_UseAll22Bits_AndNotOverwriteInstanceOrTimestamp()
    {
        // Arrange: fixed epoch and second
        var epoch = new DateTime(2023, 02, 15, 0, 0, 0, DateTimeKind.Utc);
        var secondOffset = 100;
        var dt = epoch.AddSeconds(secondOffset);
        var time = new FakeTimeProvider(new DateTimeOffset(epoch));
        var opts = new FlowflakeIdOptions { InstanceId = 42, UseUtcNow = true, Epoch = epoch };
        var gen = Create(opts, time);

        // Prime last-seen second
        _ = gen.GenerateForDate(dt);

        // Set an internal sequence close to max to exercise wrap
        var seqMax = FlowflakeLayout.Default.SequenceMax;
        var field = typeof(FlowflakeId).GetField("_sequenceId", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(gen, seqMax - 1);

        // Act: first call reaches max, second wraps to start of range
        var id1 = gen.GenerateForDate(dt); // sequence == seqMax
        var id2 = gen.GenerateForDate(dt); // sequence wraps to SequenceMin+1 (i.e., 2)

        // Assert timestamp and instance are stable and sequence uses full 22 bits
        var layout = FlowflakeLayout.Default;
        long seconds1 = id1 >> (int)layout.TimestampShift;
        long seconds2 = id2 >> (int)layout.TimestampShift;
        var instance1 = (id1 >> (int)layout.InstanceShift) & (long)layout.InstanceMask;
        var instance2 = (id2 >> (int)layout.InstanceShift) & (long)layout.InstanceMask;
        var seq1 = (int)(id1 & (long)layout.SequenceMask);
        var seq2 = (int)(id2 & (long)layout.SequenceMask);

        seconds1.Should().Be(secondOffset);
        seconds2.Should().Be(secondOffset);
        instance1.Should().Be(opts.InstanceId);
        instance2.Should().Be(opts.InstanceId);
        seq1.Should().Be(seqMax);
        seq2.Should().Be(layout.SequenceMin + 1);
        // Date should remain the same second
        var dt1 = gen.GetDateTime(id1);
        var dt2 = gen.GetDateTime(id2);
        dt1.Should().Be(dt);
        dt2.Should().Be(dt);
    }

    [Fact]
    public void Should_NotOverwrite_OnLegacySequenceMax()
    {
        // Arrange
        var epoch = new DateTime(2023, 02, 15, 0, 0, 0, DateTimeKind.Utc);
        var secondOffset = 7;
        var dt = epoch.AddSeconds(secondOffset);
        var time = new FakeTimeProvider(new DateTimeOffset(epoch));
        var opts = new FlowflakeIdOptions { InstanceId = 123, UseUtcNow = true, Epoch = epoch };
        var gen = Create(opts, time);

        // Prime second
        _ = gen.GenerateForDate(dt);

        var legacyMax = (1 << 21) - 1; // 2_097_151
        var field = typeof(FlowflakeId).GetField("_sequenceId", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(gen, legacyMax - 1);

        // Act
        var id = gen.GenerateForDate(dt);

        // Assert: timestamp/instance intact, sequence at legacy max
        var layout = FlowflakeLayout.Default;
        long seconds = id >> (int)layout.TimestampShift;
        var instance = (id >> (int)layout.InstanceShift) & (long)layout.InstanceMask;
        var seq = (int)(id & (long)layout.SequenceMask);

        seconds.Should().Be(secondOffset);
        instance.Should().Be(opts.InstanceId);
        seq.Should().Be(legacyMax);
        // Date must be preserved at the exact second
        var dtActual = gen.GetDateTime(id);
        dtActual.Should().Be(dt);
    }

    [Fact]
    public void Should_NotOverwrite_OnNewSequenceBoundary()
    {
        // Arrange
        var epoch = new DateTime(2023, 02, 15, 0, 0, 0, DateTimeKind.Utc);
        var secondOffset = 8;
        var dt = epoch.AddSeconds(secondOffset);
        var time = new FakeTimeProvider(new DateTimeOffset(epoch));
        var opts = new FlowflakeIdOptions { InstanceId = 321, UseUtcNow = true, Epoch = epoch };
        var gen = Create(opts, time);

        // Prime second
        _ = gen.GenerateForDate(dt);

        var newBoundary = (1 << 21); // 2_097_152 (first value that uses the 22nd bit)
        var field = typeof(FlowflakeId).GetField("_sequenceId", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(gen, newBoundary - 1);

        // Act: generate the first value that uses the 22nd bit
        var id = gen.GenerateForDate(dt);          // seq == 2^21

        // Assert: timestamp/instance intact, sequence equals new boundary value
        var layout = FlowflakeLayout.Default;
        long seconds = id >> (int)layout.TimestampShift;
        var instance = (id >> (int)layout.InstanceShift) & (long)layout.InstanceMask;
        var seq = (int)(id & (long)layout.SequenceMask);

        seconds.Should().Be(secondOffset);
        instance.Should().Be(opts.InstanceId);
        seq.Should().Be(newBoundary);
        // Date must be preserved at the exact second
        var dtActual = gen.GetDateTime(id);
        dtActual.Should().Be(dt);
    }

    [Fact]
    public void Should_Increase_FromLegacyMax_ToNewBoundary_WithinSameSecond()
    {
        // Arrange
        var epoch = new DateTime(2023, 02, 15, 0, 0, 0, DateTimeKind.Utc);
        var secondOffset = 9;
        var dt = epoch.AddSeconds(secondOffset);
        var time = new FakeTimeProvider(new DateTimeOffset(epoch));
        var opts = new FlowflakeIdOptions { InstanceId = 77, UseUtcNow = true, Epoch = epoch };
        var gen = Create(opts, time);

        // Prime second
        _ = gen.GenerateForDate(dt);

        var legacyMax = (1 << 21) - 1; // 2_097_151
        var field = typeof(FlowflakeId).GetField("_sequenceId", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(gen, legacyMax - 1);

        // Act
        var id1 = gen.GenerateForDate(dt); // legacy max
        var id2 = gen.GenerateForDate(dt); // first value in the new (22-bit) half

        // Assert sequence grows and date/instance are not overwritten
        var layout = FlowflakeLayout.Default;
        var seq1 = (int)(id1 & (long)layout.SequenceMask);
        var seq2 = (int)(id2 & (long)layout.SequenceMask);
        var seconds1 = id1 >> (int)layout.TimestampShift;
        var seconds2 = id2 >> (int)layout.TimestampShift;
        var instance1 = (id1 >> (int)layout.InstanceShift) & (long)layout.InstanceMask;
        var instance2 = (id2 >> (int)layout.InstanceShift) & (long)layout.InstanceMask;

        seq1.Should().Be(legacyMax);
        seq2.Should().Be(legacyMax + 1);
        id2.Should().BeGreaterThan(id1); // monotonic across the legacy→new boundary

        seconds1.Should().Be(secondOffset);
        seconds2.Should().Be(secondOffset);
        instance1.Should().Be(opts.InstanceId);
        instance2.Should().Be(opts.InstanceId);

        // Date must be preserved at the exact second
        var dt1 = gen.GetDateTime(id1);
        var dt2 = gen.GetDateTime(id2);
        dt1.Should().Be(dt);
        dt2.Should().Be(dt);
    }
}
