using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Engine.Mailing;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.React.Filters;
using Signum.Services;
using Signum.Utilities;
using Signum.Engine.Basics;

namespace Signum.React.Authorization
{
    [ValidateModelFilter]
    public class AuthController : ControllerBase
    {
        [HttpPost("api/auth/login"), SignumAllowAnonymous]
        public ActionResult<LoginResponse> Login([Required, FromBody] LoginRequest data)
        {
            if (string.IsNullOrEmpty(data.userName))
                return ModelError("userName", AuthMessage.UserNameMustHaveAValue.NiceToString());

            if (string.IsNullOrEmpty(data.password))
                return ModelError("password", AuthMessage.PasswordMustHaveAValue.NiceToString());

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
                    return ModelError("login", AuthMessage.InvalidUsernameOrPassword.NiceToString());
                }
                else if (e is IncorrectUsernameException)
                {
                    return ModelError("userName", AuthMessage.InvalidUsername.NiceToString());
                }
                else if (e is IncorrectPasswordException)
                {
                    return ModelError("password", AuthMessage.InvalidPassword.NiceToString());
                }
                throw;
            }
            catch (Exception e)
            {
                return ModelError("login", e.Message);
            }

            using (UserHolder.UserSession(user))
            {
                if (data.rememberMe == true)
                {
                    UserTicketServer.SaveCookie(ControllerContext);
                }

                AuthServer.OnUserPreLogin(ControllerContext, user);

                AuthServer.AddUserSession(ControllerContext, user);

                string? message = AuthLogic.OnLoginMessage();

                var token = AuthTokenServer.CreateToken(user);

                return new LoginResponse { message = message, userEntity = user, token = token, authenticationType = authenticationType };
            }
        }

        [HttpGet("api/auth/loginFromApiKey")]
        public LoginResponse LoginFromApiKey(string apiKey)
        {
            string? message = AuthLogic.OnLoginMessage();

            var token = AuthTokenServer.CreateToken(UserEntity.Current);

            return new LoginResponse { message = message, userEntity = UserEntity.Current, token = token, authenticationType = "api-key" };
        }

        [HttpPost("api/auth/loginFromCookie"), SignumAllowAnonymous]
        public LoginResponse? LoginFromCookie()
        {
            using (ScopeSessionFactory.OverrideSession())
            {
                if (!UserTicketServer.LoginFromCookie(ControllerContext))
                    return null;

                string? message = AuthLogic.OnLoginMessage();

                var token = AuthTokenServer.CreateToken(UserEntity.Current);

                return new LoginResponse { message = message, userEntity = UserEntity.Current, token = token, authenticationType = "cookie" };
            }
        }

        [HttpPost("api/auth/loginWindowsAuthentication"), Authorize, SignumAllowAnonymous]
        public LoginResponse? LoginWindowsAuthentication(bool throwError)
        {
            using (ScopeSessionFactory.OverrideSession())
            {
                string? error = WindowsAuthenticationServer.LoginWindowsAuthentication(ControllerContext);
                if(error != null)
                {
                    if (throwError)
                        throw new InvalidOperationException(error);

                    return null;
                }

                var token = AuthTokenServer.CreateToken(UserEntity.Current);

                return new LoginResponse { message = null, userEntity = UserEntity.Current, token = token, authenticationType = "windows" };
            }
        }

        [HttpPost("api/auth/loginWithAzureAD"), SignumAllowAnonymous]
        public LoginResponse? LoginWithAzureAD([FromBody, Required]string jwt)
        {
            using (ScopeSessionFactory.OverrideSession())
            {   
                if (!AzureADAuthenticationServer.LoginAzureADAuthentication(ControllerContext, jwt))
                    return null;

                var token = AuthTokenServer.CreateToken(UserEntity.Current);

                return new LoginResponse { message = null, userEntity = UserEntity.Current, token = token, authenticationType = "azureAD" };
            }
        }

        [HttpGet("api/auth/currentUser")]
        public UserEntity? GetCurrentUser()
        {
            var result = UserEntity.Current;
            return result.Is(AuthLogic.AnonymousUser) ? null : result;
        }

        [HttpPost("api/auth/logout")]
        public void Logout()
        {
            AuthServer.UserLoggingOut?.Invoke(UserEntity.Current);

            UserTicketServer.RemoveCookie(ControllerContext);
        }

        [HttpPost("api/auth/ChangePassword")]
        public ActionResult<LoginResponse> ChangePassword([Required, FromBody] ChangePasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.oldPassword))
                return ModelError("oldPassword", AuthMessage.PasswordMustHaveAValue.NiceToString());

            if (string.IsNullOrEmpty(request.newPassword))
                return ModelError("newPassword", AuthMessage.PasswordMustHaveAValue.NiceToString());

            var error = UserEntity.OnValidatePassword(request.newPassword);
            if (error.HasText())
                return ModelError("newPassword", error);

            var user = UserEntity.Current;

            if (!user.PasswordHash.SequenceEqual(Security.EncodePassword(request.oldPassword)))
                return ModelError("oldPassword", AuthMessage.InvalidPassword.NiceToString());

            user.PasswordHash = Security.EncodePassword(request.newPassword);
            using (AuthLogic.Disable())
            using (OperationLogic.AllowSave<UserEntity>())
            {
                user.Save();
            }

            return new LoginResponse { userEntity = user, token = AuthTokenServer.CreateToken(UserEntity.Current), authenticationType = "changePassword" };
        }


        [HttpPost("api/auth/forgotPasswordEmail"), SignumAllowAnonymous]
        public string? ForgotPasswordEmail([Required, FromBody]ForgotPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.eMail))
                return AuthMessage.PasswordMustHaveAValue.NiceToString();

            try
            {
                var rpr = ResetPasswordRequestLogic.SendResetPasswordRequestEmail(request.eMail);
            }
            catch (Exception ex)
            {
                ex.LogException();
                return AuthMessage.AnErrorOccurredRequestNotProcessed.NiceToString();
            }

            return null;
        }

        [HttpPost("api/auth/resetPassword"), SignumAllowAnonymous]
        public ActionResult<LoginResponse> ResetPassword([Required, FromBody]ResetPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.newPassword))
                return ModelError("newPassword", AuthMessage.PasswordMustHaveAValue.NiceToString());

            var rpr = ResetPasswordRequestLogic.ResetPasswordRequestExecute(request.code, request.newPassword);

            return new LoginResponse { userEntity = rpr.User, token = AuthTokenServer.CreateToken(rpr.User), authenticationType = "resetPassword" };
        }

        [HttpGet("api/auth/ResetPasswordMail/{username}"), SignumAllowAnonymous]
        public ActionResult ResetPasswordMail(string username)
        {
            using (UserHolder.UserSession(AuthLogic.SystemUser!))
            {
                var user = Database.Query<UserEntity>()
                    .SingleOrDefault(u => u.UserName.ToLower() == username.ToLower());

                if (user == null)
                    return Ok();

                var config = EmailLogic.Configuration;
                var request = ResetPasswordRequestLogic.ResetPasswordRequest(user);
                var url = $"{config.UrlLeft}/auth/resetPassword?code={request.Code}";

                var mail = new ResetPasswordRequestEmail(request, url);
                mail.SendMailAsync();

                return Ok();
            }
        }

        [HttpGet("api/auth/ResetPasswordRequest/{code}"), SignumAllowAnonymous]
        public ActionResult GetResetPasswordRequest(string code)
        {
            using (UserHolder.UserSession(AuthLogic.SystemUser!))
            {
                return Ok(Database.Query<ResetPasswordRequestEntity>()
                    .SingleOrDefault(e => e.Code == code));
            }
        }

        [HttpPost("api/auth/SetPassword"), SignumAllowAnonymous]
        public ActionResult SetPassword([Required] [FromBody] SetPasswordRequest request)
        {
            using (UserHolder.UserSession(AuthLogic.SystemUser!))
            {
                if (string.IsNullOrEmpty(request.password))
                    return ModelError("password", AuthMessage.PasswordMustHaveAValue.NiceToString());

                if (string.IsNullOrEmpty(request.confirmPassword))
                    return ModelError("confirmPassword", AuthMessage.PasswordMustHaveAValue.NiceToString());

                var error = UserEntity.OnValidatePassword(request.password);
                if (error != null)
                    return ModelError("password", error);

                var entity = Database.Query<ResetPasswordRequestEntity>()
                    .SingleOrDefault(e => e.Code == request.code);

                if (entity == null || entity.Lapsed)
                    return BadRequest();
                
                if (entity.User.State == UserState.Disabled)
                    entity.User.Execute(UserOperation.Enable);

                entity.User.PasswordHash = Security.EncodePassword(request.password);
                entity.User.Execute(UserOperation.Save);

                entity.Lapsed = true;
                entity.Save();

                var mail = new PasswordChangedEmail(entity);
                mail.SendMailAsync();

                return Ok();
            }
        }

        private BadRequestObjectResult ModelError(string field, string error)
        {
            ModelState.AddModelError(field, error);
            return new BadRequestObjectResult(ModelState);
        }

#pragma warning disable IDE1006 // Naming Styles

        public class SetPasswordRequest
        {
            public string password { get; set; }
            public string confirmPassword { get; set; }
            public string code { get; set; }
        }

        public class LoginRequest
        {
            public string userName { get; set; }
            public string password { get; set; }
            public bool? rememberMe { get; set; }
        }

        public class LoginResponse
        {
            public string authenticationType { get; set; }
            public string? message { get; set; }
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
}
