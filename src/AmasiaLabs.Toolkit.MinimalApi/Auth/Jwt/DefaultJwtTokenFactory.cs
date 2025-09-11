using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt;

internal sealed class DefaultJwtTokenFactory(IOptions<JwtTokenFactoryOptions> options) : IJwtTokenFactory
{
    private readonly JwtTokenFactoryOptions _opts = options.Value;

    public string CreateToken(IEnumerable<Claim> claims, JwtBearerOptions bearerOptions, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(bearerOptions);
        var tvp = bearerOptions.TokenValidationParameters;
        if (tvp.IssuerSigningKey is not SymmetricSecurityKey key)
            throw new InvalidOperationException("Only symmetric keys are supported by DefaultJwtTokenFactory.");

        var identity = new ClaimsIdentity(claims);
        var handler = new JwtSecurityTokenHandler();
        var current = now ?? DateTimeOffset.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = identity,
            Expires = current.UtcDateTime.Add(_opts.TokenLifetime),
            NotBefore = current.UtcDateTime.AddSeconds(-1),
            Issuer = _opts.Issuer ?? tvp.ValidIssuer,
            Audience = _opts.Audience ?? tvp.ValidAudience,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }
}
