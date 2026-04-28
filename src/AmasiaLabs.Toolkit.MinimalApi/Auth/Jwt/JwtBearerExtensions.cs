using System.Text;
using AmasiaLabs.Toolkit.MinimalApi.Problems;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;

/// <summary>
/// JWT Bearer authentication helpers with sensible defaults:
/// - Reads token from cookie (default: "jc") when Authorization header is absent.
/// - Emits RFC 7807 ProblemDetails for 401/403 using ProblemHandlingOptions.
/// - Composes (rather than replaces) user-provided JwtBearerEvents handlers, so
///   custom OnMessageReceived/OnTokenValidated/etc coexist with toolkit behavior.
/// - Provides configuration-based, explicit, and options-object overloads.
/// </summary>
public static class JwtBearerExtensions
{
    private const string DefaultJwtSectionPath = "Amasia:Toolkit:MinimalApi:Jwt";
    private const string DefaultCookieName = "jc";

    /// <summary>
    /// Adds JWT Bearer authentication with defaults and reads configuration values.
    /// Uses configuration keys under "Amasia:Toolkit:MinimalApi:Jwt" (Issuer, Audience, Key).
    /// Also configures cookie token extraction and ProblemDetails for 401/403.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="cookieName">Cookie name to read the token from (default: "jc").</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string cookieName = DefaultCookieName)
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
    /// Adds JWT Bearer authentication with defaults and reads configuration values,
    /// while allowing further scheme configuration (e.g., enabling sliding refresh).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="cookieName">Cookie name to read the token from (default: "jc").</param>
    /// <param name="configure">Additional configuration for <see cref="JwtBearerOptions"/>.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string cookieName,
        Action<JwtBearerOptions> configure)
    {
        var (issuer, audience, signingKey) = ResolveJwtSettings(configuration);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearerWithProblemDetails(issuer, audience, signingKey, cookieName, configure);

        return services;
    }

    /// <summary>
    /// Adds JWT Bearer authentication using an explicit <see cref="JwtAuthenticationOptions"/>.
    /// Composes the toolkit's cookie extraction and ProblemDetails 401/403 events on top of any
    /// events configured via <see cref="JwtAuthenticationOptions.ConfigureJwtBearer"/>.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        JwtAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        services
            .AddAuthentication(authOptions =>
            {
                authOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                authOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearerWithProblemDetails(options);

        return services;
    }

    /// <summary>
    /// Adds a JWT bearer handler to the builder with defaults, reading configuration values.
    /// Uses configuration keys under "Amasia:Toolkit:Jwt" (Issuer, Audience, Key).
    /// Falls back to a legacy "Jwt" section when the default path is missing.
    /// Also configures cookie token extraction and ProblemDetails for 401/403.
    /// </summary>
    /// <param name="builder">Authentication builder.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="cookieName">Cookie name to read the token from (default: "jc").</param>
    /// <returns>The authentication builder.</returns>
    public static AuthenticationBuilder AddJwtBearerWithProblemDetails(
        this AuthenticationBuilder builder,
        IConfiguration configuration,
        string cookieName = DefaultCookieName)
    {
        var (issuer, audience, signingKey) = ResolveJwtSettings(configuration);
        return builder.AddJwtBearerWithProblemDetails(issuer, audience, signingKey, cookieName);
    }

    /// <summary>
    /// Adds a JWT bearer handler to the builder with defaults, reading configuration values,
    /// and allows additional <see cref="JwtBearerOptions"/> configuration (e.g., sliding refresh).
    /// </summary>
    /// <param name="builder">Authentication builder.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="cookieName">Cookie name to read the token from (default: "jc").</param>
    /// <param name="configure">Additional configuration for <see cref="JwtBearerOptions"/>.</param>
    /// <returns>The authentication builder.</returns>
    public static AuthenticationBuilder AddJwtBearerWithProblemDetails(
        this AuthenticationBuilder builder,
        IConfiguration configuration,
        string cookieName,
        Action<JwtBearerOptions> configure)
    {
        var (issuer, audience, signingKey) = ResolveJwtSettings(configuration);
        return builder.AddJwtBearerWithProblemDetails(issuer, audience, signingKey, cookieName, configure);
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
        string cookieName = DefaultCookieName,
        Action<JwtBearerOptions>? configure = null)
    {
        return builder.AddJwtBearerWithProblemDetails(new JwtAuthenticationOptions
        {
            Issuer = issuer,
            Audience = audience,
            SigningKey = signingKey,
            CookieName = cookieName,
            ConfigureJwtBearer = configure,
        });
    }

    /// <summary>
    /// Adds a JWT bearer handler configured via an explicit <see cref="JwtAuthenticationOptions"/>.
    /// The toolkit's cookie extraction and ProblemDetails 401/403 handlers are composed on top of
    /// any events configured via <see cref="JwtAuthenticationOptions.ConfigureJwtBearer"/>, so
    /// custom event handlers coexist with toolkit behavior.
    /// </summary>
    public static AuthenticationBuilder AddJwtBearerWithProblemDetails(
        this AuthenticationBuilder builder,
        JwtAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Audience);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SigningKey);

        return builder.AddJwtBearer(jbo =>
        {
            jbo.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = options.Issuer,
                ValidAudience = options.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
            };
            options.ConfigureTokenValidationParameters?.Invoke(jbo.TokenValidationParameters);

            // Run user's JwtBearerOptions configuration FIRST, so any event handlers
            // they assign are visible to the composition step below.
            options.ConfigureJwtBearer?.Invoke(jbo);

            ComposeToolkitEvents(jbo, options.CookieName, options.ReadTokenFromCookie);
        });
    }

    /// <summary>
    /// Wraps the user's <see cref="JwtBearerEvents"/> in a forwarding subclass that runs the
    /// inner handler first and then chains toolkit logic. The wrapper overrides every virtual
    /// event method (not just delegate properties), so toolkit behavior survives both extension
    /// styles: assigning delegate properties (<c>o.Events.OnFoo = ...</c>) AND subclassing
    /// <see cref="JwtBearerEvents"/> with overridden virtual methods.
    ///
    /// Idempotent: if the events are already a <see cref="ToolkitJwtBearerEvents"/> (e.g. when
    /// AddJwtBearerWithProblemDetails is called more than once on the same options instance via
    /// configuration callback chaining), the call is a no-op rather than recursively wrapping.
    /// First call's cookieName / readTokenFromCookie wins.
    /// </summary>
    private static void ComposeToolkitEvents(JwtBearerOptions jbo, string cookieName, bool readTokenFromCookie)
    {
        if (jbo.Events is ToolkitJwtBearerEvents)
            return;

        var inner = jbo.Events ?? new JwtBearerEvents();
        jbo.Events = new ToolkitJwtBearerEvents(inner, cookieName, readTokenFromCookie);
    }

    private sealed class ToolkitJwtBearerEvents(JwtBearerEvents inner, string cookieName, bool readTokenFromCookie)
        : JwtBearerEvents
    {
        public override async Task MessageReceived(MessageReceivedContext context)
        {
            await inner.MessageReceived(context);

            if (readTokenFromCookie
                && string.IsNullOrEmpty(context.Token)
                && !string.IsNullOrEmpty(cookieName)
                && context.Request.Cookies.TryGetValue(cookieName, out var token)
                && !string.IsNullOrWhiteSpace(token))
            {
                context.Token = token;
            }
        }

        public override async Task Challenge(JwtBearerChallengeContext context)
        {
            await inner.Challenge(context);

            // Respect explicit user takeover. Handled is set by ctx.HandleResponse(), which
            // signals "I'll handle this myself" even if no body has been written yet.
            if (context.Handled || context.Response.HasStarted)
                return;

            context.HandleResponse();
            await ProblemDetailsWriter.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized, "Unauthorized");
        }

        public override async Task Forbidden(ForbiddenContext context)
        {
            await inner.Forbidden(context);

            if (context.Response.HasStarted)
                return;

            await ProblemDetailsWriter.WriteAsync(context.HttpContext, StatusCodes.Status403Forbidden, "Forbidden");
        }

        // Forward the remaining events untouched so user overrides/delegates survive.
        public override Task TokenValidated(TokenValidatedContext context)
            => inner.TokenValidated(context);

        public override Task AuthenticationFailed(AuthenticationFailedContext context)
            => inner.AuthenticationFailed(context);
    }

    private static (string Issuer, string Audience, string Key) ResolveJwtSettings(IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(DefaultJwtSectionPath);

        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var key = jwtSection["Key"];

        if (string.IsNullOrWhiteSpace(issuer))
            throw new InvalidOperationException("JWT configuration is missing Issuer (Amasia:Toolkit:MinimalApi:Jwt:Issuer).");
        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("JWT configuration is missing Audience (Amasia:Toolkit:MinimalApi:Jwt:Audience).");
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("JWT signing key is missing (Amasia:Toolkit:MinimalApi:Jwt:Key).");

        return (issuer, audience, key);
    }
}
