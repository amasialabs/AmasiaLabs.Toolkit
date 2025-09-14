namespace AmasiaLabs.Toolkit.FlowflakeId;

/// <summary>
/// Codec for encoding/decoding long identifiers to/from strings.
/// Provide a custom implementation to use non-Base62 formats.
/// </summary>
public interface IIdCodec
{
    string Encode(long value);
    long Decode(string text);
}
