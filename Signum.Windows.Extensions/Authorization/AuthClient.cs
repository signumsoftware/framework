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
using Signum.Windows;
using System.Windows.Controls;
using Signum.Entities.Basics;

namespace Signum.Windows.Authorization
{
    public static class AuthClient
    {
        static HashSet<object> authorizedQueries; 
        static Dictionary<Type, TypeAccess> typeRules; 
        static Dictionary<Type, Dictionary<string, Access>> propertyRules;


        public static void Start(bool types, bool property, bool queries)
        {
            if (Navigator.Manager.NotDefined<UserDN>())
            {
                Navigator.Manager.Settings.Add(typeof(UserDN), new EntitySettings(EntityType.Admin) { View = () => new User() });
                Navigator.Manager.Settings.Add(typeof(RoleDN), new EntitySettings { View = () => new Role() });
            }

            if (property && Navigator.Manager.NotDefined<RulePropertyDN>())
            {
                propertyRules = Server.Service<IPropertyAuthServer>().AuthorizedProperties();
                Common.RouteTask += Common_RouteTask;
                Common.PseudoRouteTask += Common_RouteTask;
            }

            if (types && Navigator.Manager.NotDefined<RuleTypeDN>())
            {
                typeRules = Server.Service<ITypeAuthServer>().AuthorizedTypes();
                Navigator.Manager.GlobalIsCreable += type => GetTypeAccess(type) == TypeAccess.Create;
                Navigator.Manager.GlobalIsReadOnly += type => GetTypeAccess(type) < TypeAccess.Modify;
                Navigator.Manager.GlobalIsViewable += type => GetTypeAccess(type) >= TypeAccess.Read;

                MenuManager.Tasks += new Action<MenuItem>(MenuManager_TasksTypes);
            }

            if (queries && Navigator.Manager.NotDefined<RuleQueryDN>())
            {
                authorizedQueries = Server.Service<IQueryAuthServer>().AuthorizedQueries();
                Navigator.Manager.GlobalIsFindable += qn => GetQueryAceess(qn);

                MenuManager.Tasks += new Action<MenuItem>(MenuManager_TasksQueries);
            }
        }

        static void MenuManager_TasksTypes(MenuItem menuItem)
        {
            if (menuItem.NotSet(MenuItem.VisibilityProperty))
            {
                object tag = menuItem.Tag;

                if (tag == null)
                    return;

                Type type = tag as Type ?? (tag as AdminOptions).TryCC(a => a.Type);

                if (type != null && Navigator.Manager.Settings.ContainsKey(type))
                {
                    if (GetTypeAccess(type) == TypeAccess.None)
                        menuItem.Visibility = Visibility.Collapsed;
                }
            }
        }

        static void MenuManager_TasksQueries(MenuItem menuItem)
        {
            if (menuItem.NotSet(MenuItem.VisibilityProperty))
            {
                object tag = menuItem.Tag;

                if (tag == null)
                    return;

                object queryName =
                    tag is Type ? null : //maybe a type but only if in FindOptions
                    tag is FindOptions ? ((FindOptions)tag).QueryName :
                    tag;

                if (queryName != null && Navigator.Manager.QuerySetting.ContainsKey(queryName))
                {
                    if (!GetQueryAceess(queryName))
                        menuItem.Visibility = Visibility.Collapsed;
                }
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

        static bool GetQueryAceess(object queryName)
        {
            return authorizedQueries.Contains(queryName); 
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
