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
using Signum.React.Facades;

namespace Signum.React.Auth
{
    public class AuthController : ApiController
    {
        public static bool MergeInvalidUsernameAndPasswordMessages = false;

        public static event Action<ApiController, UserEntity> UserPreLogin;
        public static event Action<UserEntity> UserLogged;
        public static event Action UserLoggingOut;


        public static void Start()
        {
            ReflectionServer.GetContext = () => new
            {
                Culture = ReflectionServer.GetCurrentValidCulture(),
                Role = RoleEntity.Current,
            };
        }


        [Route("api/auth/login"), HttpPost]
        public LoginResponse Login([FromBody]LoginRequest data)
        {
            if (string.IsNullOrEmpty(data.userName))
                throw LoginError("userName", AuthMessage.UserNameMustHaveAValue.NiceToString());

            if (string.IsNullOrEmpty(data.password))
                throw LoginError("password", AuthMessage.PasswordMustHaveAValue.NiceToString());

            // Attempt to login
            UserEntity user = null;
            try
            {
                user = AuthLogic.Login(data.userName, Security.EncodePassword(data.password));
            }
            catch (Exception e) when (e is IncorrectUsernameException || e is IncorrectPasswordException)
            {
                if (MergeInvalidUsernameAndPasswordMessages)
                {
                    ModelState.AddModelError("userName", AuthMessage.InvalidUsernameOrPassword.NiceToString());
                    ModelState.AddModelError("password", AuthMessage.InvalidUsernameOrPassword.NiceToString());
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, this.ModelState));
                }
                else if (e is IncorrectUsernameException)
                {
                    throw LoginError("userName", AuthMessage.InvalidUsername.NiceToString());
                }
                else if(e is IncorrectPasswordException)
                {
                    throw LoginError("password", AuthMessage.InvalidPassword.NiceToString());
                }
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

        [Route("api/auth/logout"), HttpPost]
        public void Logout()
        {
            var httpContext = System.Web.HttpContext.Current;

            if (UserLoggingOut != null)
                UserLoggingOut();

            UserTicketClient.RemoveCookie();

            httpContext.Session.Abandon();
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