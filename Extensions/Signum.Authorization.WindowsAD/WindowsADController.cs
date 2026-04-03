using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;
using Signum.Authorization.AuthToken;
using System.IO;

namespace Signum.Authorization.WindowsAD;

[ValidateModelFilter]
public class WindowsADController : ControllerBase
{
    [HttpPost("api/auth/loginWindowsAuthentication"), Authorize, SignumAllowAnonymous]
    public LoginResponse? LoginWindowsAuthentication(bool throwError)
    {
        if (!WindowsADServer.LoginWindowsAuthentication(ControllerContext, throwError))
            return null;

        var user = UserEntity.Current.Retrieve();

        var token = AuthTokenServer.CreateToken(user);

        return new LoginResponse { userEntity = user, token = token, authenticationType = "windows" };
    }

    public static TimeSpan PictureMaxAge = new TimeSpan(7, 0, 0);

    [HttpGet("api/adThumbnailphoto/{username}"), SignumAllowAnonymous]
    public ActionResult GetThumbnail(string username)
    {
        Response.GetTypedHeaders().CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
        {
            MaxAge = PictureMaxAge,
        };

        using (AuthLogic.Disable())
        {
            var byteArray = WindowsADLogic.GetProfilePicture(username);

            if (byteArray != null)
            {
                var memStream = new MemoryStream();

                memStream.Write(byteArray);
                memStream.Position = 0;

                var streamResult = new FileStreamResult(memStream, "image/jpeg");

                return streamResult;
            }

            return new NotFoundResult();
        }
    }
}
