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
        static Dictionary<Type, Dictionary<string, Access>> _runtimeRules;

        static Dictionary<Type, Dictionary<string, Access>> NewCache()
        {
            return Server.Service<IPropertyAuthServer>().AuthorizedProperties(); 
        }

        public static void Start(bool types, bool property)
        {
            Navigator.Manager.Settings.Add(typeof(UserDN), new EntitySettings(true){ View = ()=> new User()});
            Navigator.Manager.Settings.Add(typeof(RoleDN), new EntitySettings(false) { View = () => new Role() });

            if (property)
            {
                _runtimeRules = NewCache();
                Common.RouteTask += new CommonRouteTask(Common_RouteTask);
            }

            if (types)
                Server.Service<ITypeAuthServer>().AuthorizedTypes().JoinDictionaryForeach(Navigator.Manager.Settings, Authorize);          
        }

        static void Authorize(Type type, TypeAccess typeAccess, EntitySettings settings)
        {
            if (typeAccess == TypeAccess.None)
                settings.IsViewable = admin => false;

            if (typeAccess <= TypeAccess.Read)
                settings.IsReadOnly = admin => true;

            if (typeAccess <= TypeAccess.Modify)
                settings.IsCreable = admin => false;
        }

        static Access GetAccess(Type type, string property)
        {
            return _runtimeRules.TryGetC(type).TryGetS(property) ?? Access.Modify;
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

                switch (GetAccess(type, path))
                {
                    case Access.None: fe.Visibility = Visibility.Collapsed; break;
                    case Access.Read: Common.SetIsReadOnly(fe, true); break;
                    case Access.Modify: break;
                } 
            }
        }
    }
}
