using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Threading;
using Signum.Entities.Authorization;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Services;
using Signum.Utilities;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using Signum.Entities;
using Signum.Engine.Mailing;
using System.Collections.Generic;
using Signum.Engine.Operations;
using Signum.Web.Operations;

namespace Signum.Web.Auth
{
    [AuthenticationRequired(false)]
    public class AuthController : Controller
    {
        public static bool MergeInvalidUsernameAndPasswordMessages = true;

        public static event Func<string> GenerateRandomPassword = () => MyRandom.Current.NextString(8);

        public static event Action UserLogged;
        public static event Action<Controller, UserEntity> UserPreLogin;
        public static Func<Controller, string> UserLoggedRedirect = c =>
        {
            string referrer = c.ControllerContext.HttpContext.Request["referrer"];

            if (referrer.HasText())
                return referrer;

            return RouteHelper.New().Action("Index", "Home");
        };

        public static event Action UserLoggingOut;
        public static Func<Controller, string> UserLogoutRedirect = c =>
        {
            return RouteHelper.New().Action("Index", "Home");
        };

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveNewUser()
        {
            var context = this.ExtractEntity<UserEntity>().ApplyChanges(this, UserMapping.NewUser).Validate();

            if (context.HasErrors())
                return context.ToJsonModelState();

            context.Value.Execute(UserOperation.SaveNew);
            return this.DefaultExecuteResult(context.Value);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SetPasswordModel()
        {
            ViewData[ViewDataKeys.Title] = AuthMessage.EnterTheNewPassword.NiceToString();

            var model = new SetPasswordModel { };
            return this.PopupView(model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SetPasswordOnOk()
        {
            var passPrefix = Request["passPrefix"];

            var context = this.ExtractEntity<SetPasswordModel>(passPrefix).ApplyChanges(this, passPrefix);

            UserEntity user = this.ExtractLite<UserEntity>()
                .ExecuteLite(UserOperation.SetPassword, context.Value.PasswordHash);

            return this.DefaultExecuteResult(user);
        }

        #region "Change password"
        public ActionResult ChangePassword()
        {
            if (TempData.ContainsKey("message") && TempData["message"] != null)
                ViewData["message"] = TempData["message"].ToString();

            ViewData["Title"] = AuthMessage.ChangePassword.NiceToString();
            return View(AuthClient.ChangePasswordView);
        }

        [HttpPost]
        public ActionResult ChangePassword(FormCollection form)
        {
            UserEntity user = null;
            using (AuthLogic.Disable())
            {
                ViewData["Title"] = AuthMessage.ChangePassword.NiceToString();

                if (UserEntity.Current == null)
                {
                    var username = (string)TempData["username"];
                    if (!username.HasText())
                        username = (string)form["username"];


                    using (AuthLogic.Disable())
                        user = AuthLogic.RetrieveUser(username);

                    var context = user.ApplyChanges(this, UserMapping.ChangePasswordOld, "").Validate();

                    if (context.HasErrors())
                    {
                        ViewData["username"] = username;
                        ModelState.FromContext(context);
                        return View(AuthClient.ChangePasswordView);
                    }

                    string errorPasswordValidation = UserEntity.OnValidatePassword(Request.Params[UserMapping.NewPasswordKey]);
                    if (errorPasswordValidation.HasText())
                    {
                        ViewData["username"] = username;
                        ModelState.AddModelError("password", errorPasswordValidation);
                        return View(AuthClient.ChangePasswordView);
                    }
                }
                else
                {
                    var context = UserEntity.Current.ApplyChanges(this, UserMapping.ChangePasswordOld, "").Validate();
                    if (context.HasErrors())
                    {
                        ModelState.FromContext(context);
                        RefreshSessionUserChanges();
                        return View(AuthClient.ChangePasswordView);
                    }

                    string errorPasswordValidation = UserEntity.OnValidatePassword(Request.Params[UserMapping.NewPasswordKey]);
                    if (errorPasswordValidation.HasText())
                    {
                        ModelState.AddModelError("password", errorPasswordValidation);
                        RefreshSessionUserChanges();
                        return View(AuthClient.ChangePasswordView);
                    }

                    user = context.Value;
                }


                AuthLogic.ChangePassword(user.ToLite(),
                    Security.EncodePassword(form[UserMapping.OldPasswordKey]),
                    Security.EncodePassword(form[UserMapping.NewPasswordKey]));
            }
            Login(user.UserName, form[UserMapping.NewPasswordKey], false, null);

            return RedirectToAction("ChangePasswordSuccess");


        }

        private void RefreshSessionUserChanges()
        {
            using (AuthLogic.Disable())
                UserEntity.Current = UserEntity.Current.ToLite().Retrieve();
        }

        public ActionResult ChangePasswordSuccess()
        {
            ViewData["Message"] = AuthMessage.PasswordHasBeenChangedSuccessfully.NiceToString();
            ViewData["Title"] = AuthMessage.ChangePassword.NiceToString();

            return View(AuthClient.ChangePasswordSuccessView);
        }

        #endregion

        #region "Reset"

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ResetPassword()
        {
            return View(AuthClient.ResetPasswordView);
        }

        [HttpPost]
        public ActionResult ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("email", AuthMessage.EmailMustHaveAValue.NiceToString());
                return View(AuthClient.ResetPasswordView);
            }

            using (AuthLogic.Disable())
            {
                UserEntity user = ResetPasswordRequestLogic.GetUserByEmail(email);

                if(user == null)
                {
                    ModelState.AddModelError("email", AuthMessage.ThereSNotARegisteredUserWithThatEmailAddress.NiceToString());
                    return View(AuthClient.ResetPasswordView);
                }

                ResetPasswordRequestEntity rpr = ResetPasswordRequestLogic.ResetPasswordRequest(user);
                string url = HttpContext.Request.Url.GetLeftPart(UriPartial.Authority) + Url.Action<AuthController>(ac => ac.ResetPasswordCode(email, rpr.Code));
                new ResetPasswordRequestMail { Entity = rpr, Url = url }.SendMailAsync();
            }

            TempData["email"] = email;
            return RedirectToAction("ResetPasswordSend");
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ResetPasswordSend()
        {
            return View(AuthClient.ResetPasswordSendView);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ResetPasswordCode(string email, string code)
        {
            using (AuthLogic.Disable())
            {
                TempData["ResetPasswordRequest"] = Database.Query<ResetPasswordRequestEntity>()
                  .Where(r => r.User.Email == email && r.Code == code && !r.Lapsed)
                  .SingleOrDefaultEx(() => AuthMessage.TheConfirmationCodeThatYouHaveJustSentIsInvalid.NiceToString());
            }

            return RedirectToAction("ResetPasswordSetNew");
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ResetPasswordSetNew()
        {
            ResetPasswordRequestEntity rpr = (ResetPasswordRequestEntity)TempData["ResetPasswordRequest"];
            if (rpr == null)
            {
                TempData["Error"] = AuthMessage.ThereHasBeenAnErrorWithYourRequestToResetYourPasswordPleaseEnterYourLogin.NiceToString();
                return RedirectToAction("ResetPassword");
            }
            ViewData["rpr"] = rpr.Id;
            return View(AuthClient.ResetPasswordSetNewView);
        }

        [HttpPost]
        public ActionResult ResetPasswordSetNew(Lite<ResetPasswordRequestEntity> rpr)
        {
            using (AuthLogic.Disable())
            {
                ResetPasswordRequestEntity request = rpr.Retrieve();

                var user = request.User;

                var context = user.ApplyChanges(this, UserMapping.ChangePassword, "").Validate();

                if (!context.Errors.TryGetC(UserMapping.NewPasswordKey).IsNullOrEmpty() ||
                    !context.Errors.TryGetC(UserMapping.NewPasswordBisKey).IsNullOrEmpty())
                {
                    ViewData["Title"] = AuthMessage.ChangePassword.NiceToString();
                    ModelState.FromContext(context);
                    return ResetPasswordSetNewError(request.Id, "");
                }

                string errorPasswordValidation = UserEntity.OnValidatePassword(Request.Params[UserMapping.NewPasswordKey]);
                if (errorPasswordValidation.HasText())
                    return ResetPasswordSetNewError(request.Id, errorPasswordValidation);


                using (OperationLogic.AllowSave<UserEntity>())
                {
                    context.Value.Save();
                }
                //remove pending requests
                Database.Query<ResetPasswordRequestEntity>().Where(r => r.User.Email == user.Email && r.Code == request.Code).UnsafeDelete();
            }

            return RedirectToAction("ResetPasswordSuccess");
        }

        ViewResult ResetPasswordSetNewError(PrimaryKey idResetPasswordRequest, string error)
        {
            ModelState.AddModelError("_FORM", error);
            ViewData["rpr"] = idResetPasswordRequest;
            return View(AuthClient.ResetPasswordSetNewView);
        }

        public ActionResult ResetPasswordSuccess()
        {
            return View(AuthClient.ResetPasswordSuccessView);
        }
        #endregion

        #region Login

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Login(string referrer)
        {
            //We store the url referrer so that we can go back when logged in
            //If passed by parameter, it would be appended in the URL and we do not need to append it in the ViewData
            if (referrer == null)
            {
                if (TempData.ContainsKey("referrer") && TempData["referrer"] != null)
                    ViewData["referrer"] = TempData["referrer"].ToString();
            }

            return View(AuthClient.LoginView);
        }

        [HttpPost]
        public ActionResult Login(string username, string password, bool? rememberMe, string referrer)
        {
            // Basic parameter validation
            if (!username.HasText())
                return LoginError("username", AuthMessage.UserNameMustHaveAValue.NiceToString());

            if (string.IsNullOrEmpty(password))
                return LoginError("password", AuthMessage.PasswordMustHaveAValue.NiceToString());

            // Attempt to login
            UserEntity user = null;
            try
            {
                user = AuthLogic.Login(username, Security.EncodePassword(password));
            }
            catch (PasswordExpiredException)
            {
                TempData["message"] = AuthMessage.ExpiredPasswordMessage.NiceToString();
                TempData["username"] = username;
                return RedirectToAction("ChangePassword");
            }
            catch (IncorrectUsernameException)
            {
                return LoginError("username", MergeInvalidUsernameAndPasswordMessages ?
                    AuthMessage.InvalidUsernameOrPassword.NiceToString() :
                    AuthMessage.InvalidUsername.NiceToString());
            }
            catch (IncorrectPasswordException)
            {
                return LoginError("password", MergeInvalidUsernameAndPasswordMessages ?
                    AuthMessage.InvalidUsernameOrPassword.NiceToString() :
                    AuthMessage.InvalidPassword.NiceToString());
            }

            if (user == null)
                throw new Exception(AuthMessage.ExpectedUserLogged.NiceToString());

            OnUserPreLogin(this, user);

            UserEntity.Current = user;

            if (rememberMe == true)
            {
                UserTicketClient.SaveCookie();
            }

            AddUserSession(user);

            TempData["Message"] = AuthLogic.OnLoginMessage();


            return this.RedirectHttpOrAjax(UserLoggedRedirect(this));

        }

        internal static void OnUserPreLogin(Controller controller, UserEntity user)
        {
            if (UserPreLogin != null)
            {
                UserPreLogin(controller, user);
            }
        }

        public ViewResult LoginError(string key, string error)
        {
            ModelState.AddModelError(key, error);
            return View(AuthClient.LoginView);
        }

      
        #endregion



        public static Action<UserEntity> OnUpdatedSessionUser;

        public static void UpdateSessionUser()
        {
            var newUser = UserEntity.Current.ToLite().Retrieve();

            UserEntity.Current = newUser;

            if (OnUpdatedSessionUser != null)
                OnUpdatedSessionUser(newUser);
        }

        public static void AddUserSession(UserEntity user)
        {
            UserEntity.Current = user;

            if (UserLogged != null)
                UserLogged();
        }

        public ActionResult Logout()
        {
            LogoutDo();

            return this.RedirectHttpOrAjax(UserLogoutRedirect(this));
        }

        public static void LogoutDo()
        {
            var httpContext = System.Web.HttpContext.Current;

            if (UserLoggingOut != null)
                UserLoggingOut();

            FormsAuthentication.SignOut();

            UserTicketClient.RemoveCookie();
            
            httpContext.Session.Abandon();
        }
    }
}
