using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;

namespace AmasiaLabs.Toolkit.FlowflakeId.Extensions.Codecs;

/// <summary>
/// Base36 codec using 0-9 and a-z characters.
/// </summary>
public sealed class NumericBase36Codec : IIdCodec
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
    // Base36 is case-insensitive: fold so 'A' and 'a' map to the same index.
    private static readonly sbyte[] Map = CodecCharMap.Build(Alphabet, foldCase: true);

    public string Encode(long value)
    {
        if (value == 0) return "0";
        ulong x = (ulong)value;
        Span<char> buf = stackalloc char[13];
        int pos = buf.Length;
        while (x > 0)
        {
            ulong rem = x % 36UL;
            x /= 36UL;
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
            if (v < 0) throw new FormatException($"Invalid Base36 character: '{ch}'");
            acc = checked(acc * 36UL + (uint)v);
        }
        return (long)acc;
    }
}