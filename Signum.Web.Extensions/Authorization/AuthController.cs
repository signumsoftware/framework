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
            ViewData["Title"] = "Cambiar contraseña";
            ViewData["PasswordLength"] = Provider.MinRequiredPasswordLength;

            return View(AuthClient.ChangePasswordUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            ViewData["Title"] = "Cambiar contraseña";
            ViewData["PasswordLength"] = Provider.MinRequiredPasswordLength;

            if (String.IsNullOrEmpty(currentPassword))
                ModelState.AddModelError("currentPassword", "Debe especificar la contraseña actual.");
            
            if (newPassword == null || newPassword.Length < Provider.MinRequiredPasswordLength)
            {
                ModelState.AddModelError("newPassword",
                    String.Format(CultureInfo.CurrentCulture,
                         "Debe especificar una constraseña de {0} o más caracteres.",
                         Provider.MinRequiredPasswordLength));
            }
            if (!String.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
                ModelState.AddModelError("_FORM", "La nueva constraseña y la confirmación no concuerdan.");
            
            if (ModelState.IsValid)
            {
                UserDN usr = null;
                try
                {
                    if (Provider.ValidarUsuario((UserDN.Current).UserName, currentPassword, out usr))
                    {
                        usr.PasswordHash = Security.EncodePassword(newPassword);
                        Database.Save(usr);
                        Session["usuario"] = usr;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("_FORM", ex);
                }

                if (usr == null)
                    ModelState.AddModelError("_FORM", "La nueva contraseña es incorrecta o la nueva contraseña no es válida.");
                else
                    return RedirectToAction(AuthClient.ChangePasswordSuccessUrl);
            }

            return View(AuthClient.ChangePasswordUrl);
        }

        public ActionResult ChangePasswordSuccess()
        {
            ViewData["Mensaje"] = "Su contraseña ha sido cambiada con éxito.";
            ViewData["Titulo"] = "Cambiar contraseña";
            ViewData["Title"] = "Contraseña cambiada";

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
            if (String.IsNullOrEmpty(username))
                ModelState.AddModelError("username", "Debe especificar un nombre de usuario.");

            if (String.IsNullOrEmpty(password))
                ModelState.AddModelError("password", "Debe especificar una contraseña.");
            
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
                    ModelState.AddModelError("_FORM", "El nombre de usuario o la constraseña son incorrectos.");
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

            Dictionary<string, List<string>> errors = RegisterUserApplyChanges(Request.Form, ref u);

            this.ModelState.FromDictionary(errors, Request.Form);
            return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + "}");
        }

        public ActionResult RegisterUserPost()
        {
            UserDN u = (UserDN)Navigator.ExtractEntity(this, Request.Form);

            Dictionary<string, List<string>> errors = RegisterUserApplyChanges(Request.Form, ref u);
            if (errors != null && errors.Count > 0)
            {
                this.ModelState.FromDictionary(errors, Request.Form);
                return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + "}");
            }

            u = (UserDN)OperationLogic.ServiceExecute(u, UserOperation.Alta);

            return Navigator.View(this, u);
        }

        public ActionResult RegisterUserPostNoOperation()
        {
            UserDN u = (UserDN)Navigator.ExtractEntity(this, Request.Form);

            Dictionary<string, List<string>> errors = RegisterUserApplyChanges(Request.Form, ref u);
            if (errors != null && errors.Count > 0)
            {
                this.ModelState.FromDictionary(errors, Request.Form);
                return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + "}");
            }

            u = Database.Save(u);

            return Navigator.View(this, u);
        }

        Dictionary<string, List<string>> RegisterUserApplyChanges(NameValueCollection form, ref UserDN u)
        {
            List<string> fullIntegrityErrors;
            Dictionary<string, List<string>> errors = Navigator.ApplyChangesAndValidate(this, "my", ref u, out fullIntegrityErrors);
            if (fullIntegrityErrors != null && fullIntegrityErrors.Count > 0)
                errors.Add(ViewDataKeys.GlobalErrors, fullIntegrityErrors.Where(s => !s.Contains("Password Hash")).ToList());

            if (u != null && u.UserName.HasText())
            {
                string username = u.UserName;
                if (Database.Query<UserDN>().Any(us => us.UserName == username))
                    errors.Add(ViewDataKeys.GlobalErrors, new List<string> { Resources.UserNameAlreadyExists });
            }
            return errors;
        }

        Dictionary<string, List<string>> UserOperationApplyChanges(NameValueCollection form, ref UserDN u)
        {
            List<string> fullIntegrityErrors;
            Dictionary<string, List<string>> errors = Navigator.ApplyChangesAndValidate(this, "my", ref u, out fullIntegrityErrors);
            if (fullIntegrityErrors != null && fullIntegrityErrors.Count > 0)
                errors.Add(ViewDataKeys.GlobalErrors, fullIntegrityErrors.Where(s => !s.Contains("Password Hash")).ToList());

            return errors;
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
                    throw new ArgumentException("Could not create Lazy without an id to call Operation {{{0}}}".Formato(sfOperationFullKey));
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
            System.Web.HttpContext.Current.Session.Add("usuario", usuario);
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
