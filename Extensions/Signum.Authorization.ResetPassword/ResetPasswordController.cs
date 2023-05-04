using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;
using Signum.Authorization.AuthToken;

namespace Signum.Authorization.ResetPassword;

[ValidateModelFilter]
public class ResetPasswordController : ControllerBase
{
    [HttpPost("api/auth/forgotPasswordEmail"), SignumAllowAnonymous]
    public string? ForgotPasswordEmail([Required, FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.eMail))
            return LoginAuthMessage.EnterYourUserEmail.NiceToString();

        try
        {
            ResetPasswordRequestLogic.SendResetPasswordRequestEmail(request.eMail);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        return null;
    }

    [HttpPost("api/auth/resetPassword"), SignumAllowAnonymous]
    public ActionResult<LoginResponse> ResetPassword([Required, FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.newPassword))
            return ModelError("newPassword", LoginAuthMessage.PasswordMustHaveAValue.NiceToString());

        var error = UserEntity.OnValidatePassword(request.newPassword);
        if (error != null)
            return ModelError("newPassword", error);

        var rpr = ResetPasswordRequestLogic.ResetPasswordRequestExecute(request.code, request.newPassword);

        return new LoginResponse { userEntity = rpr.User, token = AuthTokenServer.CreateToken(rpr.User), authenticationType = "resetPassword" };
    }

    private BadRequestObjectResult ModelError(string field, string error)
    {
        ModelState.AddModelError(field, error);
        return new BadRequestObjectResult(ModelState);
    }
}
