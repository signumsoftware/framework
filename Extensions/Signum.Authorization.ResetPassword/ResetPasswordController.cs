using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;
using Signum.Authorization.AuthToken;

namespace Signum.Authorization.ResetPassword;

[ValidateModelFilter]
public class ResetPasswordController : ControllerBase
{
    [HttpPost("api/auth/forgotPasswordEmail"), SignumAllowAnonymous]
    public ForgotPasswordResponse ForgotPasswordEmail([Required, FromBody] ForgotPasswordRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.eMail))
                throw new ApplicationException(LoginAuthMessage.EnterYourUserEmail.NiceToString());

            ResetPasswordRequestLogic.SendResetPasswordRequestEmail(request.eMail);

            return new ForgotPasswordResponse()
            {
                success = true,
                title = LoginAuthMessage.RequestAccepted.NiceToString(),
                message = AuthServer.AvoidExplicitErrorMessages ? ResetPasswordMessage.IfEmailIsValidWeWillSendYouAnEmailToResetYourPassword.NiceToString()
                    : LoginAuthMessage.WeHaveSentYouAnEmailToResetYourPassword.NiceToString(),
            };
        }
        catch (Exception ex)
        {
            return new ForgotPasswordResponse()
            {
                success = false,
                message = ex.Message,
            };
        }
    }

    public class ForgotPasswordResponse
    { 
        public bool success { get; set; }
        public string message { get; set; }
        public string? title { get; set; }
    }

    [HttpPost("api/auth/resetPassword"), SignumAllowAnonymous]
    public ActionResult<LoginResponse> ResetPassword([Required, FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.newPassword))
            return ModelError("newPassword", LoginAuthMessage.PasswordMustHaveAValue.NiceToString());

        using (AuthLogic.Disable())
        {
            var rpr = Database.Query<ResetPasswordRequestEntity>()
                .Where(r => r.Code == request.code)
                .SingleOrDefaultEx();

            if (rpr == null)
                throw new ApplicationException(ResetPasswordMessage.TheCodeOfYourLinkIsIncorrect.NiceToString());

            var user = rpr.User;
            var error = UserEntity.OnValidatePassword(request.newPassword, user.Role);
            if (error != null)
                return ModelError("newPassword", error);
        }

        var rprResult = ResetPasswordRequestLogic.ResetPasswordRequestExecute(request.code, request.newPassword);

        return new LoginResponse { userEntity = rprResult.User, token = AuthTokenServer.CreateToken(rprResult.User), authenticationType = "resetPassword" };
    }

    [HttpPost("api/auth/requestNewLink"), SignumAllowAnonymous]
    public void RequestNewLink([Required, FromBody] string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ApplicationException(ResetPasswordMessage.TheCodeOfYourLinkIsIncorrect.NiceToString());

        ResetPasswordRequestLogic.RequestNewLink(code);
    }

    [HttpPost("api/auth/validatePasswordForReset"), SignumAllowAnonymous]
    public PasswordValidationResponse ValidatePasswordForReset([Required, FromBody] ValidatePasswordForResetRequest request)
    {
        using (AuthLogic.Disable())
        {
            var rpr = Database.Query<ResetPasswordRequestEntity>()
                .Where(r => r.Code == request.code)
                .SingleOrDefaultEx();

            if (rpr == null)
                throw new ApplicationException(ResetPasswordMessage.TheCodeOfYourLinkIsIncorrect.NiceToString());

            var user = rpr.User;
            var result = PasswordValidator.ValidatePassword(request.password, user.Role);
            
            return new PasswordValidationResponse
            {
                isValid = result.IsValid,
                errorMessage = result.ErrorMessage,
                complexityWarning = result.ComplexityWarning
            };
        }
    }

    private BadRequestObjectResult ModelError(string field, string error)
    {
        ModelState.AddModelError(field, error);
        return new BadRequestObjectResult(ModelState);
    }
}

public class ValidatePasswordForResetRequest
{
    public string code { get; set; }
    public string password { get; set; }
}
