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
        //public static string RegisterUrl = "~/Plugin/Signum.Web.Extensions.dll/Signum.Web.Extensions.Authorization.Register.aspx";

        public static void Start(NavigationManager manager, bool types, bool property, bool queries, bool registerUserGraph)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                manager.EntitySettings.Add(typeof(UserDN), new EntitySettings(true)); //{ PartialViewName= ViewPrefix + "UserIU.ascx" }); 
                manager.EntitySettings.Add(typeof(RoleDN), new EntitySettings(false)); //{ View = () => new Role() });

                if (property)
                    Common.CommonTask += new CommonTask(TaskAuthorizeProperties);

                if (types)
                {
                    Navigator.Manager.GlobalIsCreable += type => TypeAuthLogic.GetTypeAccess(type) == TypeAccess.Create;
                    Navigator.Manager.GlobalIsReadOnly += type => TypeAuthLogic.GetTypeAccess(type) < TypeAccess.Modify;
                    Navigator.Manager.GlobalIsViewable += type => TypeAuthLogic.GetTypeAccess(type) >= TypeAccess.Read;
                    Navigator.Manager.GlobalIsPopupViewable += type => TypeAuthLogic.GetTypeAccess(type) >= TypeAccess.Read;
                }

                if (queries)
                    Navigator.Manager.GlobalIsFindable += type => QueryAuthLogic.GetQueryAllowed(type);

                CustomModificationBinders.Binders.Add(typeof(UserDN), (formValues, interval, controlID) => new UserIUModification(typeof(UserDN), formValues, interval, controlID));

                if (registerUserGraph)
                {
                    var settings = OperationClient.Manager.Settings;

                    Func<IdentifiableEntity, bool> creada = (IdentifiableEntity entity) => !entity.IsNew;
                    settings.Add(UserOperation.SaveNew, new EntityOperationSettings
                    {
                        OnClick = "javascript:ValidateAndPostServer('{0}','{1}', '', 'my', true, '*');".Formato("Auth/RegisterUserValidate", "Auth.aspx/RegisterUserPost"),
                        IsVisible = (IdentifiableEntity entity) => entity.IsNew,
                    });
                    settings.Add(UserOperation.Save, new EntityOperationSettings 
                    {
                        OnServerClickAjax = "Auth/UserExecOperation",
                        IsVisible = creada 
                    });
                    settings.Add(UserOperation.Disable, new EntityOperationSettings { IsVisible = creada });
                    settings.Add(UserOperation.Enable, new EntityOperationSettings { IsVisible = creada });
                }

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

        static void TaskAuthorizeProperties(BaseLine bl, Type type, TypeContext context)
        {
            List<PropertyInfo> contextList = context.GetPath();

            if (contextList.Count > 0)
            {
                string path = ((IEnumerable<PropertyInfo>)contextList).Reverse()
                    .ToString(a => a.Map(p =>
                        p.Name == "Item" && p.GetIndexParameters().Length > 1 ? "/" : p.Name), ".");

                path = path.Replace("./.", "/");

                Type parentType = contextList.Last().DeclaringType;

                switch (PropertyAuthLogic.GetPropertyAccess(parentType, path))
                {
                    case Access.None: 
                        bl.Visible = false; 
                        break;
                    case Access.Read:
                        //if (bl.StyleContext == null)
                        //    bl.StyleContext = new StyleContext();
                        //bl.StyleContext.ReadOnly = true;
                        bl.ReadOnly = true;
                        bl.SetReadOnly();
                        break;
                    case Access.Modify: 
                        break;
                }
            }
        }
    }
}
