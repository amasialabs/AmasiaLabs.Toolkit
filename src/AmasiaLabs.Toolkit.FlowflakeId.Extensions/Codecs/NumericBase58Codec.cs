using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;

namespace AmasiaLabs.Toolkit.FlowflakeId.Extensions.Codecs;

public class NumericBase58Codec : IIdCodec
{
    private const string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
    private static readonly sbyte[] Map = CodecCharMap.Build(Alphabet);

    public string Encode(long value)
    {
        if (value == 0) return "1";
        ulong x = (ulong)value;
        Span<char> buf = stackalloc char[11];
        int pos = buf.Length;
        while (x > 0)
        {
            ulong rem = x % 58UL;
            x /= 58UL;
            buf[--pos] = Alphabet[(int)rem];
        }
        return new string(buf[pos..]);
    }

    public long Decode(string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        ulong acc = 0;
        foreach (char ch in text)
        {
            int v = CodecCharMap.IndexOf(Map, ch);
            if (v < 0) throw new FormatException($"Invalid Base58 character: '{ch}'");
            acc = checked(acc * 58UL + (uint)v);
        }
        return (long)acc;
    }
}