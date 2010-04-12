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

namespace Signum.Web.Authorization
{
    [HandleException]
    public partial class AuthController : Controller
    {
        public static event Action<UserDN> OnUserLogged;
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
            var context = UserDN.Current.ApplyChanges(this.ControllerContext, "", UserMapping.ChangePassword).ValidateGlobal();

            if (context.Errors.Any())
            {
                ViewData["Title"] = Resources.ChangePassword;
                ModelState.FromContext(context);
                return View(AuthClient.ChangePasswordUrl);
            }

            Database.Save(context.Value);

            return RedirectToAction("ChangePasswordSuccess");
        }


        public ActionResult ChangePasswordSuccess()
        {
            ViewData["Message"] = Resources.PasswordHasBeenChangedSuccessfully;
            ViewData["Title"] = Resources.ChangePassword;

            return View(AuthClient.ChangePasswordSuccessUrl);
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
            ViewData["Title"] = Resources.RememberPassword;
            try
            {
                if (string.IsNullOrEmpty(username))
                    return RememberPasswordError("user", Resources.UserNameMustHaveAValue);

                if (string.IsNullOrEmpty(email))
                    return RememberPasswordError("email", Resources.EmailMustHaveAValue);

                UserDN user = AuthLogic.UserToRememberPassword(username, email);
                string randomPassword = GenerateRandomPassword(8);
                using (AuthLogic.Disable())
                {
                    user.PasswordHash = Security.EncodePassword(randomPassword);
                    user.Save();
                }

                string texto = @"Le enviamos este correo por haber solicitado que le recordemos su contraseña. Por seguridad, hemos generado una contraseña aleatoria, que luego podrá cambiar.<br/><br/>La contraseña es <b>" + randomPassword + "</b>";
                MailMessage message = new MailMessage()
                {
                    To = { user.Email },
                    Subject = "Recordatorio de contraseña",
                    From = new MailAddress(AuthClient.RememberPasswordEmailFrom),
                    Body = texto,
                    IsBodyHtml = true
                };

                SmtpClient smtp = new SmtpClient(AuthClient.RememberPasswordEmailSMTP);
                smtp.Credentials = new NetworkCredential(AuthClient.RememberPasswordEmailUser, AuthClient.RememberPasswordEmailPassword);
                smtp.Send(message);

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
            ViewData["Title"] = Resources.RememberPassword;

            return View(AuthClient.RememberPasswordSuccessUrl);
        }

        enum CharType
        {
            Number,
            Upper,
            Lower
        }

        public string GenerateRandomPassword(int size)
        {
            StringBuilder sb = new StringBuilder();
            Random random = new Random();
            int charCode = 0;
            for (int i = 0; i < size; i++)
            {
                CharType type = (CharType)random.Next(3);
                switch (type)
                {
                    case CharType.Number:
                        {
                            charCode = random.Next(48, 58);
                            break;
                        }
                    case CharType.Upper:
                        {
                            charCode = random.Next(65, 91);
                            break;
                        }
                    case CharType.Lower:
                        {
                            charCode = random.Next(97, 123);
                            break;
                        }
                }
                sb.Append(Convert.ToChar(charCode));
            }
            return sb.ToString();
        }

        #endregion

        public ActionResult Login()
        {
            //We store the url referrer so that we can go back when logged in
            string referrer = System.Web.HttpContext.Current.Request.UrlReferrer.TryCC(r=>r.AbsolutePath);
            string current = System.Web.HttpContext.Current.Request.RawUrl;
            if (referrer != null && referrer != current)
                ViewData["referrer"] = System.Web.HttpContext.Current.Request.UrlReferrer.AbsolutePath;
            return View(AuthClient.LoginUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Login(string username, string password, bool rememberMe, string returnUrl)
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
            UserDN user = AuthLogic.Login(username, Security.EncodePassword(password));
            if (user == null)
                return LoginError("_FORM", Resources.InvalidUsernameOrPassword);

            Thread.CurrentPrincipal = user;
            //guardamos una cookie persistente si se ha seleccionado
            if (rememberMe)
            {
                string ticketText = UserTicketLogic.NewTicket(
                       System.Web.HttpContext.Current.Request.UserHostAddress);

                System.Web.HttpContext.Current.Response.Cookies.Add(new HttpCookie(AuthClient.CookieName, ticketText)
                {
                    Expires = DateTime.Now.Add(UserTicketLogic.ExpirationInterval),
                });
            }
            AddUserSession(user.UserName, user);

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);
            else
                if (System.Web.HttpContext.Current.Request.Params["referrer"] != null)
                {
                    return Redirect(System.Web.HttpContext.Current.Request.Params["referrer"]);
                }
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

                    AddUserSession(user.UserName, user);
                    return true;
                }
                catch
                {
                    //Remove cookie
                    HttpCookie cookie = new HttpCookie(AuthClient.CookieName)
                    {
                        Expires = DateTime.Now.AddDays(-1) // or any other time in the past
                    };
                    System.Web.HttpContext.Current.Response.Cookies.Set(cookie);

                    return false;
                }
            }
        }

        #region Register User (Commented)

        //[AcceptVerbs(HttpVerbs.Post)]
        //public ContentResult RegisterUserValidate(string prefixToIgnore)
        //{
        //    UserDN u = (UserDN)Navigator.ExtractEntity(this, Request.Form);

        //    ChangesLog changesLog = RegisterUserApplyChanges(Request.Form, ref u);

        //    this.ModelState.FromDictionary(changesLog.Errors, Request.Form);
        //    return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + "}");
        //}

        //public ActionResult RegisterUserPost()
        //{
        //    UserDN u = (UserDN)Navigator.ExtractEntity(this, Request.Form);

        //    ChangesLog changesLog = RegisterUserApplyChanges(Request.Form, ref u);
        //    if (changesLog.Errors != null && changesLog.Errors.Count > 0)
        //    {
        //        this.ModelState.FromDictionary(changesLog.Errors, Request.Form);
        //        return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + "}");
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
        //            return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + "}");
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

        public static void AddUserSession(string userName, UserDN user)
        {
            System.Web.HttpContext.Current.Session[SessionUserKey] = user;
            Thread.CurrentPrincipal = user;

            //FormsAuthentication.SetAuthCookie(userName, rememberMe);

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
