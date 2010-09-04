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
using Signum.Entities;
using Signum.Utilities.Reflection;
using Signum.Utilities.DataStructures;

namespace Signum.Windows.Authorization
{
    public static class AuthClient
    {
        static HashSet<object> authorizedQueries; 
        static Dictionary<Type, TypeAllowedBasic> typeRules; 
        static Dictionary<PropertyRoute, PropertyAllowed> propertyRules;
        static Dictionary<Enum, bool> permissionRules;
        static Dictionary<Type, MinMax<TypeAllowedBasic>> typesGroupsAllowed;

        public static bool Types { get; private set; }
        public static bool Properties { get; private set; }
        public static bool Queries { get; private set; }
        public static bool Permissions { get; private set; }
        public static bool EntityGroups { get; private set; }
        public static bool FacadeMethods { get; private set; }

        public static void UpdateCache()
        {
            if (Types)
                typeRules = Server.Return((ITypeAuthServer s) => s.AuthorizedTypes());

            if (Properties)
                propertyRules = Server.Return((IPropertyAuthServer s) => s.AuthorizedProperties());

            if (Queries)
                authorizedQueries = Server.Return((IQueryAuthServer s) => s.AuthorizedQueries()); 

            if (Permissions)
                permissionRules = Server.Return((IPermissionAuthServer s) => s.PermissionRules());

            if (EntityGroups)
                typesGroupsAllowed = Server.Return((IEntityGroupAuthServer s) => s.GetEntityGroupTypesAllowed());
        }

        public static bool? TryIsAuthorized(this Enum permissionKey)
        {
            return permissionRules.TryGetS(permissionKey);
        }

        public static bool IsAuthorized(this Enum permissionKey)
        {
            if(permissionRules == null)
                throw new InvalidOperationException("Permissions not enabled in AuthClient");
 
            bool result;
            if(!permissionRules.TryGetValue(permissionKey, out result))
                throw new ArgumentException("{0} is not a permissionKey registered in the server".Formato(permissionKey));
 
            return result;
        }

        public static void Start(bool types, bool property, bool queries, bool permissions, bool facadeMethods, bool entityGroups)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Types = types;
                Properties = property;
                Queries = queries;
                Permissions = permissions;
                FacadeMethods = facadeMethods;
                EntityGroups = entityGroups;

                Server.Connecting += UpdateCache;

                UpdateCache();

                Navigator.AddSetting(new EntitySettings<UserDN>(EntityType.Admin) { View = e => new User() });
                Navigator.AddSetting(new EntitySettings<RoleDN>(EntityType.Default) { View = e => new Role() });

                if (property)
                {
                    Common.RouteTask += Common_RouteTask;
                    Common.PseudoRouteTask += Common_RouteTask;
                    PropertyRoute.SetIsAllowedCallback(pr => GetPropertyAllowed(pr) >= PropertyAllowed.Read);
                }

                if (types)
                {
                    MenuManager.Tasks += new Action<MenuItem>(MenuManager_TasksTypes);

                    Navigator.Manager.Initializing += () =>
                    {
                        foreach (EntitySettings es in Navigator.Manager.EntitySettings.Values)
                        {
                            if (typeof(IdentifiableEntity).IsAssignableFrom(es.StaticType))
                                miAttachTypeEvent.GenericInvoke(new Type[] { es.StaticType }, null, new object[] { es });
                        }
                    };                   
                }

                if (queries)
                {
                    Navigator.Manager.Initializing += () =>
                    {
                        foreach (QuerySettings qs in Navigator.Manager.QuerySetting.Values)
                        {
                            qs.IsFindableEvent += qn=> GetQueryAceess(qn);
                        }
                    };

                    MenuManager.Tasks += new Action<MenuItem>(MenuManager_TasksQueries);
                }


                if (entityGroups)
                {
                    Navigator.Manager.Initializing += () =>
                    {
                        foreach (var es in Navigator.Manager.EntitySettings.Values.Where(es => typesGroupsAllowed.ContainsKey(es.StaticType)))
                        {
                            miAttachEntityGroupsEvent.GenericInvoke(new Type[] { es.StaticType }, null, new object[] { es });
                        }
                    };
                }

