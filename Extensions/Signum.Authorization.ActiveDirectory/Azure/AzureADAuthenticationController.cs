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
    public LoginResponse? LoginWithAzureAD([FromBody, Required] LoginWithAzureADRequest request,[FromQuery]bool azureB2C = false, [FromQuery] bool throwErrors = true)
    {
        if (!AzureADAuthenticationServer.LoginAzureADAuthentication(ControllerContext, request, azureB2C, throwErrors))
            return null;

        var user = UserEntity.Current.Retrieve();

        var token = AuthTokenServer.CreateToken(user);

        return new LoginResponse { userEntity = user, token = token, authenticationType = "azureAD" };
    }
}

public class LoginWithAzureADRequest
{
    public string idToken;
    public string accessToken; 
}
