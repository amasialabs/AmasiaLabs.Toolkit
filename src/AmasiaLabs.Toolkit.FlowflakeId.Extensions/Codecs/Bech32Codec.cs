using System.Text;
using AmasiaLabs.Toolkit.FlowflakeId.Abstractions;

namespace AmasiaLabs.Toolkit.FlowflakeId.Extensions.Codecs;

public sealed class Bech32Codec : IIdCodec
{
    private readonly string _hrp;
    private readonly byte[] _hrpExpanded; // HrpExpand(_hrp), precomputed once (immutable per instance)
    private readonly bool _useM; // false = bech32, true = bech32m

    public Bech32Codec(string hrp, bool bech32M = false)
    {
        if (string.IsNullOrWhiteSpace(hrp))
            throw new ArgumentException("HRP required", nameof(hrp));
        _hrp = hrp.ToLowerInvariant();
        _hrpExpanded = HrpExpand(_hrp);
        _useM = bech32M;
    }

    public string Encode(long value)
    {
        // number -> minimal big-endian bytes
        byte[] bytes = ToMinimalBigEndian((ulong)value);
        // bytes -> 5-bit
        var five = ConvertBits(bytes, 8, 5, pad: true);
        // create checksum
        var combined = new List<byte>(five);
        var checksum = CreateChecksum(_hrpExpanded, combined, _useM);
        combined.AddRange(checksum);

        var sb = new StringBuilder(_hrp.Length + 1 + combined.Count);
        sb.Append(_hrp);
        sb.Append('1');
        foreach (byte b in combined) sb.Append(Chars[b]);
        return sb.ToString();
    }

    public long Decode(string text)
    {
        if (string.IsNullOrEmpty(text)) 
            throw new ArgumentException("Value cannot be null or empty", nameof(text));
        var s = text.ToLowerInvariant();
        var pos = s.LastIndexOf('1');
        if (pos < 1 || pos + 7 > s.Length) throw new FormatException("Invalid Bech32 length/position");

        var hrp = s[..pos];
        if (hrp != _hrp)
            throw new FormatException("HRP mismatch");

        // data
        var data = new List<byte>(s.Length - pos - 1);
        for (var i = pos + 1; i < s.Length; i++)
        {
            var v = DecodeChar(s[i]);
            if (v < 0) throw new FormatException($"Invalid Bech32 character: '{s[i]}'");
            data.Add((byte)v);
        }
        if (!VerifyChecksum(_hrpExpanded, data, _useM))
            throw new FormatException("Checksum mismatch");

        // remove checksum (6)
        data.RemoveRange(data.Count - 6, 6);
        var eight = ConvertBitsToBytes(data, 5, 8, strict: false);
        ulong acc = 0;
        foreach (byte b in eight) acc = (acc << 8) | b;
        return (long)acc;
    }

    // -------- internals --------
    private static readonly char[] Chars = "qpzry9x8gf2tvdw0s3jn54khce6mua7l".ToCharArray();
    private static readonly sbyte[] Map = CodecCharMap.Build(Chars);

    private static int DecodeChar(char c) => CodecCharMap.IndexOf(Map, c);

    private static byte[] ToMinimalBigEndian(ulong x)
    {
        if (x == 0) return [0];
        Span<byte> buf = stackalloc byte[8];
        int i = 8;
        while (x > 0)
        {
            buf[--i] = (byte)(x & 0xFF);
            x >>= 8;
        }
        return buf[i..8].ToArray();
    }

    // ReSharper disable once CognitiveComplexity
    private static List<byte> ConvertBits(byte[] data, int fromBits, int toBits, bool pad)
    {
        int acc = 0;
        int bits = 0;
        int maxv = (1 << toBits) - 1;
        var ret = new List<byte>();
        foreach (var value in data)
        {
            if ((value >> fromBits) != 0) throw new ArgumentException("Invalid data range");
            acc = (acc << fromBits) | value;
            bits += fromBits;
            while (bits >= toBits)
            {
                bits -= toBits;
                ret.Add((byte)((acc >> bits) & maxv));
            }
        }
        if (pad)
        {
            if (bits > 0) ret.Add((byte)((acc << (toBits - bits)) & maxv));
        }
        else if (bits >= fromBits || ((acc << (toBits - bits)) & maxv) != 0)
        {
            throw new FormatException("Invalid padding");
        }
        return ret;
    }

    private static byte[] ConvertBitsToBytes(List<byte> data, int fromBits, int toBits, bool strict)
    {
        int acc = 0;
        int bits = 0;
        int maxv = (1 << toBits) - 1;
        var ret = new List<byte>();
        foreach (var v in data)
        {
            if ((v >> fromBits) != 0) throw new ArgumentException("Invalid data range");
            acc = (acc << fromBits) | v;
            bits += fromBits;
            while (bits >= toBits)
            {
                bits -= toBits;
                ret.Add((byte)((acc >> bits) & maxv));
            }
        }
        if (!strict)
        {
            if (bits > 0) ret.Add((byte)((acc << (toBits - bits)) & maxv));
        }
        else if (bits >= fromBits || ((acc << (toBits - bits)) & maxv) != 0)
        {
            throw new FormatException("Invalid padding");
        }
        return ret.ToArray();
    }

    // ReSharper disable once CognitiveComplexity
    private static uint PolyMod(ReadOnlySpan<byte> values)
    {
        uint c = 1;
        foreach (byte v in values)
        {
            uint c0 = c >> 25;
            c = (c & 0x1ffffff) << 5 ^ v;
            if ((c0 & 1) != 0) c ^= 0x3b6a57b2;
            if ((c0 & 2) != 0) c ^= 0x26508e6d;
            if ((c0 & 4) != 0) c ^= 0x1ea119fa;
            if ((c0 & 8) != 0) c ^= 0x3d4233dd;
            if ((c0 & 16) != 0) c ^= 0x2a1462b3;
        }
        return c;
    }

    private static byte[] HrpExpand(string hrp)
    {
        var ret = new byte[hrp.Length * 2 + 1];
        var i = 0;
        foreach (char ch in hrp) ret[i++] = (byte)(ch >> 5);
        ret[i++] = 0;
        foreach (char ch in hrp) ret[i++] = (byte)(ch & 31);
        return ret;
    }

    private static byte[] CreateChecksum(byte[] hrpExpanded, List<byte> data, bool useM)
    {
        byte[] values = [.. hrpExpanded, .. data, 0, 0, 0, 0, 0, 0];
        uint mod = PolyMod(values) ^ (useM ? 0x2bc830a3u : 1u);
        var ret = new byte[6];
        for (int i = 0; i < 6; i++) ret[i] = (byte)((mod >> (5 * (5 - i))) & 31);
        return ret;
    }

    private static bool VerifyChecksum(byte[] hrpExpanded, List<byte> data, bool useM)
    {
        byte[] values = [.. hrpExpanded, .. data];
        return PolyMod(values) == (useM ? 0x2bc830a3u : 1u);
    }
}
