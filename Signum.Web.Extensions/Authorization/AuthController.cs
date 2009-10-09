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

    [HandleError]
    public class AuthController : Controller
    {
        public AuthController()
            : this(null, null)
        {
        }

        public AuthController(IFormsAuthentication formsAuth, Provider provider)
        {
            FormsAuth = formsAuth ?? new FormsAuthenticationService();
            Provider = provider ?? new Provider();
        }

        public IFormsAuthentication FormsAuth
        {
            get;
            private set;
        }

        public Provider Provider
        {
            get;
            private set;
        }

        public ActionResult ChangePassword()
        {
            ViewData["Title"] = Resources.ChangePassword;
            ViewData["PasswordLength"] = Provider.MinRequiredPasswordLength;

            return View(AuthClient.ChangePasswordUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            ViewData["Title"] = Resources.ChangePassword;
            ViewData["PasswordLength"] = Provider.MinRequiredPasswordLength;

            if (String.IsNullOrEmpty(currentPassword))
                ModelState.AddModelError("currentPassword", Resources.YouMustEnterTheCurrentPassword);
            
            if (newPassword == null || newPassword.Length < Provider.MinRequiredPasswordLength)
            {
                ModelState.AddModelError("newPassword",
                    String.Format(CultureInfo.CurrentCulture,
                         Resources.PasswordMustHave0orMoreCharacters,
                         Provider.MinRequiredPasswordLength));
            }
            if (!String.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
                ModelState.AddModelError("_FORM", Resources.TheSpecifiedPasswordsDontMatch);
            
            if (ModelState.IsValid)
            {
                UserDN usr = null;
                try
                {
                    if (Provider.ValidarUsuario((UserDN.Current).UserName, currentPassword, out usr))
                    {
                        usr.PasswordHash = Security.EncodePassword(newPassword);
                        Database.Save(usr);
                        Session["user"] = usr;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("_FORM", ex);
                }

                if (usr == null)
                    ModelState.AddModelError("_FORM", Resources.InvalidNewPassword);
                else
                    return RedirectToAction("ChangePasswordSuccess");
            }

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
            FormsAuth.SignOut();

            ViewData["Title"] = "Login";

            // Basic parameter validation
            if (!username.HasText())
                ModelState.AddModelError("username", Resources.UserNameMustHaveAValue);

            if (String.IsNullOrEmpty(password))
                ModelState.AddModelError("password", Resources.PasswordMustHaveAValue);
            
            if (ViewData.ModelState.IsValid)
            {
                // Attempt to login
                UserDN usuario;
                bool loginSuccessful = Provider.ValidarUsuario(username, password, out usuario);

                if (loginSuccessful)
                {
                    //guardamos una cookie persistente si se ha seleccionado
                    if (rememberMe.HasValue && (bool)rememberMe)
                    {
                        var ticket = new FormsAuthenticationTicket(1, "Id", DateTime.Now, DateTime.Now.AddMonths(2), true, usuario.Id.ToString());
                        var encryptedTicket = FormsAuthentication.Encrypt(ticket);
                        var authCookie = new HttpCookie(AuthClient.CookieName, encryptedTicket)
                            {
                                Expires = ticket.Expiration,
                            };
                        HttpContext.Response.Cookies.Add(authCookie);
                    }

                    AddUserSession(username, rememberMe, usuario);

                    if (!String.IsNullOrEmpty(returnUrl))
                        return Redirect(returnUrl);
                    else
                        return RedirectToAction("Index", "Home");
                }
                else
                    ModelState.AddModelError("_FORM", Resources.InvalidUsernameOrPassword);
            }

            // If we got this far, something failed, redisplay form
            ViewData["rememberMe"] = rememberMe;
            return View(AuthClient.LoginUrl);
        }

        public bool LoginFromCookie()
        {
            using (AuthLogic.Disable())
            {
                try
                {
                    var authCookie = System.Web.HttpContext.Current.Request.Cookies[AuthClient.CookieName];
                    if (authCookie == null || !authCookie.Value.HasText())
                        return false;
                    var ticket = FormsAuthentication.Decrypt(authCookie.Value);
                    string idUsuario = ticket.UserData;//Name;
                    //string idUsuario = authCookie["Id"];
                    int id;
                    if (!string.IsNullOrEmpty(idUsuario) && int.TryParse(idUsuario, out id))
                    {
                        UserDN usuario = Database.Retrieve<UserDN>(id);
                        AddUserSession(usuario.UserName, true, usuario);
                        return true;
                    }
                }
                catch
                { }
                return false;

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

        public ActionResult UserExecOperation(string sfTypeName, int? sfId, string sfOperationFullKey, bool isLazy, string prefix, string sfOnOk, string sfOnCancel)
        {
            Type type = Navigator.ResolveType(sfTypeName);

            UserDN entity = null;
            if (isLazy)
            {
                if (sfId.HasValue)
                {
                    Lazy lazy = Lazy.Create(type, sfId.Value);
                    entity = (UserDN)OperationLogic.ServiceExecuteLazy((Lazy)lazy, EnumLogic<OperationDN>.ToEnum(sfOperationFullKey));
                }
                else
                    throw new ArgumentException(Resources.CouldNotCreateLazyWithoutAnIdToCallOperation0.Formato(sfOperationFullKey));
            }
            else
            {
                if (sfId.HasValue)
                    entity = Database.Retrieve<UserDN>(sfId.Value);
                else
                    entity = (UserDN)Navigator.CreateInstance(type);

                Dictionary<string, List<string>> errors = UserOperationApplyChanges(Request.Form, ref entity);

                if (errors != null && errors.Count > 0)
                {
                    this.ModelState.FromDictionary(errors, Request.Form);
                    return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + "}");
                }

                entity = (UserDN)OperationLogic.ServiceExecute(entity, EnumLogic<OperationDN>.ToEnum(sfOperationFullKey));
            }

            if (prefix.HasText())
                return Navigator.PopupView(this, entity, prefix);
            else //NormalWindow
                return Navigator.View(this, entity);
        }

        private void AddUserSession(string username, bool? rememberMe, UserDN usuario)
        {
            System.Web.HttpContext.Current.Session.Add("user", usuario);
            Thread.CurrentPrincipal = usuario;

            FormsAuth.SetAuthCookie(username, rememberMe ?? false);
        }

        public ActionResult Logout()
        {
            FormsAuth.SignOut();
            Session.RemoveAll();
            var authCookie = System.Web.HttpContext.Current.Request.Cookies[AuthClient.CookieName];
            if (authCookie != null && authCookie.Value.HasText())
                Response.Cookies[AuthClient.CookieName].Expires = DateTime.Now.AddDays(-1);

            return RedirectToAction("Index", "Home");
        }
    }

    public interface IFormsAuthentication
    {
        void SetAuthCookie(string userName, bool createPersistentCookie);
        void SignOut();
    }

    public class FormsAuthenticationService : IFormsAuthentication
    {
        public void SetAuthCookie(string userName, bool createPersistentCookie)
        {
            FormsAuthentication.SetAuthCookie(userName, createPersistentCookie);
        }
        public void SignOut()
        {
            FormsAuthentication.SignOut();
        }
    }

    public class Provider
    {
        public int MinRequiredPasswordLength
        {
            get { return 6; }
        }

        public bool ValidarUsuario(string username, string password, out UserDN usuario)
        {
            try
            {
                usuario = AuthLogic.Login(username, Security.EncodePassword(password));
                return true;
            }
            catch
            {
                usuario = null;
                return false;
            }
        }
    }
}
