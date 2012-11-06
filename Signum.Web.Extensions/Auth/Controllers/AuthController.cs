#region usings
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
using Signum.Web.Extensions.Properties;
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
#endregion

namespace Signum.Web.Auth
{
    [AuthenticationRequired(false)]
    public class AuthController : Controller
    {
        public static event Func<string> GenerateRandomPassword = () => MyRandom.Current.NextString(8);

        public static event Action OnUserLogged;
        public static event Func<Controller, UserDN, ActionResult> OnUserPreLogin;
        public static event Func<Controller, string> OnUserLoggedDefaultRedirect = c =>
        {
            string referrer = c.ControllerContext.HttpContext.Request["referrer"];

            if (referrer.HasText())
                return referrer;

            return RouteHelper.New().Action("Index", "Home");
        };

        public static event Action OnUserLoggingOut;
        public static event Func<Controller, string> OnUserLogoutRedirect = c =>
        {
            return RouteHelper.New().Action("Index", "Home");
        };

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveNewUser(string prefix)
        {
            var context = this.ExtractEntity<UserDN>(prefix).ApplyChanges(this.ControllerContext, prefix, UserMapping.NewUser).ValidateGlobal();

            if (context.GlobalErrors.Any())
            { 
                this.ModelState.FromContext(context);
                return JsonAction.ModelState(ModelState);
            }

            context.Value.Execute(UserOperation.SaveNew);
            return JsonAction.Redirect(Navigator.NavigateRoute(context.Value));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveUserWithNewPwd(string prefix)
        {
            var context = this.ExtractEntity<UserDN>(prefix).ApplyChanges(this.ControllerContext, prefix, true);
            ViewData["NewPwd"] = true;
            return Navigator.NormalControl(this, context.Value);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveUser(string prefix)
        {
            var context = this.ExtractEntity<UserDN>(prefix).ApplyChanges(this.ControllerContext, prefix, true).ValidateGlobal();

            if (context.GlobalErrors.Any())
            { 
                this.ModelState.FromContext(context);
                return JsonAction.ModelState(ModelState);
            }

            context.Value.Execute(UserOperation.Save);
            return JsonAction.Redirect(Navigator.NavigateRoute(context.Value));
        }

    



        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SetPassword(string prefix, string oldPrefix)
        {
            UserDN entity = this.ExtractEntity<UserDN>(oldPrefix);
            var model = new SetPasswordModel { User = entity.ToLite() };

            ViewData[ViewDataKeys.WriteSFInfo] = true; 
            ViewData[ViewDataKeys.OnSave] = new JsOperationExecutor(new JsOperationOptions
                {
                    ControllerUrl = RouteHelper.New().Action("SetPasswordOnOk", "Auth"),
                    Prefix = prefix,
                }).validateAndAjax().ToJS();

            ViewData[ViewDataKeys.Title] = Resources.EnterTheNewPassword;

            TypeContext tc = TypeContextUtilities.UntypedNew(model, prefix);
            return this.PopupOpen(new PopupNavigateOptions(tc));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult SetPasswordOnOk(string prefix)
        {
            var context = this.ExtractEntity<SetPasswordModel>(prefix).ApplyChanges(this.ControllerContext, prefix, true).ValidateGlobal();

            UserDN g = context.Value.User.ExecuteLite(UserOperation.SetPassword, context.Value.Password);

            return JsonAction.Redirect(Navigator.NavigateRoute(typeof(UserDN), g.Id));
        }
                
        #region "Change password"
        public ActionResult ChangePassword()
        {

            if (TempData.ContainsKey("message") && TempData["message"] != null)
                ViewData["message"] = TempData["message"].ToString();

            ViewData["Title"] = Resources.ChangePassword;
            return View(AuthClient.ChangePasswordView);
        }

        [HttpPost]
        public ActionResult ChangePassword(FormCollection form)
        {
            UserDN user = null;

            if (UserDN.Current == null)
            {
                var username = (string)TempData["username"];
                if (!username.HasText())
                    username = (string)form["username"];


                using (AuthLogic.Disable())
                    user = AuthLogic.RetrieveUser(username);

                var context = user.ApplyChanges(this.ControllerContext, "", UserMapping.ChangePasswordOld).ValidateGlobal();

                if (context.GlobalErrors.Any())
                {
                    ViewData["username"] = username;
                    ViewData["Title"] = Resources.ChangePassword;
                    ModelState.FromContext(context);
                    return View(AuthClient.ChangePasswordView);
                }

                string errorPasswordValidation = UserDN.OnValidatePassword(Request.Params[UserMapping.NewPasswordKey]);
                if (errorPasswordValidation.HasText())
                {
                    ViewData["username"] = username;
                    ViewData["Title"] = Resources.ChangePassword;
                    ModelState.AddModelError("password", errorPasswordValidation);
                    return View(AuthClient.ChangePasswordView);
                }

            }

            else
            {
                var context = UserDN.Current.ApplyChanges(this.ControllerContext, "", UserMapping.ChangePasswordOld).ValidateGlobal();
                if (context.GlobalErrors.Any())
                {
                    ViewData["Title"] = Resources.ChangePassword;
                    ModelState.FromContext(context);
                    RefreshSessionUserChanges();
                    return View(AuthClient.ChangePasswordView);
                }

                string errorPasswordValidation = UserDN.OnValidatePassword(Request.Params[UserMapping.NewPasswordKey]);
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
            Login(user.UserName, form[UserMapping.NewPasswordKey], false, null);

            return RedirectToAction("ChangePasswordSuccess");


        }

        private void RefreshSessionUserChanges()
        {
            UserDN.Current = UserDN.Current.ToLite().Retrieve(); 
        }

        public ActionResult ChangePasswordSuccess()
        {
            ViewData["Message"] = Resources.PasswordHasBeenChangedSuccessfully;
            ViewData["Title"] = Resources.ChangePassword;

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
                ModelState.AddModelError("email", Resources.EmailMustHaveAValue);
                return View(AuthClient.ResetPasswordView);
            }

            using (AuthLogic.Disable())
            {

                var user = ResetPasswordRequestLogic.GetUserByEmail(email);
                //since this is an url sent by email, it should contain the domain name
                ResetPasswordRequestLogic.ResetPasswordRequestAndSendEmail(user, rpr =>
                    Request.Url.Scheme + System.Uri.SchemeDelimiter + Request.Url.Host + (Request.Url.Port != 80 ? (":" + Request.Url.Port) : "") + RouteHelper.New().Action("ResetPasswordCode", "Auth", new { email = rpr.User.Email, code = rpr.Code }));
            }

            ViewData["email"] = email;
            return RedirectToAction("ResetPasswordSend");
        }

      
      

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ResetPasswordSend()
        {
            ViewData["Message"] = Resources.ResetPasswordCodeHasBeenSent.Formato(ViewData["email"]);

            return View(AuthClient.ResetPasswordSendView);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ResetPasswordCode(string email, string code)
        {
            ResetPasswordRequestDN rpr = null;
            using (AuthLogic.Disable())
            {
                rpr = Database.Query<ResetPasswordRequestDN>()
                    .Where(r => r.User.Email == email && r.Code == code)
                    .SingleOrDefaultEx(()=>Resources.TheConfirmationCodeThatYouHaveJustSentIsInvalid);
            }
            TempData["ResetPasswordRequest"] = rpr;

            return RedirectToAction("ResetPasswordSetNew");
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ResetPasswordSetNew()
        {
            ResetPasswordRequestDN rpr = (ResetPasswordRequestDN)TempData["ResetPasswordRequest"];
            if (rpr == null)
            {
                TempData["Error"] = Resources.ThereHasBeenAnErrorWithYourRequestToResetYourPasswordPleaseEnterYourLogin;
                return RedirectToAction("ResetPassword");
            }
            ViewData["rpr"] = rpr.Id;
            return View(AuthClient.ResetPasswordSetNewView);
        }

        [HttpPost]
        public ActionResult ResetPasswordSetNew(Lite<ResetPasswordRequestDN> rpr)
        {
            ResetPasswordRequestDN request = null;
            UserDN user = null;
            using (AuthLogic.Disable())
            {
                request = rpr.Retrieve();
                user = Database.Query<UserDN>()
                    .Where(u => u.Email == request.User.Email)
                    .SingleEx();
            }

            var context = user.ApplyChanges(this.ControllerContext, "", UserMapping.ChangePassword).ValidateGlobal();

            if (!context.Errors.TryGetC(UserMapping.NewPasswordKey).IsNullOrEmpty() ||
                !context.Errors.TryGetC(UserMapping.NewPasswordBisKey).IsNullOrEmpty())
            {
                ViewData["Title"] = Resources.ChangePassword;
                ModelState.FromContext(context);
                return ResetPasswordSetNewError(request.Id, "");
            }

            string errorPasswordValidation = UserDN.OnValidatePassword(Request.Params[UserMapping.NewPasswordKey]);
            if (errorPasswordValidation.HasText())
                return ResetPasswordSetNewError(request.Id, errorPasswordValidation);

            using (AuthLogic.Disable())
            {
                using (OperationLogic.AllowSave<UserDN>())
                {
                    context.Value.Save();
                }
                //remove pending requests
                Database.Query<ResetPasswordRequestDN>().Where(r => r.User.Email == user.Email && r.Code == request.Code).UnsafeDelete();
            }

            return RedirectToAction("ResetPasswordSuccess");
        }

        ViewResult ResetPasswordSetNewError(int idResetPasswordRequest, string error)
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
                return LoginErrorAjaxOrForm("username", Resources.UserNameMustHaveAValue);

            if (string.IsNullOrEmpty(password))
                return LoginErrorAjaxOrForm("password", Resources.PasswordMustHaveAValue);

            // Attempt to login
            UserDN user = null;
            try
            {
                user = AuthLogic.Login(username, Security.EncodePassword(password));
            }
            catch (PasswordExpiredException)
            {
                TempData["message"] = Resources.ExpiredPasswordMessage;
                TempData["username"] = username;
                return RedirectToAction("ChangePassword");
            }
            catch (IncorrectUsernameException)
            {
                return LoginErrorAjaxOrForm(Request.IsAjaxRequest() ? "username" : "_FORM", Resources.InvalidUsernameOrPassword);
            }
            catch (IncorrectPasswordException)
            {
                return LoginErrorAjaxOrForm(Request.IsAjaxRequest() ? "password" : "_FORM", Resources.InvalidUsernameOrPassword);
            }

            if (user == null)
                throw new Exception(Resources.ExpectedUserLogged);


            if (OnUserPreLogin != null)
            {
                var result = OnUserPreLogin(this, user);
                if (result != null)
                    return result;
            }

            UserDN.Current = user;

            //guardamos una cookie persistente si se ha seleccionado
            if (rememberMe.HasValue && rememberMe.Value)
            {
                string ticketText = UserTicketLogic.NewTicket(
                       System.Web.HttpContext.Current.Request.UserHostAddress);

                HttpCookie cookie = new HttpCookie(AuthClient.CookieName, ticketText)
                {
                    Expires = DateTime.UtcNow.Add(UserTicketLogic.ExpirationInterval),
                };

                System.Web.HttpContext.Current.Response.Cookies.Add(cookie);
            }

            AddUserSession(user);

            TempData["Message"] = AuthLogic.OnLoginMessage();


            return LoginRedirectAjaxOrForm(OnUserLoggedDefaultRedirect(this));

        }

        private ActionResult LoginErrorAjaxOrForm(string key, string message)
        {
            if (Request.IsAjaxRequest())
            {
                ModelState.Clear();
                ModelState.AddModelError(key, message, "");
                return JsonAction.ModelState(ModelState);
            }
            else
                return LoginError(key, message);
        }

        public ActionResult LoginRedirectAjaxOrForm(string url)
        {
            if (Request.IsAjaxRequest())
                return JsonAction.Redirect(url);
            else
                return Redirect(url);
        }

        ViewResult LoginError(string key, string error)
        {
            ModelState.AddModelError(key, error);
            return View(AuthClient.LoginView);
        }

        public static bool LoginFromCookie()
        {
            using (AuthLogic.Disable())
            {
                try
                {
                    var authCookie = System.Web.HttpContext.Current.Request.Cookies[AuthClient.CookieName];
                    if (authCookie == null || !authCookie.Value.HasText())
                        return false;   //there is no cookie

                    string ticketText = authCookie.Value;

                    UserDN user = UserTicketLogic.UpdateTicket(
                           System.Web.HttpContext.Current.Request.UserHostAddress,
                           ref ticketText);

                    if (OnUserPreLogin != null)
                    {
                        var result = OnUserPreLogin(null, user);
                        if (result != null)
                        {
                            //We can not execute :S
                        }
                    }

                    System.Web.HttpContext.Current.Response.Cookies.Add(new HttpCookie(AuthClient.CookieName, ticketText)
                    {
                        Expires = DateTime.UtcNow.Add(UserTicketLogic.ExpirationInterval),
                    });

                    AddUserSession(user);
                    return true;
                }
                catch
                {
                    //Remove cookie
                    HttpCookie cookie = new HttpCookie(AuthClient.CookieName)
                    {
                        Expires = DateTime.UtcNow.AddDays(-10) // or any other time in the past
                    };
                    System.Web.HttpContext.Current.Response.Cookies.Set(cookie);

                    return false;
                }
            }
        }
        #endregion



        public static Action<UserDN> OnUpdatedSessionUser;

        public static void UpdateSessionUser()
        {
            var newUser = UserDN.Current.ToLite().Retrieve();

            UserDN.Current = newUser; 

            if (OnUpdatedSessionUser != null)
                OnUpdatedSessionUser(newUser); 
        }

        public static void AddUserSession(UserDN user)
        {
            UserDN.Current = user;

            if (OnUserLogged != null)
                OnUserLogged();
        }

        public ActionResult Logout()
        {
            LogoutDo();

            return LoginRedirectAjaxOrForm(OnUserLogoutRedirect(this));
        }

        public static void LogoutDo()
        {
            var httpContext = System.Web.HttpContext.Current;

            if (OnUserLoggingOut != null)
                OnUserLoggingOut();

            FormsAuthentication.SignOut();

            var authCookie = httpContext.Request.Cookies[AuthClient.CookieName];
            if (authCookie != null && authCookie.Value.HasText())
                httpContext.Response.Cookies[AuthClient.CookieName].Expires = DateTime.UtcNow.AddDays(-10);

            httpContext.Session.Abandon();
        }
    }
}
