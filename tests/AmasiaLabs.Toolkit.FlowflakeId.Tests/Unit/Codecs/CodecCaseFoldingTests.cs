using AmasiaLabs.Toolkit.FlowflakeId.Extensions.Codecs;
using FluentAssertions;
using Xunit;

namespace AmasiaLabs.Toolkit.FlowflakeId.Tests.Unit.Codecs;

// Locks the case-folding / alias behavior of the codecs that share CodecCharMap.Build(foldCase: true),
// so the shared lookup-table helper cannot silently regress the case-insensitive codecs.
public class CodecCaseFoldingTests
{
    [Fact]
    public void Base36_Decode_Is_Case_Insensitive()
    {
        var codec = new NumericBase36Codec();

        codec.Decode("Z").Should().Be(35L);
        codec.Decode("z").Should().Be(35L);
        codec.Decode("1AphV82ehSF6").Should().Be(codec.Decode("1aphv82ehsf6"));
    }

    [Fact]
    public void Crockford_Decode_Is_Case_Insensitive_And_Maps_Confusable_Aliases()
    {
        var codec = new CrockfordBase32Codec();

        // Canonical digits.
        codec.Decode("0").Should().Be(0L);
        codec.Decode("1").Should().Be(1L);

        // Confusable aliases: O/o -> 0, I/i/L/l -> 1 (these letters are excluded from the alphabet).
        codec.Decode("O").Should().Be(0L);
        codec.Decode("o").Should().Be(0L);
        codec.Decode("I").Should().Be(1L);
        codec.Decode("i").Should().Be(1L);
        codec.Decode("L").Should().Be(1L);
        codec.Decode("l").Should().Be(1L);

        // Case-insensitive alphabet character.
        codec.Decode("Z").Should().Be(codec.Decode("z"));
    }
}
