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
        public static Func<string, string> PublicLoginUrl = (string returnUrl) =>
        {
            return RouteHelper.New().Action("Login", "Auth", new { referrer = returnUrl });
        };

        public static string CookieName = "sfUser";
         
        public static string ViewPrefix = "~/auth/Views/{0}.cshtml";
        
        public static string LoginView = ViewPrefix.Formato("Login");
        public static string LoginUserControlView = ViewPrefix.Formato("LoginUserControl");
        public static string ChangePasswordView = ViewPrefix.Formato("ChangePassword");
        public static string ChangePasswordSuccessView = ViewPrefix.Formato("ChangePasswordSuccess");

        public static string ResetPasswordView = ViewPrefix.Formato("ResetPassword");
        public static string ResetPasswordSendView = ViewPrefix.Formato("ResetPasswordSend");
        public static string ResetPasswordSuccessView = ViewPrefix.Formato("ResetPasswordSuccess");
        public static string ResetPasswordSetNewView = ViewPrefix.Formato("ResetPasswordSetNew");

    
        public static string RememberPasswordSuccessView = ViewPrefix.Formato("RememberPasswordSuccess");

        public static bool ResetPasswordStarted;

        public static void Start(bool types, bool property, bool queries, bool resetPassword, bool passwordExpiration)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                ResetPasswordStarted = resetPassword;

                Navigator.RegisterArea(typeof(AuthClient)); 

                if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(UserDN)))
                    Navigator.AddSetting(new EntitySettings<UserDN>(EntityType.Default));
                Navigator.EntitySettings<UserDN>().ShowSave = false;

                if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(RoleDN)))
                    Navigator.AddSetting(new EntitySettings<RoleDN>(EntityType.Default));

                if (passwordExpiration)
                {
                    Navigator.AddSetting(new EntitySettings<PasswordExpiresIntervalDN>(EntityType.Admin) { PartialViewName = _ => ViewPrefix.Formato("PasswordValidInterval") });                  
                }

                Navigator.AddSetting(new EmbeddedEntitySettings<SetPasswordModel>() { PartialViewName = _ => ViewPrefix.Formato("SetPassword") });

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
                        string returnUrl = context.HttpContext.Request.SuggestedReturnUrl().PathAndQuery;
                                            
                        //send them off to the login page
                        string loginUrl = PublicLoginUrl(returnUrl);
                        if (context.HttpContext.Request.IsAjaxRequest())
                            context.Result = JsonAction.Redirect(loginUrl);
                        else
                            context.Result = new RedirectResult(loginUrl);
                    }
                };

                Schema.Current.EntityEvents<UserDN>().Saving += AuthClient_Saving;

                var defaultException = SignumExceptionHandlerAttribute.OnControllerException;
                SignumExceptionHandlerAttribute.OnControllerException = ctx =>
                {
                    if (ctx.Exception is UnauthorizedAccessException && (UserDN.Current == null || UserDN.Current == AuthLogic.AnonymousUser))
                    {
                        string returnUrl = ctx.HttpContext.Request.SuggestedReturnUrl().PathAndQuery;
                        string loginUrl = PublicLoginUrl(returnUrl);

                        DefaultOnControllerUnauthorizedAccessException(ctx, loginUrl);
                    }
                    else
                    {
                        defaultException(ctx);
                    }
                };


                OperationsClient.Manager.Settings.AddRange(new Dictionary<Enum, OperationSettings>
                {
                    { UserOperation.SetPassword, new EntityOperationSettings 
                    { 
                        OnClick = ctx => new JsOperationConstructorFrom(ctx.Options("SetPassword","Auth"))
                            .ajax(Js.NewPrefix(ctx.Prefix), JsOpSuccess.OpenPopupNoDefaultOk),
                        IsContextualVisible = _ => false
                    }},
                });


            }
        }

        public static Uri SuggestedReturnUrl(this HttpRequestBase request)
        {
            if (request.IsAjaxRequest() || request.HttpMethod == "POST")
                return request.UrlReferrer;
            return request.Url;
        }

        public static void DefaultOnControllerUnauthorizedAccessException(ExceptionContext ctx, string absoluteLoginUrl)
        {
            Exception exception = SignumExceptionHandlerAttribute.CleanException(ctx.Exception);

            HandleErrorInfo model = new HandleErrorInfo(exception, 
                (string)ctx.RouteData.Values["controller"], 
                (string)ctx.RouteData.Values["action"]);

            if (SignumExceptionHandlerAttribute.LogException != null)
                SignumExceptionHandlerAttribute.LogException(model);

            if (ctx.HttpContext.Request.IsAjaxRequest())
                ctx.Result = JsonAction.Redirect(absoluteLoginUrl);
            else
                ctx.Result = new RedirectResult(absoluteLoginUrl);

            ctx.ExceptionHandled = true;
            ctx.HttpContext.Response.Clear();
            ctx.HttpContext.Response.TrySkipIisCustomErrors = true;

        }

        static GenericInvoker<Action<EntitySettings>> miAttachEvents = new GenericInvoker<Action<EntitySettings>>(es => AttachEvents<TypeDN>((EntitySettings<TypeDN>)es));
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

        static void AuthClient_Saving(UserDN ident)
        {
            Transaction.RealCommit += () =>
            {
                if (ident.Is(UserDN.Current))
                    AuthController.UpdateSessionUser();
            };
        }
    }
}
