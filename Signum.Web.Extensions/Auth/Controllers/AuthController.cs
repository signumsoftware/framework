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
#endregion

namespace Signum.Web.Authorization
{
    [HandleException]
    public class AuthController : Controller
    {
        public static event Func<string, string> ValidatePassword =
            p =>
            {
                if (Regex.Match(p, @"^[0-9a-zA-Z]{7,15}$").Success)
                    return null;
                return Resources.ThePasswordMustHaveBetween7And15CharactersEachOfThemBeingANumber09OrALetter;
            };

        public static event Func<string> GenerateRandomPassword = () => MyRandom.Current.NextString(8);

        public static event Action OnUserLogged;
        public static event Action<Controller, UserDN> OnUserPreLogin;
        public static event Func<Controller, string> OnUserLoggedDefaultReturn;
        public const string SessionUserKey = "user";

        #region "Change password"
        public ActionResult ChangePassword()
        {
            ViewData["Title"] = Resources.ChangePassword;
            return View(AuthClient.ChangePasswordUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ChangePassword(FormCollection form)
        {
            var context = UserDN.Current.ApplyChanges(this.ControllerContext, "", UserMapping.ChangePasswordOld).ValidateGlobal();

            if (context.GlobalErrors.Any())
            {
                ViewData["Title"] = Resources.ChangePassword;
                ModelState.FromContext(context);
                return View(AuthClient.ChangePasswordUrl);
            }

            string errorPasswordValidation = ValidatePassword(Request.Params[UserMapping.NewPasswordKey]);
            if (errorPasswordValidation.HasText())
            {
                ModelState.AddModelError("password", errorPasswordValidation);
                return View(AuthClient.ChangePasswordUrl);
                //return LoginError("password", errorPasswordValidation);
            }

            using (AuthLogic.Disable())
            {
                Database.Save(context.Value);
            }

            return RedirectToAction("ChangePasswordSuccess");
        }

        public ActionResult ChangePasswordSuccess()
        {
            ViewData["Message"] = Resources.PasswordHasBeenChangedSuccessfully;
            ViewData["Title"] = Resources.ChangePassword;

            return View(AuthClient.ChangePasswordSuccessUrl);
        }

        #endregion

        //[AcceptVerbs(HttpVerbs.Post)]
        //public ActionResult ValidateNewUser()
        //{
        //    var context = this.ExtractEntity<UserDN>().ApplyChanges(this.ControllerContext, "", true);
        //    context.Value.PasswordHash = Security.EncodePassword(GenerateRandomPassword());
        //    context.ValidateGlobal();

        //    this.ModelState.FromContext(context);
        //    return Navigator.ModelState(ModelState);
        //}

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveNewUser(string prefix)
        {
            var context = this.ExtractEntity<UserDN>(prefix).ApplyChanges(this.ControllerContext, prefix, UserMapping.NewUser).ValidateGlobal();

            if (context.GlobalErrors.Any())
            { 
                this.ModelState.FromContext(context);
                return Navigator.ModelState(ModelState);
            }

            context.Value.Execute(UserOperation.SaveNew);
            return Navigator.RedirectUrl(Navigator.ViewRoute(typeof(UserDN), context.Value.Id));
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
            var context = (Request.Form.AllKeys.Any(k => k.EndsWith(UserMapping.NewPasswordKey))) ?
                this.ExtractEntity<UserDN>(prefix).ApplyChanges(this.ControllerContext, prefix, UserMapping.NewUser).ValidateGlobal() :
                this.ExtractEntity<UserDN>(prefix).ApplyChanges(this.ControllerContext, prefix, true).ValidateGlobal();

            if (context.GlobalErrors.Any())
            { 
                this.ModelState.FromContext(context);
                return Navigator.ModelState(ModelState);
            }

            context.Value.Execute(UserOperation.SaveNew);
            return Navigator.RedirectUrl(Navigator.ViewRoute(typeof(UserDN), context.Value.Id));
        }
        #region "Reset"

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ResetPassword()
        {
            ViewData[ViewDataKeys.PageTitle] = Resources.ResetPassword;
            return View(AuthClient.ResetPasswordUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ResetPassword(string email)
        {
            ViewData[ViewDataKeys.PageTitle] = Resources.ResetPassword;
            try
            {
                if (string.IsNullOrEmpty(email))
                    return RememberPasswordError("email", Resources.EmailMustHaveAValue);

                using (AuthLogic.Disable())
                {
                    //Check the email belongs to a user
                    UserDN user = Database.Query<UserDN>().Where(u => u.Email == email).SingleOrDefault(Resources.EmailNotExistsDatabase);

                    if (user == null)
                        throw new ApplicationException(Resources.ThereSNotARegisteredUserWithThatEmailAddress);

                    AuthLogic.ResetPasswordRequest(user, HttpContextUtils.FullyQualifiedApplicationPath);
                }

                ViewData["email"] = email;
                return RedirectToAction("ResetPasswordSend");
            }
            catch (Exception ex)
            {
                return ResetPasswordError("_FORM", ex.Message);
            }
        }

        ViewResult ResetPasswordError(string key, string error)
        {
            ModelState.AddModelError("_FORM", error);
            return View(AuthClient.ResetPasswordUrl);
        }

        ViewResult ResetPasswordSetNewError(int idResetPasswordRequest, string key, string error)
        {
            ModelState.AddModelError("_FORM", error);
            ViewData["rpr"] = idResetPasswordRequest;
            return View(AuthClient.ResetPasswordSetNewUrl);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ResetPasswordSend()
        {
            ViewData["Message"] = Resources.ResetPasswordCodeHasBeenSent.Formato(ViewData["email"]);
            ViewData[ViewDataKeys.PageTitle] = Resources.ResetPassword;

            return View(AuthClient.ResetPasswordSendUrl);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ResetPasswordCode(string email, string code)
        {
            ResetPasswordRequestDN rpr = null;
            using (AuthLogic.Disable())
            {
                rpr = Database.Query<ResetPasswordRequestDN>()
                    .Where(r => r.User.Email == email && r.Code == code)
                    .SingleOrDefault(Resources.TheConfirmationCodeThatYouHaveJustSentIsInvalid);
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
            return View(AuthClient.ResetPasswordSetNewUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ResetPasswordSetNew(string code, Lite<ResetPasswordRequestDN> rpr)
        {
            ResetPasswordRequestDN request = null;
            try
            {
                UserDN user = null;
                using (AuthLogic.Disable())
                {
                    request = rpr.Retrieve();
                    user = Database.Query<UserDN>()
                        .Where(u => u.Email == request.User.Email)
                        .Single();
                }

                var context = user.ApplyChanges(this.ControllerContext, "", UserMapping.ChangePassword).ValidateGlobal();

                if (context.GlobalErrors.Any())
                {
                    ViewData["Title"] = Resources.ChangePassword;
                    ModelState.FromContext(context);
                    return ResetPasswordSetNewError(request.Id, "", "");
                }

                string errorPasswordValidation = ValidatePassword(Request.Params[UserMapping.NewPasswordKey]);
                if (errorPasswordValidation.HasText())
                    return ResetPasswordSetNewError(request.Id, "NewPassword", errorPasswordValidation);

                using (AuthLogic.Disable())
                {
                    Database.Save(context.Value);
                    //remove pending requests
                    Database.Query<ResetPasswordRequestDN>().Where(r => r.User.Email == user.Email && r.Code == code).UnsafeDelete();
                }

                return RedirectToAction("ResetPasswordSuccess");
            }
            catch (Exception ex)
            {
                return ResetPasswordSetNewError(request.Id, ViewDataKeys.GlobalErrors, ex.Message);
            }
        }

        public ActionResult ResetPasswordSuccess()
        {
            ViewData[ViewDataKeys.PageTitle] = Resources.ResetPasswordSuccess;
            return View(AuthClient.ResetPasswordSuccessUrl);
        }
        #endregion

        #region "Remember password"
        public ActionResult RememberPassword()
        {
            ViewData["Title"] = Resources.RememberPassword;
            return View(AuthClient.RememberPasswordUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult RememberPassword(string username, string email)
        {
            ViewData[ViewDataKeys.PageTitle] = Resources.RememberPassword;
            try
            {
                if (string.IsNullOrEmpty(username))
                    return RememberPasswordError("username", Resources.UserNameMustHaveAValue);

                if (string.IsNullOrEmpty(email))
                    return RememberPasswordError("email", Resources.EmailMustHaveAValue);

                UserDN user = AuthLogic.UserToRememberPassword(username, email);
                string randomPassword = GenerateRandomPassword();
                using (AuthLogic.Disable())
                {
                    user.PasswordHash = Security.EncodePassword(randomPassword);
                    user.Save();
                }

                new ResetPasswordMail
                {
                    To = user,
                    NewPassord = randomPassword
                }.Send();

                ViewData["email"] = email;
                return RedirectToAction("RememberPasswordSuccess");
            }
            catch (Exception ex)
            {
                return RememberPasswordError("_FORM", ex.Message);
            }
        }

        ViewResult RememberPasswordError(string key, string error)
        {
            ModelState.AddModelError("_FORM", error);
            return View(AuthClient.RememberPasswordUrl);
        }

        public ActionResult RememberPasswordSuccess()
        {
            ViewData["Message"] = Resources.PasswordHasBeenSent.Formato(ViewData["email"]);
            ViewData[ViewDataKeys.PageTitle] = Resources.RememberPassword;

            return View(AuthClient.RememberPasswordSuccessUrl);
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
                referrer = System.Web.HttpContext.Current.Request.UrlReferrer.TryCC(r => r.AbsolutePath);
                string current = System.Web.HttpContext.Current.Request.RawUrl;
                if (referrer != null && referrer != current)
                    ViewData["referrer"] = System.Web.HttpContext.Current.Request.UrlReferrer.AbsolutePath;
            }

            ViewData[ViewDataKeys.PageTitle] = "Login";

            return View(AuthClient.LoginUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Login(string username, string password, bool rememberMe, string returnUrl)
        {
            // Basic parameter validation
            if (!username.HasText())
                return LoginError("username", Resources.UserNameMustHaveAValue);

            if (string.IsNullOrEmpty(password))
                return LoginError("password", Resources.PasswordMustHaveAValue);

            // Attempt to login
            UserDN user = null;
            try
            {
                user = AuthLogic.Login(username, Security.EncodePassword(password));
            }
            catch (Exception) { }

            if (user == null)
                return LoginError("_FORM", Resources.InvalidUsernameOrPassword);

            if (OnUserPreLogin != null)
                OnUserPreLogin(this, user);

            Thread.CurrentPrincipal = user;

            //guardamos una cookie persistente si se ha seleccionado
            if (rememberMe)
            {
                string ticketText = UserTicketLogic.NewTicket(
                       System.Web.HttpContext.Current.Request.UserHostAddress);

                HttpCookie cookie = new HttpCookie(AuthClient.CookieName, ticketText)
                {
                    Expires = DateTime.Now.Add(UserTicketLogic.ExpirationInterval),
                };

                System.Web.HttpContext.Current.Response.Cookies.Add(cookie);
            }

            AddUserSession(user.UserName, user);


            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);
            else
                if (System.Web.HttpContext.Current.Request.Params["referrer"] != null)
                {
                    return Redirect(System.Web.HttpContext.Current.Request.Params["referrer"]);
                }

            if (OnUserLoggedDefaultReturn != null)
                return Redirect(OnUserLoggedDefaultReturn(this));

            return RedirectToAction("Index", "Home");
        }

        ViewResult LoginError(string key, string error)
        {
            ModelState.AddModelError(key, error);
            return View(AuthClient.LoginUrl);
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
                        OnUserPreLogin(null, user);

                    Thread.CurrentPrincipal = user;

                    System.Web.HttpContext.Current.Response.Cookies.Add(new HttpCookie(AuthClient.CookieName, ticketText)
                    {
                        Expires = DateTime.Now.Add(UserTicketLogic.ExpirationInterval),
                    });

                    AddUserSession(user.UserName, user);
                    return true;
                }
                catch
                {
                    //Remove cookie
                    HttpCookie cookie = new HttpCookie(AuthClient.CookieName)
                    {
                        Expires = DateTime.Now.AddDays(-10) // or any other time in the past
                    };
                    System.Web.HttpContext.Current.Response.Cookies.Set(cookie);

                    return false;
                }
            }
        }
        #endregion

        #region Register User (Commented)

        //[AcceptVerbs(HttpVerbs.Post)]
        //public ContentResult RegisterUserValidate(string prefixToIgnore)
        //{
        //    UserDN u = (UserDN)Navigator.ExtractEntity(this, Request.Form);

        //    ChangesLog changesLog = RegisterUserApplyChanges(Request.Form, ref u);

        //    this.ModelState.FromDictionary(changesLog.Errors, Request.Form);
        //    return Navigator.ModelState(ModelState);
        //}

        //public ActionResult RegisterUserPost()
        //{
        //    UserDN u = (UserDN)Navigator.ExtractEntity(this, Request.Form);

        //    ChangesLog changesLog = RegisterUserApplyChanges(Request.Form, ref u);
        //    if (changesLog.Errors != null && changesLog.Errors.Count > 0)
        //    {
        //        this.ModelState.FromDictionary(changesLog.Errors, Request.Form);
        //        return Navigator.ModelState(ModelState);
        //    }

        //    u = (UserDN)OperationLogic.ServiceExecute(u, UserOperation.SaveNew);

        //    if (Navigator.ExtractIsReactive(Request.Form))
        //    {
        //        string tabID = Navigator.ExtractTabID(Request.Form);
        //        Session[tabID] = u;
        //    }

        //    return Navigator.View(this, u);
        //}

        //ChangesLog RegisterUserApplyChanges(NameValueCollection form, ref UserDN u)
        //{
        //    List<string> fullIntegrityErrors;
        //    ChangesLog changesLog = Navigator.ApplyChangesAndValidate(this, ref u, "", "my", out fullIntegrityErrors);
        //    if (fullIntegrityErrors != null && fullIntegrityErrors.Count > 0)
        //    {
        //        fullIntegrityErrors = fullIntegrityErrors.Where(s => !s.Contains("Password Hash")).ToList();
        //        if (fullIntegrityErrors.Count > 0)
        //            changesLog.Errors.Add(ViewDataKeys.GlobalErrors, fullIntegrityErrors);
        //    }
        //    if (u != null && u.UserName.HasText())
        //    {
        //        string username = u.UserName;
        //        if (Database.Query<UserDN>().Any(us => us.UserName == username))
        //            changesLog.Errors.Add(ViewDataKeys.GlobalErrors, new List<string> { Resources.UserNameAlreadyExists });
        //    }
        //    return changesLog;
        //}

        //Dictionary<string, List<string>> UserOperationApplyChanges(NameValueCollection form, ref UserDN u)
        //{
        //    List<string> fullIntegrityErrors;
        //    ChangesLog changesLog = Navigator.ApplyChangesAndValidate(this, ref u, "", "my", out fullIntegrityErrors);
        //    if (fullIntegrityErrors != null && fullIntegrityErrors.Count > 0)
        //        changesLog.Errors.Add(ViewDataKeys.GlobalErrors, fullIntegrityErrors.Where(s => !s.Contains("Password Hash")).ToList());

        //    return changesLog.Errors;
        //}

        //public ActionResult UserExecOperation(string sfRuntimeType, int? sfId, string sfOperationFullKey, bool isLite, string prefix, string sfOnOk, string sfOnCancel)
        //{
        //    Type type = Navigator.ResolveType(sfRuntimeType);

        //    UserDN entity = null;
        //    if (isLite)
        //    {
        //        if (sfId.HasValue)
        //        {
        //            Lite lite = Lite.Create(type, sfId.Value);
        //            entity = (UserDN)OperationLogic.ServiceExecuteLite((Lite)lite, EnumLogic<OperationDN>.ToEnum(sfOperationFullKey));
        //        }
        //        else
        //            throw new ArgumentException(Resources.CouldNotCreateLiteWithoutAnIdToCallOperation0.Formato(sfOperationFullKey));
        //    }
        //    else
        //    {
        //        //if (sfId.HasValue)
        //        //    entity = Database.Retrieve<UserDN>(sfId.Value);
        //        //else
        //        //    entity = (UserDN)Navigator.CreateInstance(type);

        //        entity = (UserDN)Navigator.ExtractEntity(this, Request.Form);

        //        Dictionary<string, List<string>> errors = UserOperationApplyChanges(Request.Form, ref entity);

        //        if (errors != null && errors.Count > 0)
        //        {
        //            this.ModelState.FromDictionary(errors, Request.Form);
        //            return Navigator.ModelState(ModelState);
        //        }

        //        entity = (UserDN)OperationLogic.ServiceExecute(entity, EnumLogic<OperationDN>.ToEnum(sfOperationFullKey));

        //        if (Navigator.ExtractIsReactive(Request.Form))
        //        {
        //            string tabID = Navigator.ExtractTabID(Request.Form);
        //            Session[tabID] = entity;
        //        }
        //    }

        //    if (prefix.HasText())
        //        return Navigator.PopupView(this, entity, prefix);
        //    else //NormalWindow
        //        return Navigator.View(this, entity);
        //} 
        #endregion

        public static void UpdateSessionUser()
        {
            var newUser = UserDN.Current.ToLite().Retrieve();

            Thread.CurrentPrincipal = newUser;

            if (System.Web.HttpContext.Current != null)
                System.Web.HttpContext.Current.Session[SessionUserKey] = newUser;
        }

        public static void AddUserSession(string userName, UserDN user)
        {
            System.Web.HttpContext.Current.Session[SessionUserKey] = user;

            if (OnUserLogged != null)
                OnUserLogged();
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            //Session.RemoveAll();
            //Session.Remove(SessionUserKey);
            var authCookie = System.Web.HttpContext.Current.Request.Cookies[AuthClient.CookieName];
            if (authCookie != null && authCookie.Value.HasText())
                Response.Cookies[AuthClient.CookieName].Expires = DateTime.Now.AddDays(-10);

            Session.Abandon();

            return RedirectToAction("Index", "Home");
        }

    }
}
