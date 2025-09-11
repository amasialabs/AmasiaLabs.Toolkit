namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac;

/// <summary>
/// Generic signature behavior options, independent of a specific algorithm.
/// </summary>
public sealed class SignatureOptions
{
    /// <summary>
    /// When false, <see cref="IHmacSignatureValidator"/> always returns true.
    /// Default: true (validate signatures).
    /// </summary>
    public bool CheckSignature { get; set; } = true;

    /// <summary>
    /// When true, the payload is normalized by removing CR, LF, and TAB before signing/validation.
    /// Default: false.
    /// </summary>
    public bool Trim { get; set; } = false;
}

