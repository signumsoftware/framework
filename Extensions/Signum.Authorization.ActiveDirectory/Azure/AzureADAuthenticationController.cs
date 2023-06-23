using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;
using Signum.Authorization.AuthToken;
using System.ComponentModel.DataAnnotations;

namespace Signum.Authorization.ActiveDirectory.Azure;

[ValidateModelFilter]
public class AzureADAuthenticationController : ControllerBase
{
    [HttpPost("api/auth/loginWithAzureAD"), SignumAllowAnonymous]
    public LoginResponse? LoginWithAzureAD([FromBody, Required] string jwt, [FromQuery] bool throwErrors = true)
    {
        if (!AzureADAuthenticationServer.LoginAzureADAuthentication(ControllerContext, jwt, throwErrors))
            return null;

        var user = UserEntity.Current.Retrieve();

        var token = AuthTokenServer.CreateToken(user);

        return new LoginResponse { userEntity = user, token = token, authenticationType = "azureAD" };
    }
}
