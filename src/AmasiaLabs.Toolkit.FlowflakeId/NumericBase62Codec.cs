namespace AmasiaLabs.Toolkit.FlowflakeId;

/// <summary>
/// Default numeric Base62 codec. Note: alphabet may differ from external libraries.
/// Provide your own <see cref="IIdCodec"/> for strict compatibility.
/// </summary>
public sealed class NumericBase62Codec : IIdCodec
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public string Encode(long value)
    {
        if (value == 0) return "0";

        // Work with unsigned for division behavior
        var x = (ulong)value;
        Span<char> buffer = stackalloc char[11]; // up to 11 chars for 2^64 in base62
        var pos = buffer.Length;

        while (x > 0)
        {
            var rem = (int)(x % 62);
            x /= 62;
            buffer[--pos] = Alphabet[rem];
        }

        return new string(buffer[pos..]);
    }

    public long Decode(string text)
    {
        if (string.IsNullOrEmpty(text)) throw new ArgumentException("Value cannot be null or empty", nameof(text));

        ulong result = 0;
        foreach (var c in text)
        {
            var idx = Alphabet.IndexOf(c);
            if (idx < 0) throw new FormatException($"Invalid Base62 character: '{c}'");
            result = checked(result * 62 + (uint)idx);
        }

        return (long)result;
    }
}
