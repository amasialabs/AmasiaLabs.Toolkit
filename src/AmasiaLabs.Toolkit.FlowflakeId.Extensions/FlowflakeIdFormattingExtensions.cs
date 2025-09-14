using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;
using AmasiaLabs.Toolkit.FlowflakeId.Extensions.Codecs;

namespace AmasiaLabs.Toolkit.FlowflakeId.Extensions;

/// <summary>
/// Extension helpers for formatting and parsing Flowflake IDs using a pluggable <see cref="IIdCodec"/>.
/// </summary>
public static class FlowflakeIdFormattingExtensions
{
    /// <summary>
    /// Formats a numeric ID using the provided codec.
    /// </summary>
    public static string FormatFlowflakeId(this long value, IIdCodec codec)
    {
        ArgumentNullException.ThrowIfNull(codec);
        return codec.Encode(value);
    }

    /// <summary>
    /// Parses an encoded ID string using the provided codec.
    /// </summary>
    public static long ParseFlowflakeId(this string text, IIdCodec codec)
    {
        ArgumentNullException.ThrowIfNull(codec);
        return codec.Decode(text);
    }

    /// <summary>
    /// Formats a numeric ID using the specified codec format.
    /// </summary>
    /// <param name="value">The ID value to format.</param>
    /// <param name="codec">The codec format to use.</param>
    public static string FormatFlowflakeId(this long value, FlowflakeIdCodec codec = FlowflakeIdCodec.Base62)
    {
        var codecInstance = FlowflakeIdCodecProvider.GetCodec(codec);
        return codecInstance.Encode(value);
    }

    /// <summary>
    /// Parses an encoded ID string using the specified codec format.
    /// </summary>
    /// <param name="text">The encoded ID string.</param>
    /// <param name="codec">The codec format to use.</param>
    public static long ParseFlowflakeId(this string text, FlowflakeIdCodec codec = FlowflakeIdCodec.Base62)
    {
        ArgumentNullException.ThrowIfNull(text);
        var codecInstance = FlowflakeIdCodecProvider.GetCodec(codec);
        return codecInstance.Decode(text);
    }
}

