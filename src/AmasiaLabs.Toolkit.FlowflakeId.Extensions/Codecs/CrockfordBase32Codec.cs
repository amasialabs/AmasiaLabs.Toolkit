using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;

namespace AmasiaLabs.Toolkit.FlowflakeId.Extensions.Codecs;

// ReSharper disable once IdentifierTypo
public class CrockfordBase32Codec(bool withChecksum = false) : IIdCodec
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const string CheckAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ*";
    private static readonly sbyte[] Map = BuildMap();

    public string Encode(long value)
    {
        if (value == 0) return withChecksum ? "0" + CheckAlphabet[0] : "0";
        ulong x = (ulong)value;
        Span<char> buf = stackalloc char[14]; // 13 + чексимвол
        int pos = buf.Length;
        if (withChecksum) buf[--pos] = CheckAlphabet[(int)(x % 37)];
        while (x > 0)
        {
            ulong rem = x % 32UL;
            x /= 32UL;
            buf[--pos] = Alphabet[(int)rem];
        }
        return new string(buf[pos..]);
    }

    public long Decode(string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        var cleaned = text.Replace("-", "", StringComparison.Ordinal);
        int end = cleaned.Length;
        int payloadEnd = withChecksum ? end - 1 : end;

        ulong acc = 0;
        for (int i = 0; i < payloadEnd; i++)
        {
            char ch = cleaned[i];
            int v = CodecCharMap.IndexOf(Map, ch);
            if (v < 0) throw new FormatException($"Invalid Base32 character: '{ch}'");
            acc = checked(acc * 32UL + (uint)v);
        }

        if (withChecksum)
        {
            int idx = CheckAlphabet.IndexOf(char.ToUpperInvariant(cleaned[^1]));
            if (idx < 0 || idx != (int)(acc % 37)) throw new FormatException("Checksum mismatch");
        }

        return (long)acc;
    }

    private static sbyte[] BuildMap()
    {
        // Case-insensitive, plus Crockford's confusable aliases: O/o -> 0, I/i/L/l -> 1
        // (those letters are excluded from the alphabet itself).
        var m = CodecCharMap.Build(Alphabet, foldCase: true);
        m['o'] = m['O'] = 0;
        m['i'] = m['I'] = 1;
        m['l'] = m['L'] = 1;
        return m;
    }
}