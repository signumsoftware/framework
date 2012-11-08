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
        static DefaultDictionary<Type, TypeAllowedAndConditions> typeRules; 

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
                        miAttachTypeEvent.GetInvoker(es.StaticType)(es);
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
                            Owner = Window.GetWindow(c),
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

        static GenericInvoker<Action<EntitySettings>> miAttachTypeEvent = new GenericInvoker<Action<EntitySettings>>(es => AttachTypeEvent<TypeDN>((EntitySettings<TypeDN>)es));
        private static void AttachTypeEvent<T>(EntitySettings<T> settings) where T : IdentifiableEntity
        {
            settings.IsCreable += admin => typeRules.GetAllowed(typeof(T)).Max().GetUI() == TypeAllowedBasic.Create;

            settings.IsReadOnly += (entity, admin) => entity == null || entity.IsNew ?
                typeRules.GetAllowed(typeof(T)).Max().GetUI() < TypeAllowedBasic.Modify :
                !entity.IsAllowedFor(TypeAllowedBasic.Modify);

            settings.IsViewable += (entity, admin) => entity == null || entity.IsNew ?
                typeRules.GetAllowed(typeof(T)).Max().GetUI() >= TypeAllowedBasic.Read :
                entity.IsAllowedFor(TypeAllowedBasic.Read);
        }

        public static bool IsAllowedFor(this Lite lite, TypeAllowedBasic requested)
        {
            TypeAllowedAndConditions tac = GetAllowed(lite.RuntimeType);

            if (requested <= tac.Min().GetUI())
                return true;

            if (tac.Max().GetUI() < requested)
                return false;

            return Server.Return((ITypeAuthServer s) => s.IsAllowedForInUserInterface(lite, requested));
        }

        public static bool IsAllowedFor(this IdentifiableEntity entity, TypeAllowedBasic requested)
        {
            TypeAllowedAndConditions tac = GetAllowed(entity.GetType());

            if (requested <= tac.Min().GetUI())
                return true;

            if (tac.Max().GetUI() < requested)
                return false;

            return Server.Return((ITypeAuthServer s) => s.IsAllowedForInUserInterface(entity.ToLite(), requested));
        }

        public static TypeAllowedAndConditions GetAllowed(Type type)
        {
            TypeAllowedAndConditions tac = typeRules.GetAllowed(type);
            return tac;
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

                Type type = tag as Type;

                if (type != null && Navigator.Manager.EntitySettings.ContainsKey(type))
                {
                    if (typeRules.GetAllowed(type).Max().GetUI() < TypeAllowedBasic.Read)
                        menuItem.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}

