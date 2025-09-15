using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace AmasiaLabs.Toolkit.MinimalApi.Auth.Jwt.UsernamePasswordAuth;

/// <summary>
/// Maps a domain user to a set of claims for JWT issuance.
/// </summary>
/// <typeparam name="TUser">Domain user type.</typeparam>
public interface IClaimsMapper<in TUser>
{
    IEnumerable<Claim> Map(HttpContext context, TUser user);
}

