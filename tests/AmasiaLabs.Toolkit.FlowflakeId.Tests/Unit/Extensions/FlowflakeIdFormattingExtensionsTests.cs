using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;
using AmasiaLabs.Toolkit.FlowflakeId.Extensions;
using FluentAssertions;
using Xunit;

namespace AmasiaLabs.Toolkit.FlowflakeId.Tests.Unit.Extensions;

public class FlowflakeIdFormattingExtensionsTests
{
    [Theory]
    [InlineData(170772692876656674L, FlowflakeIdCodec.Base62, "Cc8j1VVxju")]
    [InlineData(170772692876656674L, FlowflakeIdCodec.Base58, "PzWG4cu1Cu")]
    [InlineData(170772692876656674L, FlowflakeIdCodec.Base36, "1aphv82ehsf6")]
    [InlineData(170772692876656674L, FlowflakeIdCodec.CrockfordBase32, "4QNMVG040012")]
    [InlineData(170772692876656674L, FlowflakeIdCodec.Hex, "025EB4DC00400022")]
    [InlineData(170772692876656674L, FlowflakeIdCodec.Base64Url, "Al603ABAACI")]
    public void FormatFlowflakeId_WithEnum_Should_UseCorrectCodec(long value, FlowflakeIdCodec codec, string expected)
    {
        // Act
        var result = value.FormatFlowflakeId(codec);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Cc8j1VVxju", FlowflakeIdCodec.Base62, 170772692876656674L)]
    [InlineData("PzWG4cu1Cu", FlowflakeIdCodec.Base58, 170772692876656674L)]
    [InlineData("1aphv82ehsf6", FlowflakeIdCodec.Base36, 170772692876656674L)]
    [InlineData("4QNMVG040012", FlowflakeIdCodec.CrockfordBase32, 170772692876656674L)]
    [InlineData("025EB4DC00400022", FlowflakeIdCodec.Hex, 170772692876656674L)]
    [InlineData("Al603ABAACI", FlowflakeIdCodec.Base64Url, 170772692876656674L)]
    public void ParseFlowflakeId_WithEnum_Should_UseCorrectCodec(string encoded, FlowflakeIdCodec codec, long expected)
    {
        // Act
        var result = encoded.ParseFlowflakeId(codec);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatFlowflakeId_WithoutCodec_Should_UseBase62ByDefault()
    {
        // Arrange
        const long value = 123456789L;

        // Act
        var result = value.FormatFlowflakeId();

        // Assert
        result.Should().Be("8M0kX"); // Base62 encoding
    }

    [Fact]
    public void ParseFlowflakeId_WithoutCodec_Should_UseBase62ByDefault()
    {
        // Arrange
        const string encoded = "8M0kX"; // Base62 encoded 123456789

        // Act
        var result = encoded.ParseFlowflakeId();

        // Assert
        result.Should().Be(123456789L);
    }

    [Fact]
    public void FormatFlowflakeId_WithNullCodecInstance_Should_Throw()
    {
        // Arrange
        const long value = 123456789L;

        // Act
        var act = () => value.FormatFlowflakeId(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("codec");
    }

    [Fact]
    public void ParseFlowflakeId_WithNullCodecInstance_Should_Throw()
    {
        // Arrange
        const string encoded = "test";

        // Act
        var act = () => encoded.ParseFlowflakeId(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("codec");
    }

    [Fact]
    public void ParseFlowflakeId_WithNullString_Should_Throw()
    {
        // Arrange
        string? encoded = null;

        // Act
        var act = () => encoded!.ParseFlowflakeId();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("text");
    }

    [Fact]
    public void Extensions_Should_RoundTrip_WithAllCodecs()
    {
        // Arrange
        const long value = 9876543210L;
        var codecs = Enum.GetValues<FlowflakeIdCodec>();

        // Act & Assert
        foreach (var codec in codecs)
        {
            var encoded = value.FormatFlowflakeId(codec);
            var decoded = encoded.ParseFlowflakeId(codec);

            decoded.Should().Be(value, $"Should round-trip with {codec} codec");
        }
    }
}