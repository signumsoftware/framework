using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;
using Signum.Authorization.AuthToken;

namespace Signum.Authorization.ActiveDirectory.WindowsAuthentication;

[ValidateModelFilter]
public class WindowsAuthenticationController : ControllerBase
{
    [HttpPost("api/auth/loginWindowsAuthentication"), Authorize, SignumAllowAnonymous]
    public LoginResponse? LoginWindowsAuthentication(bool throwError)
    {
        if (!WindowsAuthenticationServer.LoginWindowsAuthentication(ControllerContext, throwError))
            return null;

        var user = UserEntity.Current.Retrieve();

        var token = AuthTokenServer.CreateToken(user);

        return new LoginResponse { userEntity = user, token = token, authenticationType = "windows" };
    }
}
