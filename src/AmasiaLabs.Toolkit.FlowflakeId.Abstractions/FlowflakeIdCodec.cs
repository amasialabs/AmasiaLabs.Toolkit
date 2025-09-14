namespace AmasiaLabs.Toolkit.FlowflakeId.Abstractions;

/// <summary>
/// Specifies the encoding format for Flowflake IDs.
/// </summary>
public enum FlowflakeIdCodec
{
    /// <summary>
    /// Base62 encoding using alphanumeric characters (0-9, A-Z, a-z).
    /// </summary>
    Base62 = 0,

    /// <summary>
    /// Base36 encoding using numbers and lowercase letters (0-9, a-z).
    /// </summary>
    Base36 = 1,

    /// <summary>
    /// Hexadecimal encoding (0-9, A-F).
    /// </summary>
    Hex = 2,

    /// <summary>
    /// Base58 encoding using alphanumeric characters, similar to Base62 but excludes similar-looking characters
    /// such as '0', 'O', 'I', and 'l' to improve readability.
    /// </summary>
    Base58 = 3,

    /// <summary>
    /// Crockford's Base32 encoding format.
    /// Designed for human-friendly input to avoid transcription errors and easily readable identifiers.
    /// </summary>
    CrockfordBase32 = 4,
    
    /// <summary>
    /// Base64Url encoding using alphanumeric characters (0-9, A-Z, a-z, -, _).
    /// </summary>
    Base64Url = 5,
}