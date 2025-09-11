namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac;

/// <summary>
/// Computes an HMAC signature for a payload using a shared secret.
/// </summary>
public interface IHmacSignatureSigner
{
    /// <summary>
    /// Computes a signature using a shared secret and payload.
    /// </summary>
    /// <param name="key">Shared secret.</param>
    /// <param name="payload">Payload to sign.</param>
    /// <returns>Signature string.</returns>
    string ComputeSignature(string key, string payload);
}
