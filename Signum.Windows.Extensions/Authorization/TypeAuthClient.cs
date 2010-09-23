using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Entities.Authorization;
using System.Windows.Controls;
using Signum.Utilities;
using Signum.Services;
using System.Windows;

namespace Signum.Windows.Authorization
{
    public static class TypeAuthClient
    {
        static DefaultDictionary<Type, TypeAllowed> typeRules; 

        public static bool Started { get; private set; }

        internal static void Start()
        {
            Started = true;

            MenuManager.Tasks += new Action<MenuItem>(MenuManager_TasksTypes);

            Navigator.Manager.Initializing += () =>
            {
                foreach (EntitySettings es in Navigator.Manager.EntitySettings.Values)
                {
                    if (typeof(IdentifiableEntity).IsAssignableFrom(es.StaticType))
                        miAttachTypeEvent.GenericInvoke(new Type[] { es.StaticType }, null, new object[] { es });
                }
            };

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
                            Properties = PropertyAuthClient.Started, 
                            Operations = OperationAuthClient.Started,
                            Queries = QueryAuthClient.Started
                        }.Show())
                    { 
                        IsVisible = authorized
                    },
                 };
            }); 

            AuthClient.UpdateCacheEvent += new Action(AuthClient_UpdateCacheEvent);
        }

        static MethodInfo miAttachTypeEvent = ReflectionTools.GetMethodInfo(() => AttachTypeEvent<IdentifiableEntity>(null)).GetGenericMethodDefinition();

        private static void AttachTypeEvent<T>(EntitySettings<T> settings) where T : IdentifiableEntity
        {
            settings.IsCreableEvent += admin => GetTypeAllowed(typeof(T)) == TypeAllowedBasic.Create;
            settings.IsReadOnlyEvent += (entity, admin) => GetTypeAllowed(typeof(T)) <= TypeAllowedBasic.Read;
            settings.IsViewableEvent += (entity, admin) => GetTypeAllowed(typeof(T)) >= TypeAllowedBasic.Read;
        }

        static TypeAllowedBasic GetTypeAllowed(Type type)
        {
            return typeRules.GetAllowed(type).GetUI();
        }

        static void AuthClient_UpdateCacheEvent()
        {
            typeRules = Server.Return((ITypeAuthServer s) => s.AuthorizedTypes());
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
    }
}

