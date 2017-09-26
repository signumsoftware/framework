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

        public static string LoginView = ViewPrefix.FormatWith("Login");
        public static string LoginUserControlView = ViewPrefix.FormatWith("LoginUserControl");
        public static string ChangePasswordView = ViewPrefix.FormatWith("ChangePassword");
        public static string ChangePasswordSuccessView = ViewPrefix.FormatWith("ChangePasswordSuccess");

        public static string ResetPasswordView = ViewPrefix.FormatWith("ResetPassword");
        public static string ResetPasswordSendView = ViewPrefix.FormatWith("ResetPasswordSend");
        public static string ResetPasswordSuccessView = ViewPrefix.FormatWith("ResetPasswordSuccess");
        public static string ResetPasswordSetNewView = ViewPrefix.FormatWith("ResetPasswordSetNew");


        public static string RememberPasswordSuccessView = ViewPrefix.FormatWith("RememberPasswordSuccess");

        public static bool ResetPasswordStarted;

        public static bool SingleSignOnMessage;

        public static void Start(bool types, bool property, bool queries, bool resetPassword, bool passwordExpiration, bool singleSignOnMessage)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                ResetPasswordStarted = resetPassword;
                SingleSignOnMessage = singleSignOnMessage;

                Navigator.RegisterArea(typeof(AuthClient));

                if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(UserEntity)))
                    Navigator.AddSetting(new EntitySettings<UserEntity>());

                if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(RoleEntity)))
                    Navigator.AddSetting(new EntitySettings<RoleEntity>());

                if (resetPassword)
                    Navigator.AddSetting(new EntitySettings<ResetPasswordRequestEntity>());

                if (passwordExpiration)
                {
                    Navigator.AddSetting(new EntitySettings<PasswordExpiresIntervalEntity> { PartialViewName = _ => ViewPrefix.FormatWith("PasswordValidInterval") });
                }

                Navigator.AddSetting(new ModelEntitySettings<SetPasswordModel>
                {
                    PartialViewName = _ => ViewPrefix.FormatWith("SetPassword"),
                    MappingDefault = new EntityMapping<SetPasswordModel>(false)
                    .SetProperty(a => a.PasswordHash, ctx => UserMapping.GetNewPassword(ctx, UserMapping.NewPasswordKey, UserMapping.NewPasswordBisKey))
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
                    Finder.Manager.IsFindable += q => QueryAuthLogic.GetQueryAllowed(q) != QueryAllowed.None;
                }

                AuthenticationRequiredAttribute.Authenticate = context =>
                {
                    if (UserEntity.Current == null)
                    {
                        string returnUrl = context.HttpContext.Request.SuggestedReturnUrl().PathAndQuery;

                        //send them off to the login page
                        string loginUrl = PublicLoginUrl(returnUrl);
                        context.Result = context.Controller.RedirectHttpOrAjax(loginUrl);
                    }
                };

                Schema.Current.EntityEvents<UserEntity>().Saving += AuthClient_Saving;

                var defaultException = SignumExceptionHandlerAttribute.OnControllerException;
                SignumExceptionHandlerAttribute.OnControllerException = ctx =>
                {
                    if (ctx.Exception is UnauthorizedAccessException && (UserEntity.Current == null || UserEntity.Current.Is(AuthLogic.AnonymousUser)))
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
                    new EntityOperationSettings<UserEntity>(UserOperation.SetPassword)
                    {
                        Click = ctx => Module["setPassword"](ctx.Options(),
                            ctx.Url.Action((AuthController c)=>c.SetPasswordModel()),
                            ctx.Url.Action((AuthController c)=>c.SetPasswordOnOk()))
                    },

                    new EntityOperationSettings<UserEntity>(UserOperation.SaveNew)
                    {
                         Click = ctx => Module["saveNew"](ctx.Options(),
                            ctx.Url.Action((AuthController c)=>c.SaveNewUser()))
                    }
                });


            }
        }

        static bool manager_IsViewable(Type type, ModifiableEntity entity)
        {
            if (!typeof(Entity).IsAssignableFrom(type))
                return true;

            var ident = (Entity)entity;

            if (ident == null || ident.IsNew)
                return TypeAuthLogic.GetAllowed(type).MaxUI() >= TypeAllowedBasic.Read;

            return ident.IsAllowedFor(TypeAllowedBasic.Read, inUserInterface: true);
        }

        static bool manager_IsReadOnly(Type type, ModifiableEntity entity)
        {
            if (!typeof(Entity).IsAssignableFrom(type))
                return false;

            var ident = (Entity)entity;

            if (ident == null || ident.IsNew)
                return TypeAuthLogic.GetAllowed(type).MaxUI() < TypeAllowedBasic.Modify;

            return !ident.IsAllowedFor(TypeAllowedBasic.Modify, inUserInterface: true);
        }

        static bool manager_IsCreable(Type type)
        {
            if (!typeof(Entity).IsAssignableFrom(type))
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
                default: return AuthMessage.NotAuthorizedToChangeProperty0on1.NiceToString().FormatWith(route.PropertyString(), route.RootType.NiceName());
            }
        }

        static void TaskAuthorizeProperties(LineBase bl)
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

        static void AuthClient_Saving(UserEntity ident)
        {
            if (ident.IsGraphModified && ident.Is(UserEntity.Current))
                Transaction.PostRealCommit += ud =>
                {
                    AuthController.UpdateSessionUser();
                };
        }

        public static bool LoginFromCurrentIdentity()
        {
            using (AuthLogic.Disable())
            {
                var identityName = System.Web.HttpContext.Current.User.Identity.Name;

                var user = Database.Query<UserEntity>().SingleOrDefaultEx(u => u.UserName == identityName);

                if (user != null && user.State != UserState.Disabled)
                {
                    AuthController.OnUserPreLogin(null, user);
                    AuthController.AddUserSession(user);
                    return true;
                }

                return false;
            }
        }
    }
}
