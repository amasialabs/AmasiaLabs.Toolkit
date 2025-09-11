using System.Text;
using AmasiaLabs.Toolkit.MinimalApi.Problems;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;

/// <summary>
/// JWT Bearer authentication helpers with sensible defaults:
/// - Reads token from cookie (default: "jc") when Authorization header is absent.
/// - Emits RFC 7807 ProblemDetails for 401/403 using ProblemHandlingOptions.
/// - Provides configuration-based and explicit overloads.
/// </summary>
public static class JwtBearerExtensions
{
    /// <summary>
    /// Adds JWT Bearer authentication with defaults and reads configuration values.
    /// Uses configuration keys: "Jwt:Issuer", "Jwt:Audience" and "Jwt:Key".
    /// Also configures cookie token extraction and ProblemDetails for 401/403.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="cookieName">Cookie name to read the token from (default: "jc").</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string cookieName = "jc")
    {
        var (issuer, audience, signingKey) = ResolveJwtSettings(configuration);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearerWithProblemDetails(issuer, audience, signingKey, cookieName);

        return services;
    }

    /// <summary>
    /// Adds a JWT bearer handler to the builder with defaults, reading configuration values.
    /// Uses configuration keys: "Jwt:Issuer", "Jwt:Audience" and "Jwt:Key".
    /// Also configures cookie token extraction and ProblemDetails for 401/403.
    /// </summary>
    /// <param name="builder">Authentication builder.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="cookieName">Cookie name to read the token from (default: "jc").</param>
    /// <returns>The authentication builder.</returns>
    public static AuthenticationBuilder AddJwtBearerWithProblemDetails(
        this AuthenticationBuilder builder,
        IConfiguration configuration,
        string cookieName = "jc")
    {
        var (issuer, audience, signingKey) = ResolveJwtSettings(configuration);
        return builder.AddJwtBearerWithProblemDetails(issuer, audience, signingKey, cookieName);
    }

    /// <summary>
    /// Adds a JWT bearer handler with provided settings and sensible defaults:
    /// - Validates issuer, audience, lifetime, and signing key.
    /// - Reads token from a cookie (if Authorization header is absent).
    /// - Emits RFC 7807 ProblemDetails for 401/403 using <see cref="ProblemHandlingOptions"/>.
    /// </summary>
    /// <param name="builder">Authentication builder.</param>
    /// <param name="issuer">Expected token issuer.</param>
    /// <param name="audience">Expected token audience.</param>
    /// <param name="signingKey">Symmetric signing key (UTF-8).</param>
    /// <param name="cookieName">Cookie name to read the token from (default: "jc").</param>
    /// <param name="configure">Optional additional configuration for <see cref="JwtBearerOptions"/>.</param>
    /// <returns>The authentication builder.</returns>
    public static AuthenticationBuilder AddJwtBearerWithProblemDetails(
        this AuthenticationBuilder builder,
        string issuer,
        string audience,
        string signingKey,
        string cookieName = "jc",
        Action<JwtBearerOptions>? configure = null)
    {
        return builder.AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (string.IsNullOrEmpty(context.Token))
                    {
                        if (!string.IsNullOrEmpty(cookieName) &&
                            context.Request.Cookies.TryGetValue(cookieName, out var token) &&
                            !string.IsNullOrWhiteSpace(token))
                        {
                            context.Token = token;
                        }
                    }
                    return Task.CompletedTask;
                },
                OnChallenge = ctx =>
                {
                    ctx.HandleResponse();
                    var opts = ctx.HttpContext.RequestServices.GetRequiredService<ProblemHandlingOptions>();
                    var status = StatusCodes.Status401Unauthorized;
                    var pd = new ProblemDetails
                    {
                        Status = status,
                        Title = "Unauthorized",
                        Detail = opts.GetMessage(status),
                        Instance = ctx.HttpContext.Request.Path,
                        Type = opts.TypeUriFactory(status),
                        Extensions =
                        {
                            ["traceId"] = ctx.HttpContext.TraceIdentifier
                        }
                    };
                    ctx.Response.StatusCode = status;
                    ctx.Response.ContentType = "application/problem+json";
                    return ctx.Response.WriteAsJsonAsync(pd);
                },
                OnForbidden = ctx =>
                {
                    var opts = ctx.HttpContext.RequestServices.GetRequiredService<ProblemHandlingOptions>();
                    var status = StatusCodes.Status403Forbidden;
                    var pd = new ProblemDetails
                    {
                        Status = status,
                        Title = "Forbidden",
                        Detail = opts.GetMessage(status),
                        Instance = ctx.HttpContext.Request.Path,
                        Type = opts.TypeUriFactory(status),
                        Extensions =
                        {
                            ["traceId"] = ctx.HttpContext.TraceIdentifier
                        }
                    };
                    ctx.Response.StatusCode = status;
                    ctx.Response.ContentType = "application/problem+json";
                    return ctx.Response.WriteAsJsonAsync(pd);
                }
            };

            configure?.Invoke(options);
        });
    }

    private static (string Issuer, string Audience, string Key) ResolveJwtSettings(IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var key = jwtSection["Key"];

        if (string.IsNullOrWhiteSpace(issuer))
            throw new InvalidOperationException("JWT configuration is missing 'Jwt:Issuer'.");
        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("JWT configuration is missing 'Jwt:Audience'.");
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("JWT signing key is missing. Provide 'Jwt:Key'.");

        return (issuer, audience, key);
    }
}
