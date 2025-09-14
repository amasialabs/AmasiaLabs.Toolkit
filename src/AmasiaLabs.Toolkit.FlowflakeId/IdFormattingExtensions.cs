namespace AmasiaLabs.Toolkit.FlowflakeId;

/// <summary>
/// Extension helpers for formatting and parsing Flowflake IDs using a pluggable <see cref="IIdCodec"/>.
/// </summary>
public static class IdFormattingExtensions
{
    /// <summary>
    /// Formats a numeric ID using the provided codec.
    /// </summary>
    public static string FormatId(this long value, IIdCodec codec)
    {
        ArgumentNullException.ThrowIfNull(codec);
        return codec.Encode(value);
    }

    /// <summary>
    /// Parses an encoded ID string using the provided codec.
    /// </summary>
    public static long ParseId(this string text, IIdCodec codec)
    {
        ArgumentNullException.ThrowIfNull(codec);
        return codec.Decode(text);
    }
}

