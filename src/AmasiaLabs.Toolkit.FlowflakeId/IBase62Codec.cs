namespace AmasiaLabs.Toolkit.FlowflakeId;

/// <summary>
/// Base62 codec used to encode/decode identifiers.
/// You can provide your own implementation to preserve wire compatibility.
/// </summary>
public interface IBase62Codec
{
    string Encode(long value);
    long Decode(string text);
}

