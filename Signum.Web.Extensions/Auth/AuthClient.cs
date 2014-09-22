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
using System.Diagnostics;
using Signum.Engine;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using Signum.Engine.Maps;
using System.Web.Routing;
using System.Web;
using Signum.Utilities.Reflection;
using Signum.Web.Omnibox;
using Signum.Web.AuthAdmin;
#endregion

namespace Signum.Web.Auth
{
    public static class AuthClient
    {
        public static Func<string, string> PublicLoginUrl = (string returnUrl) =>
        {
            return RouteHelper.New().Action((AuthController c) => c.Login(returnUrl));
        };
 
        public static string ViewPrefix = "~/auth/Views/{0}.cshtml";

        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Auth/Scripts/Auth");
        
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
                    Navigator.AddSetting(new EntitySettings<UserDN>());

                if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(RoleDN)))
                    Navigator.AddSetting(new EntitySettings<RoleDN>());

                if (resetPassword)
                    Navigator.AddSetting(new EntitySettings<ResetPasswordRequestDN>());

                if (passwordExpiration)
                {
                    Navigator.AddSetting(new EntitySettings<PasswordExpiresIntervalDN> { PartialViewName = _ => ViewPrefix.Formato("PasswordValidInterval") });
                }

                Navigator.AddSetting(new EmbeddedEntitySettings<SetPasswordModel>
                {
                    PartialViewName = _ => ViewPrefix.Formato("SetPassword"),
                    MappingDefault = new EntityMapping<SetPasswordModel>(false)
                    .SetProperty(a => a.Password, ctx => UserMapping.GetNewPassword(ctx, UserMapping.NewPasswordKey, UserMapping.NewPasswordBisKey))
                });

                if (property)
                {
                    Common.CommonTask += TaskAuthorizeProperties;
                    Mapping.CanChange += Mapping_CanChange;
                }


                var manager = Navigator.Manager;
                if (types)
                {
                    manager.IsCreable += manager_IsCreable;
                    manager.IsReadOnly += manager_IsReadOnly;
                    manager.IsViewable += manager_IsViewable;
                }

                if (queries)
                {
                    Finder.Manager.IsFindable += QueryAuthLogic.GetQueryAllowed;
                }

                AuthenticationRequiredAttribute.Authenticate = context =>
                {
                    if (UserDN.Current == null)
                    {
                        string returnUrl = context.HttpContext.Request.SuggestedReturnUrl().PathAndQuery;

                        //send them off to the login page
                        string loginUrl = PublicLoginUrl(returnUrl);
                        context.Result = context.Controller.RedirectHttpOrAjax(loginUrl);
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

                OperationClient.AddSettings(new List<OperationSettings>
                {
                    new EntityOperationSettings<UserDN>(UserOperation.SetPassword) 
                    { 
                        Click = ctx => Module["setPassword"](ctx.Options(), 
                            ctx.Url.Action((AuthController c)=>c.SetPasswordModel()),
                            ctx.Url.Action((AuthController c)=>c.SetPasswordOnOk()))
                    },

                    new EntityOperationSettings<UserDN>(UserOperation.SaveNew) 
                    { 
                         Click = ctx => Module["saveNew"](ctx.Options(), 
                            ctx.Url.Action((AuthController c)=>c.SaveNewUser()))
                    }
                });


            }
        }

        static bool manager_IsViewable(Type type, ModifiableEntity entity)
        {
            if (!typeof(IdentifiableEntity).IsAssignableFrom(type))
                return true;

            var ident = (IdentifiableEntity)entity;

            if (ident == null || ident.IsNew)
                return TypeAuthLogic.GetAllowed(type).MaxUI() >= TypeAllowedBasic.Read;

            return ident.IsAllowedFor(TypeAllowedBasic.Read, inUserInterface: true);
        }

        static bool manager_IsReadOnly(Type type, ModifiableEntity entity)
        {
            if (!typeof(IdentifiableEntity).IsAssignableFrom(type))
                return false;

            var ident = (IdentifiableEntity)entity;

            if (ident == null || ident.IsNew)
                return TypeAuthLogic.GetAllowed(type).MaxUI() < TypeAllowedBasic.Modify;

            return !ident.IsAllowedFor(TypeAllowedBasic.Modify, inUserInterface: true);
        }

        static bool manager_IsCreable(Type type)
        {
            if(!typeof(IdentifiableEntity).IsAssignableFrom(type))
                return true;

            return TypeAuthLogic.GetAllowed(type).MaxUI() == TypeAllowedBasic.Create;
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

            ctx.Result = ctx.Controller.RedirectHttpOrAjax(absoluteLoginUrl); 

            ctx.ExceptionHandled = true;
            ctx.HttpContext.Response.Clear();
            ctx.HttpContext.Response.TrySkipIisCustomErrors = true;

        }

        static string Mapping_CanChange(PropertyRoute route)
        {
            switch (PropertyAuthLogic.GetPropertyAllowed(route))
            {
                case PropertyAllowed.Modify: return null;
                case PropertyAllowed.None:
                case PropertyAllowed.Read:
                default: return AuthMessage.NotAuthorizedToChangeProperty0on1.NiceToString().Formato(route.PropertyString(), route.RootType.NiceName());
            }
        }

        static void TaskAuthorizeProperties(BaseLine bl)
        {
            if (bl.PropertyRoute.PropertyRouteType == PropertyRouteType.FieldOrProperty)
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
            if (ident.IsGraphModified && ident.Is(UserDN.Current))
                Transaction.PostRealCommit += ud =>
                {
                     AuthController.UpdateSessionUser();
                };
        }
    }
}
