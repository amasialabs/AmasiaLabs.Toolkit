using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;

namespace AmasiaLabs.Toolkit.FlowflakeId.Extensions.Codecs;

/// <summary>
/// Provides built-in codec implementations for Flowflake IDs.
/// </summary>
public static class FlowflakeIdCodecProvider
{
    private static readonly Lazy<NumericBase62Codec> Base62Instance = new(() => new NumericBase62Codec());
    private static readonly Lazy<NumericBase36Codec> Base36Instance = new(() => new NumericBase36Codec());
    private static readonly Lazy<HexCodec> HexInstance = new(() => new HexCodec());
    private static readonly Lazy<NumericBase58Codec> Base58Instance = new(() => new  NumericBase58Codec());
    private static readonly Lazy<CrockfordBase32Codec> CrockfordBase32Instance = new(() => new  CrockfordBase32Codec());
    private static readonly Lazy<Base64UrlCodec> Base64UrlCodecInstance = new(() => new  Base64UrlCodec());
    
    /// <summary>
    /// Gets a shared instance of the Base62 codec.
    /// </summary>
    public static IIdCodec Base62 => Base62Instance.Value;

    /// <summary>
    /// Gets a shared instance of the Base36 codec.
    /// </summary>
    public static IIdCodec Base36 => Base36Instance.Value;

    /// <summary>
    /// Gets a shared instance of the hexadecimal codec.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static IIdCodec Hex => HexInstance.Value;
    
    /// <summary>
    /// Gets a shared instance of the Base58 codec.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static IIdCodec Base58 => Base58Instance.Value;

    /// <summary>
    /// Gets a shared instance of the CrockfordBase32 codec.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static IIdCodec CrockfordBase32 => CrockfordBase32Instance.Value;

    /// <summary>
    /// Gets a shared instance of the Base64Url codec.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static IIdCodec Base64Url => Base64UrlCodecInstance.Value;
    
    /// <summary>
    /// Gets a codec instance for the specified format.
    /// </summary>
    /// <param name="codec">The codec format to use.</param>
    /// <returns>An instance of the requested codec.</returns>
    public static IIdCodec GetCodec(FlowflakeIdCodec codec) => codec switch
    {
        FlowflakeIdCodec.Base62 => Base62,
        FlowflakeIdCodec.Base58 => Base58,
        FlowflakeIdCodec.Base36 => Base36,
        FlowflakeIdCodec.CrockfordBase32 => CrockfordBase32,
        FlowflakeIdCodec.Hex => Hex,
        FlowflakeIdCodec.Base64Url => Base64Url,
        _ => throw new ArgumentOutOfRangeException(nameof(codec), codec, "Unsupported codec format")
    };
}