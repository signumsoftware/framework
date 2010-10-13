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

namespace Signum.Web.Authorization
{
    public static class AuthClient
    {
        public static string ViewPrefix = "auth/Views/";
         
        public static string CookieName = "sfUser";
        
        public static string LoginUrl = ViewPrefix + "Login";
        public static string LoginUserControlUrl = ViewPrefix + "LoginUserControl";
        public static string ChangePasswordUrl = ViewPrefix + "ChangePassword";
        public static string ChangePasswordSuccessUrl = ViewPrefix + "ChangePasswordSuccess";

        public static string ResetPasswordUrl = ViewPrefix + "ResetPassword";
        public static string ResetPasswordSendUrl = ViewPrefix + "ResetPasswordSend";
        public static string ResetPasswordSuccessUrl = ViewPrefix + "ResetPasswordSuccess";
        public static string ResetPasswordSetNewUrl = ViewPrefix + "ResetPasswordSetNew";

        public static string RememberPasswordUrl = ViewPrefix + "RememberPassword";
        public static string RememberPasswordSuccessUrl = ViewPrefix + "RememberPasswordSuccess";

        public static void Start(bool types, bool property, bool queries, bool resetPasswordFeature)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AssemblyResourceManager.RegisterAreaResources(
                    new AssemblyResourceStore(typeof(AuthClient), "/auth/", "Signum.Web.Extensions.Auth."));

                if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(UserDN)))
                    Navigator.AddSetting(new EntitySettings<UserDN>(EntityType.Default));
                if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(RoleDN)))
                    Navigator.AddSetting(new EntitySettings<RoleDN>(EntityType.Default));
                
                if (resetPasswordFeature)
                    Navigator.RegisterTypeName<ResetPasswordRequestDN>();

                if (property)
                    Common.CommonTask += new CommonTask(TaskAuthorizeProperties);

                if (types)
                {
                    Navigator.Manager.Initializing += () =>
                    {
                        foreach (EntitySettings item in Navigator.Manager.EntitySettings.Values)
                        {
                            miAttachEvents.GenericInvoke(item.GetType().GetGenericArguments(), null, new object[] { item }); 
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
                        //send them off to the login page
                        string loginUrl = "Auth/Login?ReturnUrl={0}";
                        if (context.HttpContext.Request.IsAjaxRequest())
                            context.Result = Navigator.RedirectUrl(loginUrl.Formato(context.HttpContext.Request.UrlReferrer.PathAndQuery));
                        else
                            context.Result = new RedirectResult(HttpContextUtils.FullyQualifiedApplicationPath + loginUrl.Formato(context.HttpContext.Request.Url.PathAndQuery));
                    }
                };

                Schema.Current.EntityEvents<UserDN>().Saving += AuthClient_Saving;
            }
        }

        static MethodInfo miAttachEvents = ReflectionTools.GetMethodInfo(() => AttachEvents<Entity>(null)).GetGenericMethodDefinition(); 
        static void AttachEvents<T>(EntitySettings<T> settings) where T : ModifiableEntity
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