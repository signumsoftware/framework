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

namespace Signum.Web.Authorization
{
    public static class AuthClient
    {
        public static string ViewPrefix = "~/Plugin/Signum.Web.Extensions.dll/Signum.Web.Extensions.Authorization.";

        public static string CookieName = "sfUser";
        public static string LoginUrl = ViewPrefix + "Login.aspx";
        public static string LoginUserControlUrl = ViewPrefix + "LoginUserControl.ascx";
        public static string ChangePasswordUrl = ViewPrefix + "ChangePassword.aspx";
        public static string ChangePasswordSuccessUrl = ViewPrefix + "ChangePasswordSuccess.aspx";

        public static string ResetPasswordUrl = ViewPrefix + "ResetPassword.aspx";
        public static string ResetPasswordCodeUrl = ViewPrefix + "ResetPasswordCode.aspx";
        public static string ResetPasswordSuccessUrl = ViewPrefix + "ResetPasswordSuccess.aspx";
        public static string ResetPasswordSetNewUrl = ViewPrefix + "ResetPasswordSetNew.aspx";

        public static string RememberPasswordUrl = ViewPrefix + "RememberPassword.aspx";
        public static string RememberPasswordSuccessUrl = ViewPrefix + "RememberPasswordSuccess.aspx";

        //public static string RegisterUrl = "~/Plugin/Signum.Web.Extensions.dll/Signum.Web.Extensions.Authorization.Register.aspx";

