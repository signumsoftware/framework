using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Signum.Engine.Authorization;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Operations;
using Signum.React.Filters;
using Signum.Services;

namespace Signum.React.Authorization;

[ValidateModelFilter]
public class AuthController : ControllerBase
{
    [HttpPost("api/auth/login"), SignumAllowAnonymous]
    public ActionResult<LoginResponse> Login([Required, FromBody] LoginRequest data)
    {
        if (string.IsNullOrEmpty(data.userName))
            return ModelError("userName", LoginAuthMessage.UserNameMustHaveAValue.NiceToString());

        if (string.IsNullOrEmpty(data.password))
            return ModelError("password", LoginAuthMessage.PasswordMustHaveAValue.NiceToString());

        string authenticationType;
        // Attempt to login
        UserEntity user;
        try
        {
            if (AuthLogic.Authorizer == null)
                user = AuthLogic.Login(data.userName, Security.EncodePassword(data.password), out authenticationType);
            else
                user = AuthLogic.Authorizer.Login(data.userName, data.password, out authenticationType);
        }
        catch (Exception e) when (e is IncorrectUsernameException || e is IncorrectPasswordException)
        {
            if (AuthServer.MergeInvalidUsernameAndPasswordMessages)
            {
                return ModelError("login", LoginAuthMessage.InvalidUsernameOrPassword.NiceToString());
            }
            else if (e is IncorrectUsernameException)
            {
                return ModelError("userName", LoginAuthMessage.InvalidUsername.NiceToString());
            }
            else if (e is IncorrectPasswordException)
            {
                return ModelError("password", LoginAuthMessage.InvalidPassword.NiceToString());
            }
            throw;
        }
        catch (Exception e)
        {
            return ModelError("login", e.Message);
        }

        AuthServer.OnUserPreLogin(ControllerContext, user);

        AuthServer.AddUserSession(ControllerContext, user);

        if (data.rememberMe == true)
        {
            UserTicketServer.SaveCookie(ControllerContext);
        }

        var token = AuthTokenServer.CreateToken(user);

        return new LoginResponse { userEntity = user, token = token, authenticationType = authenticationType };
    }

    [HttpGet("api/auth/loginFromApiKey")]
    public LoginResponse LoginFromApiKey(string apiKey)
    {
        var user = UserEntity.Current.Retrieve();

        var token = AuthTokenServer.CreateToken(user);

        return new LoginResponse { userEntity = user, token = token, authenticationType = "api-key" };
    }

    [HttpPost("api/auth/loginFromCookie"), SignumAllowAnonymous]
    public LoginResponse? LoginFromCookie()
    {
        if (!UserTicketServer.LoginFromCookie(ControllerContext))
            return null;

        var user = UserEntity.Current.Retrieve();

        var token = AuthTokenServer.CreateToken(user);
        return new LoginResponse { userEntity = user, token = token, authenticationType = "cookie" };
    }

    [HttpPost("api/auth/loginWindowsAuthentication"), Authorize, SignumAllowAnonymous]
    public LoginResponse? LoginWindowsAuthentication(bool throwError)
    {
        if (!WindowsAuthenticationServer.LoginWindowsAuthentication(ControllerContext, throwError))
            return null;

        var user = UserEntity.Current.Retrieve();

        var token = AuthTokenServer.CreateToken(user);

        return new LoginResponse { userEntity = user, token = token, authenticationType = "windows" };
    }

    [HttpPost("api/auth/loginWithAzureAD"), SignumAllowAnonymous]
    public LoginResponse? LoginWithAzureAD([FromBody, Required] string jwt, [FromQuery] bool throwErrors = true)
    {
        if (!AzureADAuthenticationServer.LoginAzureADAuthentication(ControllerContext, jwt, throwErrors))
            return null;

        var user = UserEntity.Current.Retrieve();

        var token = AuthTokenServer.CreateToken(user);

        return new LoginResponse { userEntity = user, token = token, authenticationType = "azureAD" };
    }

    [HttpGet("api/auth/currentUser")]
    public UserEntity? GetCurrentUser()
    {
        var result = UserEntity.Current;
        return result.Is(AuthLogic.AnonymousUser) ? null : result.Retrieve();
    }

    [HttpPost("api/auth/logout")]
    public void Logout()
    {
        AuthServer.UserLoggingOut?.Invoke(ControllerContext, UserHolder.Current);

        UserTicketServer.RemoveCookie(ControllerContext);
    }

    [HttpPost("api/auth/ChangePassword")]
    public ActionResult<LoginResponse> ChangePassword([Required, FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.newPassword))
            return ModelError("newPassword", LoginAuthMessage.PasswordMustHaveAValue.NiceToString());

        var error = UserEntity.OnValidatePassword(request.newPassword);
        if (error.HasText())
            return ModelError("newPassword", error);

        var user = UserEntity.Current.Retrieve();
        if(string.IsNullOrEmpty(request.oldPassword))
        {
            if(user.PasswordHash != null)
                return ModelError("oldPassword", LoginAuthMessage.PasswordMustHaveAValue.NiceToString());
        }
        else
        {
            if (user.PasswordHash == null || !user.PasswordHash.SequenceEqual(Security.EncodePassword(request.oldPassword)))
                return ModelError("oldPassword", LoginAuthMessage.InvalidPassword.NiceToString());
        }

        user.PasswordHash = Security.EncodePassword(request.newPassword);
        using (AuthLogic.Disable())
        using (OperationLogic.AllowSave<UserEntity>())
        {
            user.Save();
        }

        return new LoginResponse { userEntity = user, token = AuthTokenServer.CreateToken(user), authenticationType = "changePassword" };
    }


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

#pragma warning disable IDE1006 // Naming Styles
    public class LoginRequest
    {
        public string userName { get; set; }
        public string password { get; set; }
        public bool? rememberMe { get; set; }
    }

    public class LoginResponse
    {
        public string authenticationType { get; set; }
        public string token { get; set; }
        public UserEntity userEntity { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string oldPassword { get; set; }
        public string newPassword { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string code { get; set; }
        public string newPassword { get; set; }
    }

    public class ForgotPasswordRequest
    {
        public string eMail { get; set; }
    }
#pragma warning restore IDE1006 // Naming Styles
}
