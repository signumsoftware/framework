using System.Security.Claims;

#pragma warning disable CA1416 // Validate platform compatibility

namespace Signum.Authorization.AzureAD.Authorizer;

public class AzureB2CClaimsAutoCreateUserContext : AzureClaimsAutoCreateUserContext
{
  
    public override string UserName => GetClaim("emails");
    public override string? EmailAddress => GetClaim("emails");

    public override string? FullName => TryGetClaim("name") ?? " ".Combine(FirstName, LastName);
    public override string FirstName => GetClaim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname");
    public override string LastName => GetClaim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname");

    public override Guid? OID => Guid.Parse(GetClaim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"));

    public AzureB2CClaimsAutoCreateUserContext(ClaimsPrincipal claimsPrincipal, string accessToken) : base(claimsPrincipal, accessToken)
    {
    }
}
