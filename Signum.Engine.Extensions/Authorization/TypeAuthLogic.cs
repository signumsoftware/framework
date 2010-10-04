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
        static AuthCache<RuleTypeDN, TypeAllowedRule, TypeDN, Type, TypeAllowed> cache;

        public static IManualAuth<Type, TypeAllowed> Manual { get { return cache; } }

        public static bool IsStarted { get { return cache != null; } }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);              

                sb.Schema.EntityEventsGlobal.Saving += Schema_Saving; //because we need Modifications propagated
                sb.Schema.EntityEventsGlobal.Retrieving += Schema_Retrieving;
                sb.Schema.IsAllowedCallback += Schema_IsAllowedCallback;

                cache = new AuthCache<RuleTypeDN, TypeAllowedRule, TypeDN, Type, TypeAllowed>(sb,
                    dn => TypeLogic.DnToType[dn],
                    type => TypeLogic.TypeToDN[type],
                    AuthUtils.MaxType, 
                    AuthUtils.MinType);
            }
        }

        static string Schema_IsAllowedCallback(Type type)
        {
            return cache.GetAllowed(type).GetDB() != TypeAllowedBasic.None ? null : "Type '{0}' is set to None".Formato(type.NiceName());
        }

        static void Schema_Saving(IdentifiableEntity ident, bool isRoot)
        {
            if (ident.Modified.Value)
            {
                TypeAllowedBasic access = cache.GetAllowed(ident.GetType()).GetDB();

                if (access == TypeAllowedBasic.Create || (!ident.IsNew && access == TypeAllowedBasic.Modify))
                    return;

                throw new UnauthorizedAccessException(Resources.NotAuthorizedToSave0.Formato(ident.GetType()));
            }
        }

        static void Schema_Retrieving(Type type, int id, bool isRoot)
        {
            TypeAllowedBasic access = cache.GetAllowed(type).GetDB();
            if (access < TypeAllowedBasic.Read)
                throw new UnauthorizedAccessException(Resources.NotAuthorizedToRetrieve0.Formato(type));
        }

        public static TypeRulePack GetTypeRules(Lite<RoleDN> roleLite)
        {
            var result = new TypeRulePack { Role = roleLite };

            cache.GetRules(result, TypeLogic.TypeToDN.Where(t => !t.Key.IsEnumProxy()).Select(a => a.Value));

            foreach (TypeAllowedRule r in result.Rules)
	        {
               Type type = TypeLogic.DnToType[r.Resource]; 

                if(OperationAuthLogic.IsStarted)
                    r.Operations = OperationAuthLogic.GetAllowedThumbnail(roleLite, type); 
                
                if(PropertyAuthLogic.IsStarted)
                    r.Properties = PropertyAuthLogic.GetAllowedThumbnail(roleLite, type); 
                
                if(QueryAuthLogic.IsStarted)
                    r.Queries = QueryAuthLogic.GetAllowedThumbnail(roleLite, type); 
	        }

            return result;

        }

        public static void SetTypeRules(TypeRulePack rules)
        {
            cache.SetRules(rules, r => true);
        }

        public static TypeAllowed GetTypeAllowed(Type type)
        {
            if (!TypeLogic.TypeToDN.ContainsKey(type))
                return TypeAllowed.DBCreateUICreate;

            return cache.GetAllowed(type);
        }

        public static TypeAllowed GetTypeAllowed(Lite<RoleDN> role, Type type)
        {
            return cache.GetAllowed(role, type);
        }

        public static DefaultDictionary<Type, TypeAllowed> AuthorizedTypes()
        {
            return cache.GetCleanDictionary();
        }
    }

    public static class AuthThumbnailExtensions
    {
        public static AuthThumbnail? Collapse(this IEnumerable<bool> values)
        {
            bool? acum = null;
            foreach (var item in values)
            {
                if (acum == null)
                    acum = item;
                else if (acum.Value != item)
                    return AuthThumbnail.Mix;
            }

            if (acum == null)
                return null;

            return acum.Value ? AuthThumbnail.All : AuthThumbnail.None;
        }

        public static AuthThumbnail? Collapse(this IEnumerable<PropertyAllowed> values)
        {
            PropertyAllowed? acum = null;
            foreach (var item in values)
            {
                if (acum == null)
                    acum = item;
                else if (acum.Value != item || acum.Value == PropertyAllowed.Read)
                    return AuthThumbnail.Mix;
            }

            if (acum == null)
                return null;

            return 
                acum.Value == PropertyAllowed.None ? AuthThumbnail.None :
                acum.Value == PropertyAllowed.Read ? AuthThumbnail.Mix : AuthThumbnail.All;

        }
    }
}
