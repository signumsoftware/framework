using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Utilities;
using System.Windows;
using Signum.Services;
using System.Reflection;
using System.Collections;

namespace Signum.Windows.Authorization
{
    public static class AuthClient
    {
        static Dictionary<Type, TypeAccess> typeRules; 
        static Dictionary<Type, Dictionary<string, Access>> propertyRules;


        public static void Start(bool types, bool property)
        {
            Navigator.Manager.Settings.Add(typeof(UserDN), new EntitySettings(true) { View = () => new User() });
            Navigator.Manager.Settings.Add(typeof(RoleDN), new EntitySettings(false) { View = () => new Role() });

            if (property)
            {
                propertyRules = Server.Service<IPropertyAuthServer>().AuthorizedProperties();
                Common.RouteTask += new CommonRouteTask(Common_RouteTask);
            }

            if (types)
            {
                typeRules = Server.Service<ITypeAuthServer>().AuthorizedTypes();
                Navigator.Manager.GlobalIsCreable += type => GetTypeAccess(type) == TypeAccess.Create;
                Navigator.Manager.GlobalIsReadOnly += type => GetTypeAccess(type) < TypeAccess.Modify;
                Navigator.Manager.GlobalIsViewable += type => GetTypeAccess(type) >= TypeAccess.Read;
            }
        }

        static TypeAccess GetTypeAccess(Type type)
        {
           return typeRules.TryGetS(type) ?? TypeAccess.Create;
        }

        static Access GetPropertyAccess(Type type, string property)
        {
            return propertyRules.TryGetC(type).TryGetS(property) ?? Access.Modify;
        }


        static void Common_RouteTask(FrameworkElement fe, string route, TypeContext context)
        {
            var contextList = context.FollowC(a => (a as TypeSubContext).TryCC(t => t.Parent)).ToList();

            if (contextList.Count > 1)
            {
                string path = contextList.OfType<TypeSubContext>().Reverse()
                    .ToString(a => a.PropertyInfo.Map(p =>
                        p.Name == "Item" && p.GetIndexParameters().Length > 1 ? "/" : p.Name), ".");

                path = path.Replace("./.", "/");

                Type type = contextList.Last().Type;

                switch (GetPropertyAccess(type, path))
                {
                    case Access.None: fe.Visibility = Visibility.Collapsed; break;
                    case Access.Read: Common.SetIsReadOnly(fe, true); break;
                    case Access.Modify: break;
                } 
            }
        }
    }
}
