namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac;

/// <summary>
/// Optional store that records observed (clientId, nonce, timestamp) tuples to provide
/// replay protection in <see cref="HmacPayloadMode.CanonicalRequest"/> mode.
///
/// Implementations should typically use a TTL store (Redis, MemoryCache, etc.) sized to at
/// least the configured <see cref="HmacAuthenticationOptions.AllowedClockSkew"/>; entries
/// older than that window cannot be successfully replayed regardless.
///
/// If no implementation is registered, the handler still validates the signature and the
/// timestamp window, but does not enforce single-use of nonces.
/// </summary>
public interface IHmacNonceStore
{
    /// <summary>
    /// Records the (clientId, nonce, timestamp) tuple as observed. Returns true if this is
    /// the first time the tuple has been seen and the request should be accepted; false if
    /// the tuple has already been observed (replay) and the request must be rejected.
    /// </summary>
    Task<bool> TryRegisterAsync(string clientId, string nonce, DateTimeOffset timestamp, CancellationToken cancellationToken = default);
}
