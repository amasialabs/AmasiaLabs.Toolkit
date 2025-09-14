using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;
using AmasiaLabs.Toolkit.FlowflakeId.Extensions.Codecs;
using FluentAssertions;
using Xunit;

namespace AmasiaLabs.Toolkit.FlowflakeId.Tests.Unit.Codecs;

public class FlowflakeIdCodecProviderTests
{
    [Fact]
    public void Base62_Should_ReturnSameInstance()
    {
        // Act
        var instance1 = FlowflakeIdCodecProvider.Base62;
        var instance2 = FlowflakeIdCodecProvider.Base62;

        // Assert
        instance1.Should().BeSameAs(instance2);
        instance1.Should().BeOfType<NumericBase62Codec>();
    }

    [Fact]
    public void Base36_Should_ReturnSameInstance()
    {
        // Act
        var instance1 = FlowflakeIdCodecProvider.Base36;
        var instance2 = FlowflakeIdCodecProvider.Base36;

        // Assert
        instance1.Should().BeSameAs(instance2);
        instance1.Should().BeOfType<NumericBase36Codec>();
    }

    [Fact]
    public void Hex_Should_ReturnSameInstance()
    {
        // Act
        var instance1 = FlowflakeIdCodecProvider.Hex;
        var instance2 = FlowflakeIdCodecProvider.Hex;

        // Assert
        instance1.Should().BeSameAs(instance2);
        instance1.Should().BeOfType<HexCodec>();
    }

    [Theory]
    [InlineData(FlowflakeIdCodec.Base62, typeof(NumericBase62Codec))]
    [InlineData(FlowflakeIdCodec.Base36, typeof(NumericBase36Codec))]
    [InlineData(FlowflakeIdCodec.Hex, typeof(HexCodec))]
    public void GetCodec_Should_ReturnCorrectCodec(FlowflakeIdCodec codec, Type expectedType)
    {
        // Act
        var result = FlowflakeIdCodecProvider.GetCodec(codec);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType(expectedType);
    }

    [Fact]
    public void GetCodec_Should_ReturnSameInstanceAsProperties()
    {
        // Act & Assert
        FlowflakeIdCodecProvider.GetCodec(FlowflakeIdCodec.Base62)
            .Should().BeSameAs(FlowflakeIdCodecProvider.Base62);

        FlowflakeIdCodecProvider.GetCodec(FlowflakeIdCodec.Base36)
            .Should().BeSameAs(FlowflakeIdCodecProvider.Base36);

        FlowflakeIdCodecProvider.GetCodec(FlowflakeIdCodec.Hex)
            .Should().BeSameAs(FlowflakeIdCodecProvider.Hex);
    }

    [Fact]
    public void GetCodec_WithInvalidEnum_Should_Throw()
    {
        // Arrange
        const FlowflakeIdCodec invalidCodec = (FlowflakeIdCodec)999;

        // Act
        var act = () => FlowflakeIdCodecProvider.GetCodec(invalidCodec);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("codec")
            .WithMessage("*Unsupported codec format*");
    }

    [Fact]
    public async Task Provider_Should_WorkCorrectlyInMultithreadedEnvironment()
    {
        // Arrange
        var tasks = new List<Task<IIdCodec>>();
        var barrier = new Barrier(10);

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                barrier.SignalAndWait();
                return FlowflakeIdCodecProvider.Base62;
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllBeEquivalentTo(results[0]);
        results.Should().HaveCount(10);
        results.Should().OnlyContain(codec => codec is NumericBase62Codec);
    }
}