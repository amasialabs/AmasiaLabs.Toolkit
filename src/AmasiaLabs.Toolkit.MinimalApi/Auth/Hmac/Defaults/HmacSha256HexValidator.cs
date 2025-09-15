using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac.Defaults;

/// <summary>
/// HMAC-SHA256 validator for lower-case hex signatures using the registered
/// <see cref="IHmacSignatureSigner"/>. Uses constant-time comparison and supports
/// bypass via <see cref="SignatureOptions.CheckSignature"/>.
/// </summary>
public sealed class HmacSha256HexValidator(IHmacSignatureSigner signer, IOptions<SignatureOptions> options)
    : IHmacSignatureValidator
{
    public bool ValidateSignature(string key, string signature, string payload)
    {
        if (!options.Value.CheckSignature)
            return true;

        var expected = signer.ComputeSignature(key, payload);

        // Constant-time compare on UTF-8 bytes of the signature strings
        var a = Encoding.UTF8.GetBytes(expected);
        var b = Encoding.UTF8.GetBytes(signature);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
}
