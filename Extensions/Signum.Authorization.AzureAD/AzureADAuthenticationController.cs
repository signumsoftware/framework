using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;
using Signum.Authorization.AuthToken;
using System.ComponentModel.DataAnnotations;

namespace Signum.Authorization.AzureAD;

[ValidateModelFilter]
public class AzureADAuthenticationController : ControllerBase
{
    [HttpPost("api/auth/loginWithAzureAD"), SignumAllowAnonymous]
    public LoginResponse? LoginWithAzureAD([FromBody, Required] LoginWithAzureADRequest request,[FromQuery]string adVariant, [FromQuery] bool throwErrors = true)
    {
        if (!AzureADAuthenticationServer.LoginAzureADAuthentication(ControllerContext, request, adVariant, throwErrors))
            return null;

        var user = UserEntity.Current.Retrieve();

        var token = AuthTokenServer.CreateToken(user);

        return new LoginResponse { userEntity = user, token = token, authenticationType = "azureAD" };
    }

    public static TimeSpan PictureMaxAge = new TimeSpan(7, 0, 0);

    [HttpGet("api/cachedAzureUserPhoto/{size}/{oID}")]
    public async Task<string?> GetCachedUserPhotoUrl(string oId, int size)
    {
        Response.GetTypedHeaders().CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
        {
            MaxAge = PictureMaxAge,
        };

        var cpp = await CachedProfilePhotoLogic.GetOrCreateCachedPicture(new Guid(oId), size);

        return cpp.Photo?.FullWebPath();
    }

    [HttpGet("api/azureUserPhoto/{size}/{oID}"), SignumAllowAnonymous]
    public Task<ActionResult> GetUserPhoto(string oId, int size)
    {
        Response.GetTypedHeaders().CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
        {
            MaxAge = PictureMaxAge,
        };

        return AzureADLogic.GetUserPhoto(new Guid(oId), size).ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
                return (ActionResult)new NotFoundResult();

            var photo = task.Result;
            return new FileStreamResult(photo, "image/jpeg");
        });
    }


}

public class LoginWithAzureADRequest
{
    public string idToken;
    public string accessToken; 
}