        public static void Start(bool types, bool property, bool queries)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<UserDN>(EntityType.Default), 
                    new EntitySettings<RoleDN>(EntityType.Admin)
                });

                if (property)
                    Common.CommonTask += new CommonTask(TaskAuthorizeProperties);

                if (types)
                {
                    Navigator.Manager.GlobalIsCreable += type => TypeAuthLogic.GetTypeAllowed(type) == TypeAllowed.Create;
                    Navigator.Manager.GlobalIsReadOnly += type => TypeAuthLogic.GetTypeAllowed(type) <= TypeAllowed.Read;
                    Navigator.Manager.GlobalIsNavigable += type => TypeAuthLogic.GetTypeAllowed(type) >= TypeAllowed.Read;
                    Navigator.Manager.GlobalIsViewable += type => TypeAuthLogic.GetTypeAllowed(type) >= TypeAllowed.Read;
                }

                if (queries)
                    Navigator.Manager.GlobalIsFindable += type => QueryAuthLogic.GetQueryAllowed(type);


                AuthenticationRequiredAttribute.Authenticate = context =>
                {
                    if (UserDN.Current == null)
                    {
                        //use the current url for the redirect
                        string redirectOnSuccess = context.HttpContext.Request.Url.AbsolutePath;
                        //send them off to the login page
                        string redirectUrl = string.Format("?ReturnUrl={0}", redirectOnSuccess);
                        string loginUrl = context.HttpContext.Request.ApplicationPath + "/Auth/Login" + redirectUrl;
                        context.HttpContext.Response.Redirect(loginUrl, true);
                    }
                };

                Schema.Current.EntityEvents<UserDN>().Saved += new SavedEntityEventHandler<UserDN>(AuthClient_Saved);

                
            }
        }

        static void AuthClient_Saved(UserDN ident, bool isRoot, bool isNew)
        {
            if (ident.Is(UserDN.Current))
                AuthController.UpdateSessionUser(); 
        }

        public static void StartAuthAdmin(bool types, bool properties, bool queries, bool operations, bool permissions, bool facadeMethods, bool entityGroups)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (types)
                    Register<TypeRulePack, TypeAllowedRule, TypeDN, TypeAllowed, TypeDN>("types", a => a.Resource, "Resource", false);

                if (properties)
                    Register<PropertyRulePack, PropertyAllowedRule, PropertyDN, PropertyAllowed, string>("properties", a => a.Resource.Path, "Resource_Path", true);

                if (queries)
                    Register<QueryRulePack, QueryAllowedRule, QueryDN, bool, string>("queries", a => a.Resource.Key, "Resource_Key", true);

                if (operations)
                    Register<OperationRulePack, OperationAllowedRule, OperationDN, bool, OperationDN>("operations", a => a.Resource, "Resource", true);

                if (permissions)
                    Register<PermissionRulePack, PermissionAllowedRule, PermissionDN, bool, PermissionDN>("permissions", a => a.Resource, "Resource", false);

                if (facadeMethods)
                    Register<FacadeMethodRulePack, FacadeMethodAllowedRule, FacadeMethodDN, bool, string>("facadeMethods", a => a.Resource.Name, "Resource_Name", false);

                if (entityGroups)
                {
                    Register<EntityGroupRulePack, EntityGroupAllowedRule, EntityGroupDN, EntityGroupAllowed, EntityGroupDN>("entityGroups", a => a.Resource, "Resource", false);

                    Navigator.EntitySettings<EntityGroupRulePack>().MappingAdmin
                        .GetProperty(m => m.Rules, rul =>
                        ((EntityMapping<EntityGroupAllowedRule>)((MListDictionaryMapping<EntityGroupAllowedRule, EntityGroupDN>)rul).ElementMapping)
                                .RemoveProperty(a => a.Allowed)
                                .SetProperty(a => a.In, new ValueMapping<TypeAllowed>(), null)
                                .SetProperty(a => a.Out, new ValueMapping<TypeAllowed>(), null));
                }
            }
        }

        public static void StartUserGraph()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.EntitySettings<UserDN>().ShowOkSave = admin => false;

                OperationClient.Manager.Settings.AddRange(new Dictionary<Enum, OperationSettings>
                    {
                        { UserOperation.SaveNew, new EntityOperationSettings { IsVisible = ctx => ctx.Entity.IsNew }},
                        { UserOperation.Save, new EntityOperationSettings { IsVisible = ctx => !ctx.Entity.IsNew }},
                        { UserOperation.Disable, new EntityOperationSettings { IsVisible = ctx => !ctx.Entity.IsNew }},
                        { UserOperation.Enable, new EntityOperationSettings { IsVisible = ctx => !ctx.Entity.IsNew }}
                    });
            }
        }

        static void Register<T, AR, R, A, K>(string partialViewName, Func<AR, K> getKey, string getKeyRoute, bool embedded)
            where T : BaseRulePack<AR>
            where AR: AllowedRule<R, A>, new()
            where R : IdentifiableEntity
            where A : struct
        {
            if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(R)))
                Navigator.AddSetting(new EntitySettings<R>(EntityType.ServerOnly));

            Navigator.AddSetting(new EntitySettings<T>(EntityType.NotSaving)
            {
                PartialViewName = e => "Views/AuthAdmin/{0}".Formato(partialViewName),
                MappingAdmin = new EntityMapping<T>(false)
                    .SetProperty(m => m.Rules,
                    new MListDictionaryMapping<AR, K>(getKey, getKeyRoute)
                    {
                        ElementMapping = new EntityMapping<AR>(false)
                                .SetProperty(p => p.Allowed, new ValueMapping<A>(), null)
                    }, null)
            });

            ButtonBarEntityHelper.RegisterEntityButtons<T>((ControllerContext controllerContext, T entity, string viewName, string prefix) =>
                new[] { new ToolBarButton { 
                    OnClick = (embedded ? "postDialog('{0}', '{1}')" :  "Submit('{0}', '{1}')").Formato(
                        new UrlHelper(controllerContext.RequestContext).Action((embedded? "save" : "") +  partialViewName, "AuthAdmin"), prefix), 
                    Text = Resources.Save } });
        }

        static void TaskAuthorizeProperties(BaseLine bl)
        {
            if (bl.PropertyRoute.PropertyRouteType == PropertyRouteType.Property)
            {
                switch (PropertyAuthLogic.GetPropertyAccess(bl.PropertyRoute))
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
    }
}
