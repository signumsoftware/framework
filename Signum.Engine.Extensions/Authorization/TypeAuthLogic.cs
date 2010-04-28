using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using System.Threading;
using Signum.Entities;
using Signum.Utilities;
using System.Reflection;
using System.Security.Authentication;
using Signum.Engine.Extensions.Properties;
using Signum.Entities.Reflection;

namespace Signum.Engine.Authorization
{
    public static class TypeAuthLogic
    {
        static AuthCache<RuleTypeDN, TypeDN, Type, TypeAllowed> cache;

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertIsStarted(sb);
                sb.Schema.EntityEventsGlobal.Saving += Schema_Saving;
                sb.Schema.EntityEventsGlobal.Retrieving += Schema_Retrieving;
                sb.Schema.IsAllowedCallback += new Func<Type, bool>(Schema_IsAllowedCallback);

                cache = new AuthCache<RuleTypeDN, TypeDN, Type, TypeAllowed>(sb,
                    dn => TypeLogic.DnToType[dn],
                    type => TypeLogic.TypeToDN[type], MaxTypeAllowed, TypeAllowed.Create);
            }
        }

        static TypeAllowed MaxTypeAllowed(this IEnumerable<TypeAllowed> collection)
        {
            return collection.Max();
        }

        static bool Schema_IsAllowedCallback(Type type)
        {
            if (!AuthLogic.IsEnabled)
                return true;

            return cache.GetAllowed(RoleDN.Current, type) != TypeAllowed.None;
        }

        static void Schema_Saving(IdentifiableEntity ident, bool isRoot, ref bool graphModified)
        {
            if (AuthLogic.IsEnabled)
            {
                TypeAllowed access = cache.GetAllowed(RoleDN.Current, ident.GetType());

                if (access == TypeAllowed.Create || (!ident.IsNew && access == TypeAllowed.Modify))
                    return;

                throw new UnauthorizedAccessException(Resources.NotAuthorizedToSave0.Formato(ident.GetType()));
            }
        }

        static void Schema_Retrieving(Type type, int id, bool isRoot)
        {
            if (AuthLogic.IsEnabled)
            {
                TypeAllowed access = cache.GetAllowed(RoleDN.Current, type);
                if (access < TypeAllowed.Read)
                    throw new UnauthorizedAccessException(Resources.NotAuthorizedToRetrieve0.Formato(type));
            }
        }

        public static TypeRulePack GetTypeRules(Lite<RoleDN> roleLite)
        {
            return new TypeRulePack
            {
                Role = roleLite,
                Rules = cache.GetRules(roleLite, TypeLogic.TypeToDN.Where(t => !t.Key.IsEnumProxy()).Select(a => a.Value)).ToMList()
            };
        }

        public static void SetTypeRules(TypeRulePack rules)
        {
            cache.SetRules(rules, r => true);
        }

        public static void SetTypeAllowed(Lite<RoleDN> role, Type type, TypeAllowed allowed)
        {
            cache.SetAllowed(role, type, allowed);
        }

        public static TypeAllowed GetTypeAllowed(Type type)
        {
            return cache.GetAllowed(RoleDN.Current, type);
        }

        public static Dictionary<Type, TypeAllowed> AuthorizedTypes()
        {
            return cache.GetCleanDictionary();
        }
    }
}
