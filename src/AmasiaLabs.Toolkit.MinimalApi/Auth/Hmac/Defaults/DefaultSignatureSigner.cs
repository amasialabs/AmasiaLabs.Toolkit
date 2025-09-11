using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac.Defaults;

/// <summary>
/// Default signer: HMAC-SHA256 with lower-case hex output. Applies optional payload trimming from <see cref="SignatureOptions"/>.
/// </summary>
public sealed class DefaultSignatureSigner : IHmacSignatureSigner
{
    private readonly IOptions<SignatureOptions> options;

    public DefaultSignatureSigner(IOptions<SignatureOptions> options) => this.options = options;

    public string ComputeSignature(string key, string payload)
    {
        if (options.Value.Trim)
        {
            payload = payload.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace("\t", string.Empty);
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return ConvertToLowerHex(hash);
    }

    private static string ConvertToLowerHex(byte[] bytes)
    {
        var c = new char[bytes.Length * 2];
        var i = 0;
        foreach (var b in bytes)
        {
            c[i++] = GetHexNibble(b >> 4);
            c[i++] = GetHexNibble(b & 0xF);
        }
        return new string(c);
    }

    private static char GetHexNibble(int value)
        => (char)(value < 10 ? ('0' + value) : ('a' + (value - 10)));
}

