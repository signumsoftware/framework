using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Utilities.DataStructures;
using Signum.Services;
using Signum.Entities;
using System.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.Windows.Authorization
{
    public static class EntityGroupAuthClient
    {
        static Dictionary<Type, MinMax<TypeAllowedBasic>> typesGroupsAllowed;

        public static bool Started { get; private set; }

        internal static void Start()
        {
            Started = true;

            AuthClient.UpdateCacheEvent += new Action(AuthClient_UpdateCacheEvent);

            Links.RegisterEntityLinks<RoleDN>((r, c) =>
            {
                bool authorized = BasicPermissions.AdminRules.TryIsAuthorized() ?? true;
                return new QuickLink[]
                {
                    new QuickLinkAction("Entity Groups", () => new EntityGroupRules { Role = r.ToLite(), Owner = c.FindCurrentWindow() }.Show())
                    {
                        IsVisible = authorized,
                    }
                };
            }); 
        }

        static void AuthClient_UpdateCacheEvent()
        {
            typesGroupsAllowed = Server.Return((IEntityGroupAuthServer s) => s.GetEntityGroupTypesAllowed());
        }

        static MethodInfo miAttachEntityGroupsEvent = ReflectionTools.GetMethodInfo(() => AttachEntityGroupsEvent<IdentifiableEntity>(null)).GetGenericMethodDefinition();

        private static void AttachEntityGroupsEvent<T>(EntitySettings<T> settings) where T : IdentifiableEntity
        {
            settings.IsReadOnlyEvent += (entity, admin) => entity == null ? false : !IsEntityGroupAllowedFor(entity, TypeAllowedBasic.Modify);
            settings.IsViewableEvent += (entity, admin) => entity == null ? true : IsEntityGroupAllowedFor(entity, TypeAllowedBasic.Read);
        }

        private static bool IsEntityGroupAllowedFor(IdentifiableEntity entity, TypeAllowedBasic allowed)
        {
            if (entity.IsNew)
                return true;

            MinMax<TypeAllowedBasic> minMax = typesGroupsAllowed[entity.GetType()];

            if (allowed <= minMax.Min)
                return true;

            if (minMax.Max < allowed)
                return false;

            return Server.Return((IEntityGroupAuthServer s) => s.IsAllowedFor(entity.ToLite(), allowed));
        }
    }
}
