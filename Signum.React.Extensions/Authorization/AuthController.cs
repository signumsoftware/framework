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
using Signum.Entities.Mailing;
using Signum.React.Filters;
using Signum.Services;
using Signum.Utilities;

namespace Signum.React.Authorization
{
    [ValidateModelFilter]
    public class AuthController : ControllerBase
    {
        [HttpPost("api/auth/login"), AllowAnonymous]
        public ActionResult<LoginResponse> Login([Required, FromBody] LoginRequest data)
        {
            if (string.IsNullOrEmpty(data.userName))
                return ModelError("userName", AuthMessage.UserNameMustHaveAValue.NiceToString());

            if (string.IsNullOrEmpty(data.password))
                return ModelError("password", AuthMessage.PasswordMustHaveAValue.NiceToString());

            // Attempt to login
            UserEntity user = null;
            try
            {
                if (AuthLogic.Authorizer == null)
                    user = AuthLogic.Login(data.userName, Security.EncodePassword(data.password));
                else
                    user = AuthLogic.Authorizer.Login(data.userName, data.password);
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

                string message = AuthLogic.OnLoginMessage();

                var token = AuthTokenServer.CreateToken(user);

                return new LoginResponse { message = message, userEntity = user, token = token };
            }
        }

        [HttpGet("api/auth/loginFromApiKey")]
        public LoginResponse LoginFromApiKey(string apiKey)
        {
            string message = AuthLogic.OnLoginMessage();

            var token = AuthTokenServer.CreateToken(UserEntity.Current);

            return new LoginResponse { message = message, userEntity = UserEntity.Current, token = token };
        }

        [HttpPost("api/auth/loginFromCookie"), AllowAnonymous]
        public LoginResponse LoginFromCookie()
        {
            using (ScopeSessionFactory.OverrideSession())
            {
                if (!UserTicketServer.LoginFromCookie(ControllerContext))
                    return null;

                string message = AuthLogic.OnLoginMessage();

                var token = AuthTokenServer.CreateToken(UserEntity.Current);

                return new LoginResponse { message = message, userEntity = UserEntity.Current, token = token };
            }
        }

        [HttpGet("api/auth/currentUser")]
        public UserEntity GetCurrentUser()
        {
            var result = UserEntity.Current;
            return result.Is(AuthLogic.AnonymousUser) ? null : result;
        }

        [HttpPost("api/auth/logout")]
        public void Logout()
        {
            AuthServer.UserLoggingOut?.Invoke();

            UserTicketServer.RemoveCookie(ControllerContext);
        }

        [HttpPost("api/auth/ChangePassword")]
        public ActionResult<LoginResponse> ChangePassword([Required, FromBody] ChangePasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.oldPassword))
                return ModelError("oldPassword", AuthMessage.PasswordMustHaveAValue.NiceToString());

            if (string.IsNullOrEmpty(request.newPassword))
                return ModelError("newPassword", AuthMessage.PasswordMustHaveAValue.NiceToString());

            var user = UserEntity.Current;
            var error = UserEntity.OnValidatePassword(request.newPassword);
            if (error != null)
                return ModelError("newPassword", error);

            if (!user.PasswordHash.SequenceEqual(Security.EncodePassword(request.oldPassword)))
                return ModelError("oldPassword", AuthMessage.InvalidPassword.NiceToString());

            user.PasswordHash = Security.EncodePassword(request.newPassword);
            using (AuthLogic.Disable())
                user.Execute(UserOperation.Save);

            return new LoginResponse { userEntity = user, token = AuthTokenServer.CreateToken(UserEntity.Current) };
        }

        [HttpGet("api/auth/ResetPasswordMail/{username}"), AllowAnonymous]
        public ActionResult ResetPasswordMail(string username)
        {
            using (UserHolder.UserSession(AuthLogic.SystemUser))
            {
                var user = Database.Query<UserEntity>()
                    .SingleOrDefault(u => u.UserName.ToLower() == username.ToLower());

                if (user == null)
                    return Ok();

                var request = ResetPasswordRequestLogic.ResetPasswordRequest(user);
                var url = $"{Request.Scheme}://{Request.Host}/auth/resetPassword?code={request.Code}";

                var mail = new ResetPasswordRequestMail(request, url);
                mail.SendMailAsync();

                return Ok();
            }
        }

        [HttpGet("api/auth/ResetPasswordRequest/{code}"), AllowAnonymous]
        public ActionResult GetResetPasswordRequest(string code)
        {
            using (UserHolder.UserSession(AuthLogic.SystemUser))
            {
                return Ok(Database.Query<ResetPasswordRequestEntity>()
                    .SingleOrDefault(e => e.Code == code));
            }
        }

        [HttpPost("api/auth/SetPassword"), AllowAnonymous]
        public ActionResult SetPassword([Required] [FromBody] SetPasswordRequest request)
        {
            using (UserHolder.UserSession(AuthLogic.SystemUser))
            {
                if (string.IsNullOrEmpty(request.Password))
                    return ModelError("password", AuthMessage.PasswordMustHaveAValue.NiceToString());

                if (string.IsNullOrEmpty(request.ConfirmPassword))
                    return ModelError("confirmPassword", AuthMessage.PasswordMustHaveAValue.NiceToString());

                var error = UserEntity.OnValidatePassword(request.Password);
                if (error != null)
                    return ModelError("password", error);

                var entity = Database.Query<ResetPasswordRequestEntity>()
                    .SingleOrDefault(e => e.Code == request.Code);

                if (entity == null || entity.Lapsed)
                    return BadRequest();

                entity.User.PasswordHash = Security.EncodePassword(request.Password);
                entity.User.State = UserState.Saved;
                entity.User.Execute(UserOperation.Save);

                entity.Lapsed = true;
                entity.Save();

                var mail = new PasswordChangedMail(entity);
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
        public class LoginRequest
        {
            public string userName { get; set; }
            public string password { get; set; }
            public bool? rememberMe { get; set; }
        }

        public class LoginResponse
        {
            public string message { get; set; }
            public string token { get; set; }
            public UserEntity userEntity { get; set; }
        }

        public class ChangePasswordRequest
        {
            public string oldPassword { get; set; }
            public string newPassword { get; set; }
        }

        public class SetPasswordRequest
        {
            public string Code { get; set; }
            public string Password { get; set; }
            public string ConfirmPassword { get; set; }
        }
#pragma warning restore IDE1006 // Naming Styles
    }
}
