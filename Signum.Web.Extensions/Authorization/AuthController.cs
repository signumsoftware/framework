using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI;
using System.Threading;
using Signum.Entities.Authorization;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.Entities;
using System.Collections.Specialized;
using Signum.Web.Extensions.Properties;
using Signum.Engine.Operations;
using Signum.Engine.Basics;
using Signum.Entities.Operations;

namespace Signum.Web.Authorization
{

    [HandleException]
    public class AuthController : Controller
    {
        public static event Action<UserDN> OnUserLogged;
        public const string SessionUserKey = "user";

        public ActionResult ChangePassword()
        {
            ViewData["Title"] = Resources.ChangePassword;
            return View(AuthClient.ChangePasswordUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            ViewData["Title"] = Resources.ChangePassword;
            ViewData["PasswordLength"] = AuthLogic.MinRequiredPasswordLength;

            try
            {

                if (string.IsNullOrEmpty(currentPassword))
                    return ChangePasswordError("currentPassword", Resources.YouMustEnterTheCurrentPassword);

                if (newPassword == null || newPassword.Length < AuthLogic.MinRequiredPasswordLength)
                    return ChangePasswordError("newPassword",
                         Resources.PasswordMustHave0orMoreCharacters.Formato(AuthLogic.MinRequiredPasswordLength));

                if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
                    return ChangePasswordError("_FORM", Resources.TheSpecifiedPasswordsDontMatch);

                UserDN usr = AuthLogic.Login((UserDN.Current).UserName, Security.EncodePassword(currentPassword));
                if (usr == null)
                    return ChangePasswordError("_FORM", "Invalid current password");

                usr.PasswordHash = Security.EncodePassword(newPassword);
                Database.Save(usr);
                Session[SessionUserKey] = usr;
                return RedirectToAction("ChangePasswordSuccess");
            }
            catch (Exception ex)
            {
                return ChangePasswordError("_FORM", ex.Message);
            }

        }

        ViewResult ChangePasswordError(string key, string error)
        {
            ModelState.AddModelError("_FORM", error);
            return View(AuthClient.ChangePasswordUrl);
        }

        public ActionResult ChangePasswordSuccess()
        {
            ViewData["Message"] = Resources.PasswordHasBeenChangedSuccessfully;
            ViewData["Title"] = Resources.ChangePassword;

            return View(AuthClient.ChangePasswordSuccessUrl);
        }

        public ActionResult Login()
        {
            return View(AuthClient.LoginUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Login(string username, string password, bool? rememberMe, string returnUrl)
        {
            FormsAuthentication.SignOut();

            ViewData["Title"] = "Login";
            ViewData["rememberMe"] = rememberMe;

            // Basic parameter validation
            if (!username.HasText())
                return LoginError("username", Resources.UserNameMustHaveAValue);

            if (string.IsNullOrEmpty(password))
                return LoginError("password", Resources.PasswordMustHaveAValue);

            // Attempt to login
            UserDN usr = AuthLogic.Login(username, Security.EncodePassword(password));
            if (usr == null)
                return LoginError("_FORM", Resources.InvalidUsernameOrPassword);

            //guardamos una cookie persistente si se ha seleccionado
            if (rememberMe.HasValue && (bool)rememberMe)
            {
                var ticket = new FormsAuthenticationTicket(1, "Id", DateTime.Now, DateTime.Now.AddMonths(2), true, usr.Id.ToString());
                var encryptedTicket = FormsAuthentication.Encrypt(ticket);
                var authCookie = new HttpCookie(AuthClient.CookieName, encryptedTicket)
                    {
                        Expires = ticket.Expiration,
                    };
                HttpContext.Response.Cookies.Add(authCookie);
            }

            AddUserSession(username, rememberMe, usr);

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);
            else
                return RedirectToAction("Index", "Home");
        }

        ViewResult LoginError(string key, string error)
        {
            ModelState.AddModelError("_FORM", error);
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

                    System.Web.HttpContext.Current.Response.Cookies.Add(new HttpCookie(AuthClient.CookieName, ticketText)
                    {
                        Expires = DateTime.Now.Add(UserTicketLogic.ExpirationInterval),
                    });

                    AddUserSession(user.UserName, true, user);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult RegisterUserValidate(string prefixToIgnore)
        {
            UserDN u = (UserDN)Navigator.ExtractEntity(this, Request.Form);

            ChangesLog changesLog = RegisterUserApplyChanges(Request.Form, ref u);

            this.ModelState.FromDictionary(changesLog.Errors, Request.Form);
            return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + "}");
        }

        public ActionResult RegisterUserPost()
        {
            UserDN u = (UserDN)Navigator.ExtractEntity(this, Request.Form);

            ChangesLog changesLog = RegisterUserApplyChanges(Request.Form, ref u);
            if (changesLog.Errors != null && changesLog.Errors.Count > 0)
            {
                this.ModelState.FromDictionary(changesLog.Errors, Request.Form);
                return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + "}");
            }

            u = (UserDN)OperationLogic.ServiceExecute(u, UserOperation.SaveNew);

            if (Navigator.ExtractIsReactive(Request.Form))
            {
                string tabID = Navigator.ExtractTabID(Request.Form);
                Session[tabID] = u;
            }

            return Navigator.View(this, u);
        }

        public ActionResult RegisterUserPostNoOperation()
        {
            UserDN u = (UserDN)Navigator.ExtractEntity(this, Request.Form);

            ChangesLog changesLog = RegisterUserApplyChanges(Request.Form, ref u);
            if (changesLog.Errors != null && changesLog.Errors.Count > 0)
            {
                this.ModelState.FromDictionary(changesLog.Errors, Request.Form);
                return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + "}");
            }

            u = Database.Save(u);

            return Navigator.View(this, u);
        }

