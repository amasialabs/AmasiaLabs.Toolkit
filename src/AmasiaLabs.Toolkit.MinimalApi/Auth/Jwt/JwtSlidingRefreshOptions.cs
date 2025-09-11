namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;

/// <summary>
/// Controls when sliding refresh is triggered after token validation.
/// </summary>
public sealed class JwtSlidingRefreshOptions
{
    /// <summary>
    /// If the time to expiration is less than this threshold, a refresh is performed.
    /// Default: 5 minutes.
    /// </summary>
    public TimeSpan RefreshThreshold { get; set; } = TimeSpan.FromMinutes(5);
}

