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

    /// <summary>
    /// Attempts to parse an encoded ID string using the specified codec format.
    /// Returns false instead of throwing on malformed input or null/empty text.
    /// </summary>
    /// <param name="text">The encoded ID string. May be null.</param>
    /// <param name="value">When this method returns true, the decoded ID; otherwise zero.</param>
    /// <param name="codec">The codec format to use. Defaults to <see cref="FlowflakeIdCodec.Base62"/>.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    public static bool TryParseFlowflakeId(this string? text, out long value, FlowflakeIdCodec codec = FlowflakeIdCodec.Base62)
    {
        if (string.IsNullOrEmpty(text))
        {
            value = 0;
            return false;
        }

        try
        {
            var codecInstance = FlowflakeIdCodecProvider.GetCodec(codec);
            value = codecInstance.Decode(text);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
        {
            value = 0;
            return false;
        }
    }

    /// <summary>
    /// Attempts to parse an encoded ID string using the provided codec instance.
    /// Returns false instead of throwing on malformed input or null/empty text.
    /// </summary>
    /// <param name="text">The encoded ID string. May be null.</param>
    /// <param name="codec">The codec to use.</param>
    /// <param name="value">When this method returns true, the decoded ID; otherwise zero.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    public static bool TryParseFlowflakeId(this string? text, IIdCodec codec, out long value)
    {
        ArgumentNullException.ThrowIfNull(codec);

        if (string.IsNullOrEmpty(text))
        {
            value = 0;
            return false;
        }

        try
        {
            value = codec.Decode(text);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
        {
            value = 0;
            return false;
        }
    }

    /// <summary>
    /// Attempts to format an ID using the specified codec format.
    /// Mirrors <see cref="TryParseFlowflakeId(string?, out long, FlowflakeIdCodec)"/> for symmetry;
    /// in practice built-in codecs do not throw for any <see cref="long"/> input.
    /// </summary>
    /// <param name="value">The ID value to format.</param>
    /// <param name="codec">The codec format to use.</param>
    /// <param name="text">When this method returns true, the encoded ID; otherwise null.</param>
    /// <returns><c>true</c> if formatting succeeded; otherwise <c>false</c>.</returns>
    public static bool TryFormatFlowflakeId(this long value, FlowflakeIdCodec codec, out string? text)
    {
        try
        {
            var codecInstance = FlowflakeIdCodecProvider.GetCodec(codec);
            text = codecInstance.Encode(value);
            return true;
        }
        catch
        {
            text = null;
            return false;
        }
    }

    /// <summary>
    /// Attempts to format an ID using the provided codec instance.
    /// Mirrors <see cref="TryParseFlowflakeId(string?, IIdCodec, out long)"/> for symmetry;
    /// in practice built-in codecs do not throw for any <see cref="long"/> input.
    /// </summary>
    /// <param name="value">The ID value to format.</param>
    /// <param name="codec">The codec to use.</param>
    /// <param name="text">When this method returns true, the encoded ID; otherwise null.</param>
    /// <returns><c>true</c> if formatting succeeded; otherwise <c>false</c>.</returns>
    public static bool TryFormatFlowflakeId(this long value, IIdCodec codec, out string? text)
    {
        ArgumentNullException.ThrowIfNull(codec);

        try
        {
            text = codec.Encode(value);
            return true;
        }
        catch
        {
            text = null;
            return false;
        }
    }
}

