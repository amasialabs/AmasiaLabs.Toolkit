using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac.Defaults;

/// <summary>
/// HMAC-SHA256 validator for Base64-encoded signatures.
/// Supports bypass via <see cref="SignatureOptions.CheckSignature"/> and optional payload trimming.
/// </summary>
public sealed class HmacSha256Base64Validator : IHmacSignatureValidator
{
    private readonly IOptions<SignatureOptions>? options;

    public HmacSha256Base64Validator()
    {
    }

    public HmacSha256Base64Validator(IOptions<SignatureOptions> options) => this.options = options;

    public bool ValidateSignature(string key, string signature, string payload)
    {
        if (options?.Value.CheckSignature == false)
            return true;

        if (options?.Value.Trim == true)
        {
            payload = payload.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace("\t", string.Empty);
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));

        try
        {
            var provided = Convert.FromBase64String(signature);
            return CryptographicOperations.FixedTimeEquals(provided, expected);
        }
        catch
        {
            return false;
        }
    }
}
