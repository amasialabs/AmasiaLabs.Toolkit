namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac;

/// <summary>
/// Selects the payload format used by the HMAC authentication handler when
/// computing/validating the request signature. Defaults to <see cref="BodyOnly"/>
/// to preserve compatibility with existing clients.
/// </summary>
public enum HmacPayloadMode
{
    /// <summary>
    /// The signature is computed over the raw request body only. This is the
    /// historical default — existing clients that sign just the body keep working.
    /// Timestamp/nonce/clock-skew checks are skipped in this mode.
    /// </summary>
    BodyOnly = 0,

    /// <summary>
    /// The signature is computed over a canonical-request string composed of:
    /// <c>METHOD\nPATH\nQUERY\nTIMESTAMP\nNONCE\nSHA256_HEX(BODY)</c>
    /// Requires a timestamp header and a nonce header on every request.
    /// Enables clock-skew checking and (when an <see cref="IHmacNonceStore"/>
    /// is registered) replay protection. Opt-in.
    /// </summary>
    CanonicalRequest = 1,
}
