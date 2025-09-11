namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac;

/// <summary>
/// Validates an HMAC signature for a given payload using a shared secret.
/// Implement this in your application to encapsulate the signing algorithm.
/// </summary>
public interface IHmacSignatureValidator
{
    /// <summary>
    /// Validates the provided signature using a shared secret and request payload.
    /// </summary>
    /// <param name="key">The shared secret used for HMAC.</param>
    /// <param name="signature">The signature received from the client.</param>
    /// <param name="payload">The request payload used to compute/validate the signature.</param>
    /// <returns>True if signature is valid; otherwise false.</returns>
    bool ValidateSignature(string key, string signature, string payload);
}
