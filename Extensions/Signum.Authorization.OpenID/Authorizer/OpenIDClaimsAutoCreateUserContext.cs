using Signum.Authorization.BaseAD;
using System.Security.Claims;

namespace Signum.Authorization.OpenID.Authorizer;

public class OpenIDClaimsAutoCreateUserContext : IAutoCreateUserContext
{
    public string AccessToken { get; }
    public OpenIDConfigurationEmbedded Config { get; }
    BaseADConfigurationEmbedded IAutoCreateUserContext.Config => Config;
    public ClaimsPrincipal ClaimsPrincipal { get; }

    public OpenIDClaimsAutoCreateUserContext(ClaimsPrincipal claimsPrincipal, string accessToken, OpenIDConfigurationEmbedded config)
    {
        ClaimsPrincipal = claimsPrincipal;
        AccessToken = accessToken;
        Config = config;
    }

    public string GetClaim(string type) => ClaimsPrincipal.Claims.SingleEx(a => a.Type == type).Value;
    public string? TryGetClaim(string type) => ClaimsPrincipal.Claims.SingleOrDefaultEx(a => a.Type == type)?.Value;

    public virtual string? ExternalId => TryGetClaim("sub");

    public virtual string UserName =>
        TryGetClaim("preferred_username") ??
        TryGetClaim("email") ??
        GetClaim("sub");

    public virtual string? EmailAddress =>
        TryGetClaim("email") ??
        (TryGetClaim("preferred_username") is { } u && u.Contains('@') ? u : null);

    public virtual string FirstName =>
        TryGetClaim("given_name") ??
        (FullName?.TryBefore(" ")?.Trim()) ??
        UserName;

    public virtual string LastName =>
        TryGetClaim("family_name") ??
        (FullName?.TryAfter(" ")?.Trim()) ??
        "Unknown";

    protected virtual string? FullName => TryGetClaim("name");
}
