#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using System.Reflection;
using Signum.Web.Operations;
using Signum.Entities;
using System.Web.Mvc;
using Signum.Web.Properties;
using System.Diagnostics;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using Signum.Entities.Operations;
using System.Linq.Expressions;
using Signum.Engine.Maps;
using System.Web.Routing;
using System.Web;
using Signum.Utilities.Reflection;
#endregion

namespace Signum.Web.Auth
{
    public static class AuthClient
    {
        public static string PublicLoginUrl(string returnUrl)
        {
            return RouteHelper.New().Action("Login", "Auth", new { referrer = returnUrl }); 
        }

        public static string CookieName = "sfUser";
        
        public static string ViewPrefix = "~/auth/Views/{0}.cshtml";

        public static string LoginUrl = ViewPrefix.Formato("Login");
        public static string LoginUserControlUrl = ViewPrefix.Formato("LoginUserControl");
        public static string ChangePasswordUrl = ViewPrefix.Formato("ChangePassword");
        public static string ChangePasswordSuccessUrl = ViewPrefix.Formato("ChangePasswordSuccess");

        public static string ResetPasswordUrl = ViewPrefix.Formato("ResetPassword");
        public static string ResetPasswordSendUrl = ViewPrefix.Formato("ResetPasswordSend");
        public static string ResetPasswordSuccessUrl = ViewPrefix.Formato("ResetPasswordSuccess");
        public static string ResetPasswordSetNewUrl = ViewPrefix.Formato("ResetPasswordSetNew");

        public static string RememberPasswordUrl = ViewPrefix.Formato("RememberPassword");
        public static string RememberPasswordSuccessUrl = ViewPrefix.Formato("RememberPasswordSuccess");

        public static bool ResetPasswordStarted;

        public static void Start(bool types, bool property, bool queries, bool resetPassword)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                ResetPasswordStarted = resetPassword;

                Navigator.RegisterArea(typeof(AuthClient)); 

                if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(UserDN)))
                    Navigator.AddSetting(new EntitySettings<UserDN>(EntityType.Default));
                if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(RoleDN)))
                    Navigator.AddSetting(new EntitySettings<RoleDN>(EntityType.Default));
                
                if (property)
                    Common.CommonTask += new CommonTask(TaskAuthorizeProperties);

                if (types)
                {
                    Navigator.Manager.Initializing += () =>
                    {
                        foreach (EntitySettings item in Navigator.Manager.EntitySettings.Values.Where(a=>a.StaticType.IsIdentifiableEntity()))
                        {
                            miAttachEvents.GetInvoker(item.GetType().GetGenericArguments())(item); 
                        }

                    };
                }

                if (queries)
                {
                    Navigator.Manager.Initializing += () =>
                    {
                        foreach (QuerySettings qs in Navigator.Manager.QuerySettings.Values)
                        {
                            qs.IsFindable += QueryAuthLogic.GetQueryAllowed;
                        }
                    };
                }

                AuthenticationRequiredAttribute.Authenticate = context =>
                { 
                    if (UserDN.Current == null)
                    {
                        string returnUrl = context.HttpContext.Request.Url.PathAndQuery;
                                            
                        //send them off to the login page
                        string loginUrl = PublicLoginUrl(returnUrl);
                        if (context.HttpContext.Request.IsAjaxRequest())
                            context.Result = Navigator.RedirectUrl(loginUrl);
                        else
                            context.Result = new RedirectResult(loginUrl);
                    }
                };

                Schema.Current.EntityEvents<UserDN>().Saving += AuthClient_Saving;

                HandleExceptionAttribute.OnExceptionHandled = ctx =>
                {
                    if (ctx.Exception is UnauthorizedAccessException)
                    {
                        string returnUrl = ctx.HttpContext.Request.Url.PathAndQuery;
                        string loginUrl = PublicLoginUrl(returnUrl);

                        HandleAnonymousNotAutorizedException(ctx, loginUrl);
                    }
                    else
                    {
                        HandleExceptionAttribute.DefaultOnException(ctx);
                    }
                };
            }
        }

        public static void HandleAnonymousNotAutorizedException(ExceptionContext ctx, string absoluteLoginUrl)
        { 
            Exception exception = ctx.Exception.FollowC(a => a.InnerException).Last();
            string controllerName = (string)ctx.RouteData.Values["controller"];
            string actionName = (string)ctx.RouteData.Values["action"];
            HandleErrorInfo model = new HandleErrorInfo(exception, controllerName, actionName);

            if (HandleExceptionAttribute.LogControllerException != null)
                HandleExceptionAttribute.LogControllerException(model);

            if (UserDN.Current == null || UserDN.Current == AuthLogic.AnonymousUser)
            {
                //send them off to the login page
                
                if (ctx.HttpContext.Request.IsAjaxRequest())
                    ctx.Result = Navigator.RedirectUrl(absoluteLoginUrl);
                else
                    ctx.Result = new RedirectResult(absoluteLoginUrl);

                ctx.ExceptionHandled = true;
                ctx.HttpContext.Response.Clear();
                ctx.HttpContext.Response.TrySkipIisCustomErrors = true;
            }
            else
            {
                HandleExceptionAttribute.DefaultOnException(ctx);
            }
        }

        static GenericInvoker miAttachEvents = GenericInvoker.Create(() => AttachEvents<Entity>(null));
        static void AttachEvents<T>(EntitySettings<T> settings) where T : IdentifiableEntity
        {
            settings.IsCreable += admin => TypeAuthLogic.GetTypeAllowed(typeof(T)).GetUI() == TypeAllowedBasic.Create;
            settings.IsReadOnly += (_, admin) => TypeAuthLogic.GetTypeAllowed(typeof(T)).GetUI() <= TypeAllowedBasic.Read;
            settings.IsNavigable += (_, admin) => TypeAuthLogic.GetTypeAllowed(typeof(T)).GetUI() >= TypeAllowedBasic.Read;
            settings.IsViewable += (_, admin) => TypeAuthLogic.GetTypeAllowed(typeof(T)).GetUI() >= TypeAllowedBasic.Read;
        }

        static void TaskAuthorizeProperties(BaseLine bl)
        {
            if (bl.PropertyRoute.PropertyRouteType == PropertyRouteType.Property)
            {
                switch (PropertyAuthLogic.GetPropertyAllowed(bl.PropertyRoute))
                {
                    case PropertyAllowed.None:
                        bl.Visible = false;
                        break;
                    case PropertyAllowed.Read:
                        bl.ReadOnly = true;
                        break;
                    case PropertyAllowed.Modify:
                        break;
                }
            }
        }

        static void AuthClient_Saving(UserDN ident, bool isRoot)
        {
            Transaction.RealCommit += () =>
            {
                if (ident.Is(UserDN.Current))
                    AuthController.UpdateSessionUser();
            };
        }
    }
}
