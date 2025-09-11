namespace AmasiaLabs.Toolkit.MinimalApi.Auth.ApiKey;

/// <summary>
/// Provides information about API keys (e.g., resolves a subject for a given key).
/// Return null when the key is unknown or invalid.
/// </summary>
public interface IApiKeyProvider
{
    /// <summary>
    /// Resolves a subject (identity) for the provided API key, or null if invalid.
    /// </summary>
    /// <param name="apiKey">The API key value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Subject/identity string, or null when invalid/unknown.</returns>
    Task<string?> GetSubjectAsync(string apiKey, CancellationToken cancellationToken = default);
}