                Links.RegisterEntityLinks<RoleDN>((r, c) =>
                {
                    bool authorized = BasicPermissions.AdminRules.TryIsAuthorized() ?? true;
                    return new QuickLink[]
                    {
                         new QuickLinkAction("Type Rules", () => 
                            new TypeRules 
                            { 
                                Owner = c.FindCurrentWindow(),
                                Role = r.ToLite(), 
                                Properties = property, 
                                Operations = Server.Implements<IOperationAuthServer>(), 
                                Queries = queries 
                            }.Show())
                         { 
                             IsVisible = authorized && types
                         },
                         new QuickLinkAction("Permission Rules", () => new PermissionRules { Role = r.ToLite(), Owner = c.FindCurrentWindow() }.Show())
                         {
                             IsVisible = authorized && Permissions
                         },
                         new QuickLinkAction("Facade Method Rules", () => new FacadeMethodRules { Role = r.ToLite(), Owner = c.FindCurrentWindow() }.Show())
                         { 
                             IsVisible = authorized && FacadeMethods
                         },
                         new QuickLinkAction("Entity Groups", () => new EntityGroupRules { Role = r.ToLite(), Owner = c.FindCurrentWindow() }.Show())
                         {
                             IsVisible = authorized && EntityGroups,
                         }
                     };
                }); 
            }
        }


        static MethodInfo miAttachTypeEvent = ReflectionTools.GetMethodInfo(() => AttachTypeEvent<IdentifiableEntity>(null)).GetGenericMethodDefinition();

        private static void AttachTypeEvent<T>(EntitySettings<T> settings) where T:IdentifiableEntity
        {
            settings.IsCreableEvent += admin => GetTypeAllowed(typeof(T)) == TypeAllowedBasic.Create;
            settings.IsReadOnlyEvent += (entity, admin) => GetTypeAllowed(typeof(T)) <= TypeAllowedBasic.Read;
            settings.IsViewableEvent += (entity, admin) => GetTypeAllowed(typeof(T)) >= TypeAllowedBasic.Read;
        }

        static MethodInfo miAttachEntityGroupsEvent = ReflectionTools.GetMethodInfo(() => AttachEntityGroupsEvent<IdentifiableEntity>(null)).GetGenericMethodDefinition();

        private static void AttachEntityGroupsEvent<T>(EntitySettings<T> settings) where T : IdentifiableEntity
        {
            settings.IsReadOnlyEvent += (entity, admin) => entity == null? false: !IsEntityGroupAllowedFor(entity, TypeAllowedBasic.Modify);
            settings.IsViewableEvent += (entity, admin) => entity == null ? true : IsEntityGroupAllowedFor(entity, TypeAllowedBasic.Read);
        }

        private static bool IsEntityGroupAllowedFor(IdentifiableEntity entity, TypeAllowedBasic allowed)
        {
            if (entity.IsNew)
                return true;

            MinMax<TypeAllowedBasic> minMax = typesGroupsAllowed[entity.GetType()];

            if (minMax.Min >= allowed)
                return true;

            if (minMax.Max < allowed)
                return false;

            return Server.Return((IEntityGroupAuthServer s) => s.IsAllowedFor(entity.ToLite(), allowed));
        }

        static void MenuManager_TasksTypes(MenuItem menuItem)
        {
            if (menuItem.NotSet(MenuItem.VisibilityProperty))
            {
                object tag = menuItem.Tag;

                if (tag == null)
                    return;

                Type type = tag as Type ?? (tag as AdminOptions).TryCC(a => a.Type);

                if (type != null && Navigator.Manager.EntitySettings.ContainsKey(type))
                {
                    if (GetTypeAllowed(type) == TypeAllowedBasic.None)
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
                    tag is FindOptionsBase ? ((FindOptionsBase)tag).QueryName :
                    tag;

                if (queryName != null && Navigator.Manager.QuerySetting.ContainsKey(queryName))
                {
                    if (!GetQueryAceess(queryName))
                        menuItem.Visibility = Visibility.Collapsed;
                }
            }
        }

        static TypeAllowedBasic GetTypeAllowed(Type type)
        {
            return typeRules.TryGetS(type) ?? TypeAllowedBasic.Create;
        }

        static PropertyAllowed GetPropertyAllowed(PropertyRoute route)
        {
            if (route.PropertyRouteType == PropertyRouteType.MListItems || route.PropertyRouteType == PropertyRouteType.LiteEntity)
                return GetPropertyAllowed(route.Parent);

            return propertyRules.TryGetS(route) ?? PropertyAllowed.Modify;
        }

        static bool GetQueryAceess(object queryName)
        {
            return authorizedQueries.Contains(queryName); 
        }

        static void Common_RouteTask(FrameworkElement fe, string route, PropertyRoute context)
        {
            if (context.PropertyRouteType == PropertyRouteType.Property)
            {
                switch (GetPropertyAllowed(context))
                {
                    case PropertyAllowed.None: fe.Visibility = Visibility.Collapsed; break;
                    case PropertyAllowed.Read: Common.SetIsReadOnly(fe, true); break;
                    case PropertyAllowed.Modify: break;
                } 
            }
        }
    }
}
