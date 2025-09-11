using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac.Defaults;

/// <summary>
/// HMAC-SHA256 signer that returns a Base64-encoded signature. Applies optional payload trimming.
/// </summary>
public sealed class HmacSha256Base64Signer : IHmacSignatureSigner
{
    private readonly IOptions<SignatureOptions>? options;

    public HmacSha256Base64Signer()
    {
    }

    public HmacSha256Base64Signer(IOptions<SignatureOptions> options) => this.options = options;

    public string ComputeSignature(string key, string payload)
    {
        if (options?.Value.Trim == true)
        {
            payload = payload.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace("\t", string.Empty);
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }
}
