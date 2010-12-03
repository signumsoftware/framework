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

        public static IManualAuth<PropertyRoute, PropertyAllowed> Manual { get { return cache; } }

        public static bool IsStarted { get { return cache != null; } }

        public static void Start(SchemaBuilder sb, bool queries)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);
                PropertyLogic.Start(sb);

                cache = new AuthCache<RulePropertyDN, PropertyAllowedRule, PropertyDN, PropertyRoute, PropertyAllowed>(sb,
                    PropertyLogic.GetPropertyRoute,
                    PropertyLogic.GetEntity,
                    AuthUtils.MaxProperty,
                    AuthUtils.MinProperty);

                if (queries)
                {
                    PropertyRoute.SetIsAllowedCallback(pp => GetPropertyAllowed(pp) > PropertyAllowed.None);
                }

                AuthLogic.ExportToXml += () => cache.ExportXml("Properties", "Property", p => p.Type.ClassName + "|" + p.Path, pa => pa.ToString());
                AuthLogic.ImportFromXml += (x, roles) => cache.ImportXml(x, "Properties", "Property", roles, s =>
                {
                    var arr = s.Split('|');
                    Type type = TypeLogic.GetType(arr[0]);
                    return PropertyLogic.GetEntity(PropertyRoute.Parse(type, arr[1]));
                }, EnumExtensions.ToEnum<PropertyAllowed>);
            }
        }


        public static PropertyRulePack GetPropertyRules(Lite<RoleDN> roleLite, TypeDN typeDN)
        {
            var result = new PropertyRulePack {Role = roleLite, Type = typeDN }; 
            cache.GetRules(result, PropertyLogic.RetrieveOrGenerateProperties(typeDN));
            return result;
        }

        public static void SetPropertyRules(PropertyRulePack rules)
        {
            cache.SetRules(rules, r => r.Type == rules.Type); 
        }

        public static PropertyAllowed GetPropertyAllowed(Lite<RoleDN> role, PropertyRoute property)
        {
            return cache.GetAllowed(role, property);
        }

        public static PropertyAllowed GetPropertyAllowed(PropertyRoute property)
        {
            return cache.GetAllowed(property);
        }

        public static DefaultDictionary<PropertyRoute, PropertyAllowed> AuthorizedProperties()
        {
            return cache.GetCleanDictionary();
        }

        public static AuthThumbnail? GetAllowedThumbnail(Lite<RoleDN> role, Type entityType)
        {
            return PropertyRoute.GenerateRoutes(entityType).Select(pr => cache.GetAllowed(role, pr)).Collapse();
        }
    }
}
