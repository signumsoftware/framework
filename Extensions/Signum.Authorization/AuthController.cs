using Signum.Authorization.AuthToken;
using Signum.Authorization.UserTicket;
using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;
using System.ComponentModel.DataAnnotations;

namespace Signum.Authorization;

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
                user = AuthLogic.Login(data.userName, PasswordEncoding.EncodePasswordAlternatives(data.userName, data.password), out authenticationType);
            else
                user = AuthLogic.Authorizer.Login(data.userName, data.password, out authenticationType);
        }
        catch (Exception e) when (e is IncorrectUsernameException || e is IncorrectPasswordException)
        {
            if (AuthServer.AvoidExplicitErrorMessages)
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

        user.FillTypeConditions();

        if (data.rememberMe == true)
        {
            UserTicketServer.OnSaveCookie(ControllerContext);
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

    [HttpGet("api/auth/relogin")]
    public LoginResponse Relogin()
    {
        var user = UserEntity.Current.Retrieve();

        var token = AuthTokenServer.CreateToken(user);

        AuthLogic.OnUserLogingIn(user, nameof(Relogin));

        return new LoginResponse { userEntity = user, token = token, authenticationType = "relogin" };
    }

    [HttpPost("api/auth/loginFromCookie"), SignumAllowAnonymous]
    public LoginResponse? LoginFromCookie()
    {
        if (!UserTicketServer.LoginFromCookie(ControllerContext))
            return null;

        var user = UserEntity.Current.Retrieve();

        AuthLogic.OnUserLogingIn(user, nameof(LoginFromCookie));

        var token = AuthTokenServer.CreateToken(user);
        return new LoginResponse { userEntity = user, token = token, authenticationType = "cookie" };
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
        if (string.IsNullOrEmpty(request.oldPassword))
        {
            if (user.PasswordHash != null)
                return ModelError("oldPassword", LoginAuthMessage.PasswordMustHaveAValue.NiceToString());
        }
        else
        {
            if (user.PasswordHash == null || !PasswordEncoding.EncodePasswordAlternatives(user.UserName, request.oldPassword).Any(oldPasswordHash => oldPasswordHash.SequenceEqual(user.PasswordHash)))
                return ModelError("oldPassword", LoginAuthMessage.InvalidPassword.NiceToString());
        }

        user.PasswordHash = PasswordEncoding.EncodePassword(user.UserName, request.newPassword);
        using (AuthLogic.Disable())
        using (OperationLogic.AllowSave<UserEntity>())
        {
            user.Save();
        }

        return new LoginResponse { userEntity = user, token = AuthTokenServer.CreateToken(user), authenticationType = "changePassword" };
    }


    private BadRequestObjectResult ModelError(string field, string error)
    {
        ModelState.AddModelError(field, error);
        return new BadRequestObjectResult(ModelState);
    }
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
