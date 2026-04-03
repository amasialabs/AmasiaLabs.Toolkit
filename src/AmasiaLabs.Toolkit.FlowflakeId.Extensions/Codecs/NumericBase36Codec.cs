using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;

namespace AmasiaLabs.Toolkit.FlowflakeId.Extensions.Codecs;

/// <summary>
/// Base36 codec using 0-9 and a-z characters.
/// </summary>
public sealed class NumericBase36Codec : IIdCodec
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
    private static readonly sbyte[] Map = BuildMap();

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
            int v = ch < 256 ? Map[(byte)ch] : -1;
            if (v < 0) throw new FormatException($"Invalid Base36 character: '{ch}'");
            acc = checked(acc * 36UL + (uint)v);
        }
        return (long)acc;
    }
    
    private static sbyte[] BuildMap()
    {
        var m = new sbyte[256];
        Array.Fill(m, (sbyte)-1);
        for (int i = 0; i <= 9; i++) { m['0' + i] = (sbyte)i; }
        for (int i = 0; i < 26; i++)
        {
            m['a' + i] = (sbyte)(10 + i);
            m['A' + i] = (sbyte)(10 + i);
        }
        return m;
    }
}