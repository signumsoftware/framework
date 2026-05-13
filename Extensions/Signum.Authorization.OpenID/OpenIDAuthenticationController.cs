using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;
using Signum.Authorization.AuthToken;
using System.ComponentModel.DataAnnotations;

namespace Signum.Authorization.OpenID;

[ValidateModelFilter]
public class OpenIDAuthenticationController : ControllerBase
{
    [HttpPost("api/auth/loginWithOpenID"), SignumAllowAnonymous]
    public LoginResponse? LoginWithOpenID([FromBody, Required] LoginWithOpenIDRequest request, [FromQuery] bool throwErrors = true)
    {
        if (!OpenIDAuthenticationServer.LoginOpenIDAuthentication(ControllerContext, request, throwErrors))
            return null;

        var user = UserEntity.Current.Retrieve();
        var token = AuthTokenServer.CreateToken(user);
        return new LoginResponse { userEntity = user, token = token, authenticationType = "openID" };
    }
}

public class LoginWithOpenIDRequest
{
    public string Code { get; set; }
    public string RedirectUri { get; set; }
}
