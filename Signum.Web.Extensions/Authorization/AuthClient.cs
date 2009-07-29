using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using System.Reflection;

namespace Signum.Web.Authorization
{
    public static class AuthClient
    {
        public static string CookieName = "sfUser"; 
        public static string LoginUrl = "~/Plugin/Signum.Web.Extensions.dll/Signum.Web.Extensions.Authorization.Login.aspx";
        public static string LoginUserControlUrl = "~/Plugin/Signum.Web.Extensions.dll/Signum.Web.Extensions.Authorization.LoginUserControl.ascx";
        public static string ChangePasswordUrl = "~/Plugin/Signum.Web.Extensions.dll/Signum.Web.Extensions.Authorization.ChangePassword.aspx";
        public static string ChangePasswordSuccessUrl = "~/Plugin/Signum.Web.Extensions.dll/Signum.Web.Extensions.Authorization.ChangePasswordSuccess.aspx";
        public static string RegisterUrl = "~/Plugin/Signum.Web.Extensions.dll/Signum.Web.Extensions.Authorization.Register.aspx";

        public static void Start(NavigationManager manager, bool types, bool property, bool queries)
        {
            manager.EntitySettings.Add(typeof(UserDN), new EntitySettings(true)); //{ View = () => new User() });
            manager.EntitySettings.Add(typeof(RoleDN), new EntitySettings(false)); //{ View = () => new Role() });

            if (property)
                Common.CommonTask += new CommonTask(TaskAuthorizeProperties);

            if (types)
            {
                Navigator.Manager.GlobalIsCreable += type => TypeAuthLogic.GetTypeAccess(type) == TypeAccess.Create;
                Navigator.Manager.GlobalIsReadOnly += type => TypeAuthLogic.GetTypeAccess(type) < TypeAccess.Modify;
                Navigator.Manager.GlobalIsViewable += type => TypeAuthLogic.GetTypeAccess(type) >= TypeAccess.Read;
            }

            if (queries)
                Navigator.Manager.GlobalIsFindable += type => QueryAuthLogic.GetQueryAllowed(type);
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
                        bl.View = false; 
                        break;
                    case Access.Read:
                        if (bl.StyleContext == null)
                            bl.StyleContext = new StyleContext();
                        bl.StyleContext.ReadOnly = true;
                        bl.SetReadOnly();
                        break;
                    case Access.Modify: 
                        break;
                }
            }
        }
    }
}
