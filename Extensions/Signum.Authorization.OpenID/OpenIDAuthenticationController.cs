using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;
using Signum.Authorization.AuthToken;
using Signum.Authorization.OpenID.Authorizer;
using System.ComponentModel.DataAnnotations;

namespace Signum.Authorization.OpenID;

[ValidateModelFilter]
public class OpenIDAuthenticationController : ControllerBase
{
    [HttpPost("api/auth/loginWithOpenID"), SignumAllowAnonymous]
    public async Task<LoginResponse?> LoginWithOpenID([FromBody, Required] LoginWithOpenIDRequest request, [FromQuery] bool throwErrors = true)
    {
        if (!await OpenIDAuthenticationServer.LoginOpenIDAuthentication(ControllerContext, request, throwErrors))
            return null;

        var user = UserEntity.Current.Retrieve();
        var token = AuthTokenServer.CreateToken(user);
        return new LoginResponse { userEntity = user, token = token, authenticationType = "openID" };
    }

    [HttpGet("api/auth/openIDEndpoints"), SignumAllowAnonymous]
    public async Task<OpenIDEndpointsResponse> GetOpenIDEndpoints()
    {
        var authorizer = (OpenIDAuthorizer)AuthLogic.Authorizer!;
        var config = authorizer.GetConfig()
            ?? throw new InvalidOperationException("OpenID is not configured");

        var discovery = await OpenIDAuthenticationServer.GetDiscoveryDocument(config);
        return new OpenIDEndpointsResponse
        {
            AuthorizationEndpoint = discovery.AuthorizationEndpoint,
            EndSessionEndpoint = discovery.EndSessionEndpoint,
        };
    }
}

public class OpenIDEndpointsResponse
{
    public string AuthorizationEndpoint { get; set; } = null!;
    public string? EndSessionEndpoint { get; set; }
}

public class LoginWithOpenIDRequest
{
    public string Code { get; set; }
    public string RedirectUri { get; set; }
}
