using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using System.Reflection; 

namespace Signum.Engine.Authorization
{
    public static class PropertyAuthLogic
    {
        static AuthCache<RulePropertyDN, PropertyAllowedRule, PropertyDN, PropertyRoute, PropertyAllowed> cache;

        public static bool IsStarted { get { return cache != null; } }

        public static void Start(SchemaBuilder sb, bool queries)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);
                PropertyLogic.Start(sb);

                cache = new AuthCache<RulePropertyDN, PropertyAllowedRule, PropertyDN, PropertyRoute, PropertyAllowed>(sb,
                    PropertyLogic.GetPropertyRoute,
                    PropertyLogic.GetEntity, MaxPropertyAccess, PropertyAllowed.Modify);

                if (queries)
                {
                    PropertyRoute.SetIsAllowedCallback(pp => GetPropertyAllowed(pp) > PropertyAllowed.None);
                }
            }
        }

        static PropertyAllowed MaxPropertyAccess(this IEnumerable<PropertyAllowed> collection)
        {
            return collection.Max();
        }

        public static PropertyRulePack GetPropertyRules(Lite<RoleDN> roleLite, TypeDN typeDN)
        {
            var role = roleLite.Retrieve(); 

            return new PropertyRulePack
            {
                Role = roleLite,
                Type = typeDN,
                Rules = cache.GetRules(roleLite, PropertyLogic.RetrieveOrGenerateProperties(typeDN)).ToMList()
            }; 
        }

        public static void SetPropertyRules(PropertyRulePack rules)
        {
            cache.SetRules(rules, r => r.Type == rules.Type); 
        }

        public static void SetPermissionAllowed(Lite<RoleDN> role, PropertyRoute property, PropertyAllowed allowed)
        {
            cache.SetAllowed(role, property, allowed);
        }

        public static PropertyAllowed GetPropertyAllowed(Lite<RoleDN> role, PropertyRoute property)
        {
            return cache.GetAllowed(role, property);
        }

        public static PropertyAllowed GetPropertyAllowed(PropertyRoute property)
        {
            return cache.GetAllowed(property);
        }

        public static Dictionary<PropertyRoute, PropertyAllowed> AuthorizedProperties()
        {
            return cache.GetCleanDictionary();
        }

        public static AuthThumbnail? GetAllowedThumbnail(Lite<RoleDN> role, Type entityType)
        {
            return PropertyRoute.GenerateRoutes(entityType).Select(pr => cache.GetAllowed(role, pr)).Collapse();
        }
    }
}
