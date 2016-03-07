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
using Signum.React.Authorization;

namespace Signum.React.Auth
{
    public class AuthController : ApiController
    {
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
                if (AuthServer.MergeInvalidUsernameAndPasswordMessages)
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
                throw LoginError("password", AuthServer.MergeInvalidUsernameAndPasswordMessages ?
                    AuthMessage.InvalidUsernameOrPassword.NiceToString() :
                    AuthMessage.InvalidPassword.NiceToString());
            }

            UserEntity.Current = user;

            if (data.rememberMe == true)
            {
                UserTicketServer.SaveCookie();
            }

            AuthServer.AddUserSession(user);

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
            
            AuthServer.UserLoggingOut?.Invoke();

            UserTicketServer.RemoveCookie();

            httpContext.Session.Abandon();
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


    }
}