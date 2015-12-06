using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;

namespace Signum.React.Auth
{
    public class AuthController : ApiController
    {
        public static bool MergeInvalidUsernameAndPasswordMessages = true;

        public static event Action<ApiController, UserEntity> UserPreLogin;
        public static event Action<UserEntity> UserLogged;

        [Route("api/auth/login"), HttpPost]
        public LoginResponse Login([FromBody]LoginRequest data)
        {
            if (string.IsNullOrEmpty(data.userName))
                throw LoginError("Username", AuthMessage.UserNameMustHaveAValue.NiceToString());

            if (string.IsNullOrEmpty(data.password))
                throw LoginError("Password", AuthMessage.PasswordMustHaveAValue.NiceToString());

            // Attempt to login
            UserEntity user = null;
            try
            {
                user = AuthLogic.Login(data.userName, Security.EncodePassword(data.password));
            }
            catch (IncorrectUsernameException)
            {
                ModelState.AddModelError("Username", MergeInvalidUsernameAndPasswordMessages ?
                    AuthMessage.InvalidUsernameOrPassword.NiceToString() :
                    AuthMessage.InvalidUsername.NiceToString());

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, this.ModelState));
            }
            catch (IncorrectPasswordException)
            {
                throw LoginError("password", MergeInvalidUsernameAndPasswordMessages ?
                    AuthMessage.InvalidUsernameOrPassword.NiceToString() :
                    AuthMessage.InvalidPassword.NiceToString());
            }

            UserEntity.Current = user;

            if (data.rememberMe == true)
            {
                UserTicketClient.SaveCookie();
            }

            AddUserSession(user);

            string message = AuthLogic.OnLoginMessage();

            return new LoginResponse { message = message, userEntity = user };
        }

        [Route("api/auth/currentUser")]
        public UserEntity GetCurrentUser()
        {
            return UserEntity.Current;
        }

        internal static void OnUserPreLogin(ApiController controller, UserEntity user)
        {
            if (UserPreLogin != null)
            {
                UserPreLogin(controller, user);
            }
        }

        private HttpResponseException LoginError(string field, string error)
        {
            ModelState.AddModelError(field, error);
            return new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, this.ModelState));
        }

        public class LoginRequest
        {
            public string userName { get; set; }
            public string password { get; set; }
            public bool? rememberMe { get; set; }
        }

        public class LoginResponse
        {
            public string message { get; set; }
            public UserEntity userEntity { get; set; }
        }

        public static void AddUserSession(UserEntity user)
        {
            UserEntity.Current = user;

            if (UserLogged != null)
                UserLogged(user);
        }
    }
}