namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Hmac;

/// <summary>
/// Resolves a shared secret (key) for a given HMAC client id.
/// Implement this in your application to look up keys from configuration, DB, etc.
/// </summary>
public interface IHmacKeyProvider
{
    /// <summary>
    /// Returns a key (shared secret) for the provided client id.
    /// </summary>
    /// <param name="clientId">Client identifier passed by the caller.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The shared secret, or null if client id is unknown.</returns>
    Task<string?> GetKeyAsync(string clientId, CancellationToken cancellationToken = default);
}
