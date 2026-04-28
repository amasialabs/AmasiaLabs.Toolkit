using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;
using AmasiaLabs.Toolkit.FlowflakeId.Extensions;
using AmasiaLabs.Toolkit.FlowflakeId.Extensions.Codecs;
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

    [Theory]
    [InlineData("Cc8j1VVxju", FlowflakeIdCodec.Base62, 170772692876656674L)]
    [InlineData("PzWG4cu1Cu", FlowflakeIdCodec.Base58, 170772692876656674L)]
    [InlineData("8M0kX", FlowflakeIdCodec.Base62, 123456789L)]
    public void TryParseFlowflakeId_WithValidInput_Should_Return_True_And_Match_ParseFlowflakeId(string encoded, FlowflakeIdCodec codec, long expected)
    {
        // Act
        var success = encoded.TryParseFlowflakeId(out var value, codec);

        // Assert
        success.Should().BeTrue();
        value.Should().Be(expected);
        value.Should().Be(encoded.ParseFlowflakeId(codec));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-base62-!@#")]
    public void TryParseFlowflakeId_WithInvalidInput_Should_Return_False_And_Zero(string? input)
    {
        // Act
        var success = input.TryParseFlowflakeId(out var value);

        // Assert
        success.Should().BeFalse();
        value.Should().Be(0L);
    }

    [Fact]
    public void TryParseFlowflakeId_WithOverflow_Should_Return_False()
    {
        // Arrange — 12 'z' chars in Base62 overflows ulong (max is ~11 chars for 2^64)
        const string overflowing = "zzzzzzzzzzzzz";

        // Act
        var success = overflowing.TryParseFlowflakeId(out var value, FlowflakeIdCodec.Base62);

        // Assert
        success.Should().BeFalse();
        value.Should().Be(0L);
    }

    [Fact]
    public void TryParseFlowflakeId_WithCodecInstance_Should_Decode_ValidInput()
    {
        // Arrange
        const long original = 170772692876656674L;
        var codec = FlowflakeIdCodecProvider.GetCodec(FlowflakeIdCodec.Base62);
        var encoded = codec.Encode(original);

        // Act
        var success = encoded.TryParseFlowflakeId(codec, out var value);

        // Assert
        success.Should().BeTrue();
        value.Should().Be(original);
    }

    [Fact]
    public void TryParseFlowflakeId_WithNullCodecInstance_Should_Throw()
    {
        // Arrange
        const string encoded = "anything";

        // Act
        var act = () => encoded.TryParseFlowflakeId(null!, out _);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("codec");
    }

    [Fact]
    public void TryFormatFlowflakeId_WithValidValue_Should_Return_True_And_Match_FormatFlowflakeId()
    {
        // Arrange
        const long value = 123456789L;

        // Act
        var success = value.TryFormatFlowflakeId(FlowflakeIdCodec.Base62, out var text);

        // Assert
        success.Should().BeTrue();
        text.Should().Be("8M0kX");
        text.Should().Be(value.FormatFlowflakeId(FlowflakeIdCodec.Base62));
    }

    [Fact]
    public void TryFormatFlowflakeId_WithCodecInstance_Should_Encode()
    {
        // Arrange
        const long value = 170772692876656674L;
        var codec = FlowflakeIdCodecProvider.GetCodec(FlowflakeIdCodec.Base58);

        // Act
        var success = value.TryFormatFlowflakeId(codec, out var text);

        // Assert
        success.Should().BeTrue();
        text.Should().Be("PzWG4cu1Cu");
    }

    [Fact]
    public void TryFormatFlowflakeId_WithNullCodecInstance_Should_Throw()
    {
        // Arrange
        const long value = 123456789L;

        // Act
        var act = () => value.TryFormatFlowflakeId(null!, out _);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("codec");
    }

    [Fact]
    public void TryFormatFlowflakeId_RoundTrip_With_TryParseFlowflakeId_Should_Match()
    {
        // Arrange
        const long original = 9876543210L;

        // Act
        var formatted = original.TryFormatFlowflakeId(FlowflakeIdCodec.Base62, out var text);
        var parsed = text.TryParseFlowflakeId(out var value, FlowflakeIdCodec.Base62);

        // Assert
        formatted.Should().BeTrue();
        parsed.Should().BeTrue();
        value.Should().Be(original);
    }
}