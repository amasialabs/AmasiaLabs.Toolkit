using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;

namespace AmasiaLabs.Toolkit.FlowflakeId.Extensions.Codecs;

/// <summary>
/// Hexadecimal codec for Flowflake IDs.
/// </summary>
public sealed class HexCodec : IIdCodec
{
    public string Encode(long value)
    {
        if (value == 0) return "0";
        Span<byte> buf = stackalloc byte[8];
        ulong x = (ulong)value;
        int i = 8;
        while (x > 0)
        {
            buf[--i] = (byte)(x & 0xFF);
            x >>= 8;
        }
        return Convert.ToHexString(buf[i..8]);
    }

    public long Decode(string text)
    {
        if (string.IsNullOrEmpty(text)) throw new ArgumentException("Value cannot be null or empty", nameof(text));
        if (text == "0") return 0;
        if ((text.Length & 1) == 1) text = "0" + text;

        byte[] bytes = Convert.FromHexString(text);
        ulong acc = 0;
        foreach (byte b in bytes) acc = (acc << 8) | b;
        return (long)acc;
    }
}