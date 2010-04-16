using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System.Threading;
using Signum.Entities;
using System.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Authorization
{

    public static class PermissionAuthLogic
    {
        static List<Type> permissionTypes = new List<Type>();
        public static void RegisterTypes(params Type[] types)
        {
            permissionTypes.AddRange(types);
        }

        static AuthCache<RulePermissionDN, PermissionDN, Enum, bool> cache;

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null)));
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertIsStarted(sb);

                sb.Include<PermissionDN>();

                EnumLogic<PermissionDN>.Start(sb, () => permissionTypes.SelectMany(t => Enum.GetValues(t).Cast<Enum>()).ToHashSet());

                cache = new AuthCache<RulePermissionDN, PermissionDN, Enum, bool>(sb,
                    EnumLogic<PermissionDN>.ToEnum,
                    EnumLogic<PermissionDN>.ToEntity,
                    AuthUtils.MaxAllowed, true);

                RegisterTypes(typeof(BasicPermissions));
            }
        }

        public static void Authorize(this Enum permissionKey)
        {
            if (!cache.GetAllowed(RoleDN.Current, permissionKey))
                throw new UnauthorizedAccessException("Permission '{0}' is denied".Formato(permissionKey));
        }

        public static Dictionary<Enum, bool> ServicePermissionRules()
        {
            RoleDN role = RoleDN.Current;

            return EnumLogic<PermissionDN>.Keys.ToDictionary(a => a, a => cache.GetAllowed(RoleDN.Current, a));
        }

        public static bool IsAuthorized(this Enum permissionKey)
        {
            return cache.GetAllowed(RoleDN.Current, permissionKey);
        }

        public static PermissionRulePack GetPermissionRules(Lite<RoleDN> roleLite)
        {
            var role = roleLite.Retrieve();

            return new PermissionRulePack
            {
                Role = roleLite,
                Rules = cache.GetRules(roleLite, EnumLogic<PermissionDN>.AllEntities()).ToMList()
            };
        }

        public static void SetPermissionRules(PermissionRulePack rules)
        {
            cache.SetRules(rules, r => true);
        }

        public static void SetPermissionAllowed(Lite<RoleDN> role, Enum permissionKey, bool allowed)
        {
            cache.SetAllowed(role, permissionKey, allowed);
        }
    }
}