        ChangesLog RegisterUserApplyChanges(NameValueCollection form, ref UserDN u)
        {
            List<string> fullIntegrityErrors;
            ChangesLog changesLog = Navigator.ApplyChangesAndValidate(this, ref u, "", "my", out fullIntegrityErrors);
            if (fullIntegrityErrors != null && fullIntegrityErrors.Count > 0)
            {
                fullIntegrityErrors = fullIntegrityErrors.Where(s => !s.Contains("Password Hash")).ToList();
                if (fullIntegrityErrors.Count > 0)
                    changesLog.Errors.Add(ViewDataKeys.GlobalErrors, fullIntegrityErrors);
            }
            if (u != null && u.UserName.HasText())
            {
                string username = u.UserName;
                if (Database.Query<UserDN>().Any(us => us.UserName == username))
                    changesLog.Errors.Add(ViewDataKeys.GlobalErrors, new List<string> { Resources.UserNameAlreadyExists });
            }
            return changesLog;
        }

        Dictionary<string, List<string>> UserOperationApplyChanges(NameValueCollection form, ref UserDN u)
        {
            List<string> fullIntegrityErrors;
            ChangesLog changesLog = Navigator.ApplyChangesAndValidate(this, ref u, "", "my", out fullIntegrityErrors);
            if (fullIntegrityErrors != null && fullIntegrityErrors.Count > 0)
                changesLog.Errors.Add(ViewDataKeys.GlobalErrors, fullIntegrityErrors.Where(s => !s.Contains("Password Hash")).ToList());

            return changesLog.Errors;
        }

        public ActionResult UserExecOperation(string sfRuntimeType, int? sfId, string sfOperationFullKey, bool isLite, string prefix, string sfOnOk, string sfOnCancel)
        {
            Type type = Navigator.ResolveType(sfRuntimeType);

            UserDN entity = null;
            if (isLite)
            {
                if (sfId.HasValue)
                {
                    Lite lite = Lite.Create(type, sfId.Value);
                    entity = (UserDN)OperationLogic.ServiceExecuteLite((Lite)lite, EnumLogic<OperationDN>.ToEnum(sfOperationFullKey));
                }
                else
                    throw new ArgumentException(Resources.CouldNotCreateLiteWithoutAnIdToCallOperation0.Formato(sfOperationFullKey));
            }
            else
            {
                //if (sfId.HasValue)
                //    entity = Database.Retrieve<UserDN>(sfId.Value);
                //else
                //    entity = (UserDN)Navigator.CreateInstance(type);

                entity = (UserDN)Navigator.ExtractEntity(this, Request.Form);

                Dictionary<string, List<string>> errors = UserOperationApplyChanges(Request.Form, ref entity);

                if (errors != null && errors.Count > 0)
                {
                    this.ModelState.FromDictionary(errors, Request.Form);
                    return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + "}");
                }

                entity = (UserDN)OperationLogic.ServiceExecute(entity, EnumLogic<OperationDN>.ToEnum(sfOperationFullKey));

                if (Navigator.ExtractIsReactive(Request.Form))
                {
                    string tabID = Navigator.ExtractTabID(Request.Form);
                    Session[tabID] = entity;
                }
            }

            if (prefix.HasText())
                return Navigator.PopupView(this, entity, prefix);
            else //NormalWindow
                return Navigator.View(this, entity);
        }

        static void AddUserSession(string userName, bool? rememberMe, UserDN user)
        {
            System.Web.HttpContext.Current.Session.Add(SessionUserKey, user);
            Thread.CurrentPrincipal = user;

            FormsAuthentication.SetAuthCookie(userName, rememberMe ?? false);

            if (OnUserLogged != null)
                OnUserLogged(user);
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.RemoveAll();
            var authCookie = System.Web.HttpContext.Current.Request.Cookies[AuthClient.CookieName];
            if (authCookie != null && authCookie.Value.HasText())
                Response.Cookies[AuthClient.CookieName].Expires = DateTime.Now.AddDays(-1);

            return RedirectToAction("Index", "Home");
        }
    }
}
