using System.Buffers.Text;
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

        // Base64Url emits the URL-safe alphabet without padding directly, so there is
        // no intermediate byte[]/string or +///= character substitution to undo.
        return Base64Url.EncodeToString(tmp[i..8]);
    }

    public long Decode(string text)
    {
        if (string.IsNullOrEmpty(text)) throw new ArgumentException("Value cannot be null or empty", nameof(text));
        if (text == "0") return 0;

        // Decode the URL-safe alphabet (padding optional) straight into an 8-byte buffer — the
        // widest a valid id occupies. A value decoding to more than 8 bytes is rejected rather
        // than silently truncated to its low 8 bytes. Characters outside the URL-safe alphabet
        // (including standard-Base64 '+' and '/') throw FormatException; note the BCL decoder
        // does tolerate embedded ASCII whitespace.
        Span<byte> bytes = stackalloc byte[8];
        if (!Base64Url.TryDecodeFromChars(text, bytes, out int written))
            throw new FormatException($"Invalid Base64Url value: '{text}'");

        ulong acc = 0;
        for (int i = 0; i < written; i++)
            acc = (acc << 8) | bytes[i];
        return (long)acc;
    }
}
