using Signum.Authorization.BaseAD;
using System.Security.Claims;

#pragma warning disable CA1416 // Validate platform compatibility
namespace Signum.Authorization.AzureAD.Authorizer;

public class AzureClaimsAutoCreateUserContext : IAutoCreateUserContext
{
    public string AccessToken { get; }

    AzureADConfigurationEmbedded Config { get; }
    BaseADConfigurationEmbedded IAutoCreateUserContext.Config => Config;

    public AzureClaimsAutoCreateUserContext(ClaimsPrincipal claimsPrincipal, string accessToken, AzureADConfigurationEmbedded config)
    {
        ClaimsPrincipal = claimsPrincipal;
        AccessToken = accessToken;
        this.Config = config;
    }

    public ClaimsPrincipal ClaimsPrincipal { get; private set; }

    public string GetClaim(string type) => ClaimsPrincipal.Claims.SingleEx(a => a.Type == type).Value;

    public string? TryGetClaim(string type) => ClaimsPrincipal.Claims.SingleOrDefaultEx(a => a.Type == type)?.Value;

    public virtual Guid? OID
    {
        get
        {
            var oid = TryGetClaim("http://schemas.microsoft.com/identity/claims/objectidentifier")
                   ?? TryGetClaim("oid"); // fallback for AAD v2.0
            return oid != null ? Guid.Parse(oid) : null;
        }
    }

    public string? SID => null;

    public virtual string UserName => GetClaim("preferred_username");
    public virtual string? EmailAddress => GetClaim("preferred_username");

    public virtual string? FullName => TryGetClaim("name");

    public virtual string FirstName
    {
        get
        {
            var name = FullName;

            return name == null ? "Unknown" : 
                name.Contains(",") ? name.After(",").Trim() :
                name.TryBefore(" ")?.Trim() ?? name.DefaultToNull() ?? "Unknown";
        }
    }

    public virtual string LastName
    {
        get
        {
            var name = FullName;

            return name == null ? "Unknown" : 
                name.Contains(",") ? name.Before(",").Trim() : 
                name.TryAfter(" ")?.Trim() ??  "Unknown";
        }
    }
}


public class AzureExternalIDAutoCreateUserContext : AzureClaimsAutoCreateUserContext
{
    public AzureExternalIDAutoCreateUserContext(ClaimsPrincipal claimsPrincipal, string accessToken, AzureADConfigurationEmbedded config) : base(claimsPrincipal, accessToken, config)
    {
    }
}

public class AzureB2CClaimsAutoCreateUserContext : AzureClaimsAutoCreateUserContext
{

    public override string UserName => GetClaim("emails");
    public override string? EmailAddress => GetClaim("emails");

    public override string? FullName => TryGetClaim("name") ?? " ".Combine(FirstName, LastName);
    public override string FirstName => GetClaim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname");
    public override string LastName => GetClaim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname");

    public override Guid? OID => Guid.Parse(GetClaim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"));

    public AzureB2CClaimsAutoCreateUserContext(ClaimsPrincipal claimsPrincipal, string accessToken, AzureADConfigurationEmbedded config) : base(claimsPrincipal, accessToken, config)
    {
    }
}


