using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac.Defaults;

/// <summary>
/// Default validator using the registered <see cref="IHmacSignatureSigner"/> to compute the expected signature
/// and comparing with constant-time semantics. Supports bypass via <see cref="SignatureOptions.CheckSignature"/>.
/// </summary>
public sealed class DefaultSignatureValidator : IHmacSignatureValidator
{
    private readonly IHmacSignatureSigner signer;
    private readonly IOptions<SignatureOptions> options;

    public DefaultSignatureValidator(IHmacSignatureSigner signer, IOptions<SignatureOptions> options)
    {
        this.signer = signer;
        this.options = options;
    }

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

