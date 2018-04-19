using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Engine.Operations;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Services;
using Signum.Utilities;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Signum.React.ApiControllers;

namespace Signum.React.Authorization
{
    public class AuthController : ApiController
    {
        [Route("api/auth/login"), HttpPost, AllowAnonymous]
        public ActionResult<LoginResponse> Login([FromBody]LoginRequest data)
        {
            if (string.IsNullOrEmpty(data.userName))
                return ModelError("userName", AuthMessage.UserNameMustHaveAValue.NiceToString());

            if (string.IsNullOrEmpty(data.password))
                return ModelError("password", AuthMessage.PasswordMustHaveAValue.NiceToString());

            // Attempt to login
            UserEntity user = null;
            try
            {
                user = AuthLogic.Login(data.userName, Security.EncodePassword(data.password));
            }
            catch (Exception e) when (e is IncorrectUsernameException || e is IncorrectPasswordException)
            {
                if (AuthServer.MergeInvalidUsernameAndPasswordMessages)
                {
                    ActionContext.ModelState.AddModelError("userName", AuthMessage.InvalidUsernameOrPassword.NiceToString());
                    ActionContext.ModelState.AddModelError("password", AuthMessage.InvalidUsernameOrPassword.NiceToString());
                    return new BadRequestObjectResult(ActionContext.ModelState);
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
            catch (IncorrectPasswordException)
            {
                return ModelError("password", AuthServer.MergeInvalidUsernameAndPasswordMessages ?
                    AuthMessage.InvalidUsernameOrPassword.NiceToString() :
                    AuthMessage.InvalidPassword.NiceToString());
            }

            using (UserHolder.UserSession(user))
            {
                if (data.rememberMe == true)
                {
                    UserTicketServer.SaveCookie(this.ActionContext);
                }

                AuthServer.AddUserSession(user);

                string message = AuthLogic.OnLoginMessage();

                var token = AuthTokenServer.CreateToken(user);

                return new LoginResponse { message = message, userEntity = user, token = token };
            }
        }

        [Route("api/auth/loginFromApiKey"), HttpGet]
        public LoginResponse LoginFromApiKey(string apiKey)
        {
            string message = AuthLogic.OnLoginMessage();

            var token = AuthTokenServer.CreateToken(UserEntity.Current);

            return new LoginResponse { message = message, userEntity = UserEntity.Current, token = token };
        }

        [Route("api/auth/loginFromCookie"), HttpPost, AllowAnonymous]
        public LoginResponse LoginFromCookie()
        {
            using (ScopeSessionFactory.OverrideSession())
            {
                if (!UserTicketServer.LoginFromCookie(this.ActionContext))
                    return null;

                string message = AuthLogic.OnLoginMessage();

                var token = AuthTokenServer.CreateToken(UserEntity.Current);

                return new LoginResponse { message = message, userEntity = UserEntity.Current, token = token };
            }
        }

        [Route("api/auth/currentUser")]
        public UserEntity GetCurrentUser()
        {
            return UserEntity.Current;
        }

        [Route("api/auth/logout"), HttpPost]
        public void Logout()
        {
            AuthServer.UserLoggingOut?.Invoke();

            UserTicketServer.RemoveCookie(this.ActionContext);
        }

        [Route("api/auth/refreshToken"), HttpPost, AllowAnonymous]
        public LoginResponse RefreshToken([FromBody]string oldToken)
        {
            var newToken = AuthTokenServer.RefreshToken(oldToken, out UserEntity user);

            return new LoginResponse { message = null, userEntity = user, token = newToken };
        }

        [Route("api/auth/ChangePassword"), HttpPost]
        public ActionResult<LoginResponse> ChangePassword(ChangePasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.oldPassword))
                return ModelError("oldPassword", AuthMessage.PasswordMustHaveAValue.NiceToString());

            if (string.IsNullOrEmpty(request.newPassword))
                return ModelError("newPassword", AuthMessage.PasswordMustHaveAValue.NiceToString());

            var user = UserEntity.Current;

            if (!user.PasswordHash.SequenceEqual(Security.EncodePassword(request.oldPassword)))
                return ModelError("oldPassword", AuthMessage.InvalidPassword.NiceToString());

            user.PasswordHash = Security.EncodePassword(request.newPassword);
            using (AuthLogic.Disable())
                user.Execute(UserOperation.Save);

            return new LoginResponse { userEntity = user, token = AuthTokenServer.CreateToken(UserEntity.Current) };
        }

        private BadRequestObjectResult ModelError(string field, string error)
        {
            this.ActionContext.ModelState.AddModelError(field, error);
            return new BadRequestObjectResult(ActionContext.ModelState);
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
#pragma warning restore IDE1006 // Naming Styles
    }
}