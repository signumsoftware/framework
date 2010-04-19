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
        public static string RememberPasswordUrl = ViewPrefix + "RememberPassword.aspx";
        public static string RememberPasswordSuccessUrl = ViewPrefix + "RememberPasswordSuccess.aspx";

        /* Settings to send password when 'Remember Password' is used */
        public static string RememberPasswordEmailFrom;
        public static string RememberPasswordEmailSMTP;
        public static string RememberPasswordEmailUser;
        public static string RememberPasswordEmailPassword;

        //public static string RegisterUrl = "~/Plugin/Signum.Web.Extensions.dll/Signum.Web.Extensions.Authorization.Register.aspx";

        public static void Start(bool types, bool property, bool queries)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<UserDN>(EntityType.Default), 
                    new EntitySettings<RoleDN>(EntityType.Default)
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

                //if (registerUserGraph)
                //{
                //    var settings = OperationClient.Manager.Settings;

                //    Func<IdentifiableEntity, bool> creada = (IdentifiableEntity entity) => !entity.IsNew;
                //    settings.Add(UserOperation.SaveNew, new OperationButton
                //    {
                //        OnClick = "javascript:ValidateAndPostServer('{0}','{1}', '', 'my', true, '*');".Formato("Auth/RegisterUserValidate", "Auth/RegisterUserPost"),
                //        Settings = new EntityOperationSettings { IsVisible = (IdentifiableEntity entity) => entity.IsNew }
                //    });
                //    settings.Add(UserOperation.Save, new OperationButton
                //    {
                //        Settings = new EntityOperationSettings
                //        {
                //            Options = { ControllerUrl = "Auth/UserExecOperation" },
                //            IsVisible = creada
                //        }
                //    });
                //    settings.Add(UserOperation.Disable, new OperationButton { Settings = new EntityOperationSettings { IsVisible = creada } });
                //    settings.Add(UserOperation.Enable, new OperationButton { Settings = new EntityOperationSettings { IsVisible = creada } });
                //}

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


            }
        }

        public static void StartAuthAdmin(bool types, bool properties, bool queries, bool operations, bool permissions, bool facadeMethods, bool entityGroups)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (types)
                    Register<TypeRulePack, TypeDN, TypeAllowed, TypeDN>("types", a => a.Resource, "Resource", false);

                if (properties)
                    Register<PropertyRulePack, PropertyDN, PropertyAllowed, string>("properties", a => a.Resource.Path, "Resource_Path", true);

                if (queries)
                    Register<QueryRulePack, QueryDN, bool, string>("queries", a => a.Resource.Key, "Resource_Key", true);

                if (operations)
                    Register<OperationRulePack, OperationDN, bool, OperationDN>("operations", a => a.Resource, "Resource", true);

                if (permissions)
                    Register<PermissionRulePack, PermissionDN, bool, PermissionDN>("permissions", a => a.Resource, "Resource", false);

                if (facadeMethods)
                    Register<FacadeMethodRulePack, FacadeMethodDN, bool, string>("facadeMethods", a => a.Resource.Name, "Resource_Name", false);

                if (entityGroups)
                    Register<EntityGroupRulePack, EntityGroupDN, EntityGroupAllowed, EntityGroupDN>("entityGroups", a => a.Resource, "Resource", false);
            }
        }

        static void Register<T, R, A, K>(string partialViewName, Func<AllowedRule<R, A>, K> getKey, string getKeyRoute, bool embedded)
            where T : BaseRulePack<R, A>
            where R : IdentifiableEntity
            where A : struct
        {
            if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(R)))
                Navigator.AddSetting(new EntitySettings<R>(EntityType.ServerOnly));

            Navigator.AddSetting(new EntitySettings<T>(EntityType.NotSaving)
            {
                PartialViewName = e => "Views/Auth/{0}".Formato(partialViewName),
                MappingAdmin = new EntityMapping<T>(false)
                    .SetProperty(m => m.Rules,
                    new MListDictionaryMapping<AllowedRule<R, A>, K>(getKey, getKeyRoute)
                    {
                        ElementMapping = new EntityMapping<AllowedRule<R, A>>(false)
                                .SetProperty(p => p.Allowed, new ValueMapping<A>(), null)
                    }, null)
            });

            ButtonBarEntityHelper.RegisterEntityButtons<T>((ControllerContext controllerContext, T entity, string mainControlUrl) =>
                new[] { new ToolBarButton { 
                    OnClick = (embedded ? "postDialog('{0}', '{1}')" : "PostServer('{0}', '{1}')").Formato(
                        new UrlHelper(controllerContext.RequestContext).Action((embedded? "save" : "") +  partialViewName, "Auth")), 
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
                        bl.SetReadOnly();
                        break;
                    case PropertyAllowed.Modify:
                        break;
                }
            }
        }
    }
}
