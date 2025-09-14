using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;
using AmasiaLabs.Toolkit.FlowflakeId.Extensions.Codecs;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AmasiaLabs.Toolkit.FlowflakeId.Tests.Unit;

public class FlowflakeIdLegacyCompatTests
{
    [Fact]
    public async Task Should_MatchLegacyId_AndBase62_ForGivenDate_AndInstance1()
    {
        // Arrange: legacy semantics (Unspecified epoch/time), instance=1
        // Legacy sample expected values:
        // id: 174890110519607298
        // base62: CuzuTC9raU
        var expectedId = 174890110519607298L;
        var expectedBase62 = "CuzuTC9raU";

        // Epoch used by our services (and legacy baseline): 2023-02-15 (Unspecified)
        var epoch = new DateTime(2023, 02, 15); // Unspecified kind
        var opts = new FlowflakeIdOptions
        {
            InstanceId = 1,
            UseUtcNow = false,
            Epoch = epoch,
            TimeSemantics = FlowflakeTimeSemantics.LegacyUnspecifiedEpoch
        };

        var gen = new FlowflakeId(Options.Create(opts));

        // Date from the legacy example (Unspecified kind)
        var dt = new DateTime(2025, 9, 14, 14, 5, 54);

        // Act
        var id = await gen.GenerateForDateAsync(dt);

        // Assert: exact match with legacy numeric id
        id.Should().Be(expectedId);

        // And Base62 formatting/parsing matches
        var codec = new NumericBase62Codec();
        var base62 = codec.Encode(id);
        base62.Should().Be(expectedBase62);
        var roundtrip = codec.Decode(base62);
        roundtrip.Should().Be(id);

        // Verify that bits decode back to the same second and instance
        var layout = FlowflakeLayout.Default;
        var dtBack = gen.GetDateTime(id);
        dtBack.Should().Be(dt);
        var instance = (int)((id >> (int)layout.InstanceShift) & (long)layout.InstanceMask);
        instance.Should().Be(1);
    }
}

