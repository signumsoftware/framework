using System.Security.Claims;

#pragma warning disable CA1416 // Validate platform compatibility
namespace Signum.Authorization.AzureAD.Authorizer;

public class AzureClaimsAutoCreateUserContext : IAutoCreateUserContext
{
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


    public string AccessToken; 
    public AzureClaimsAutoCreateUserContext(ClaimsPrincipal claimsPrincipal, string accessToken)
    {
        ClaimsPrincipal = claimsPrincipal;
        AccessToken = accessToken;
    }
}
