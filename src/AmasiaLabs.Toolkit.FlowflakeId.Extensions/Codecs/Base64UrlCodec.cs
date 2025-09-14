using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;

namespace AmasiaLabs.Toolkit.FlowflakeId.Extensions.Codecs;

public sealed class Base64UrlCodec : IIdCodec
{
    public string Encode(long value)
    {
        if (value == 0) return "0";
        ulong x = (ulong)value;
        Span<byte> tmp = stackalloc byte[8];
        int i = 8;
        while (x > 0)
        {
            tmp[--i] = (byte)(x & 0xFF);
            x >>= 8;
        }
        var data = tmp[i..8].ToArray(); // Convert.ToBase64String требует byte[]
        string s = Convert.ToBase64String(data)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return s;
    }

    public long Decode(string text)
    {
        if (string.IsNullOrEmpty(text)) throw new ArgumentException("Value cannot be null or empty", nameof(text));
        if (text == "0") return 0;

        string s = text.Replace('-', '+').Replace('_', '/');
        int pad = (4 - s.Length % 4) & 3;
        if (pad != 0) s = s + new string('=', pad);

        byte[] bytes = Convert.FromBase64String(s);
        ulong acc = 0;
        foreach (byte b in bytes)
        {
            acc = (acc << 8) | b;
        }
        return (long)acc;
    }
}