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
        static Dictionary<RoleDN, Dictionary<Type, Dictionary<string, Access>>> _runtimeRules;
        static Dictionary<RoleDN, Dictionary<Type, Dictionary<string, Access>>> RuntimeRules
        {
            get { return Sync.Initialize(ref _runtimeRules, () => NewCache()); }
        }

        public static void Start(SchemaBuilder sb, bool queries)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertIsStarted(sb);
                PropertyLogic.Start(sb); 
                sb.Include<RulePropertyDN>();
                sb.Schema.Initializing(InitLevel.Level1SimpleEntities, Schema_Initializing);
                sb.Schema.EntityEvents<RulePropertyDN>().Saved +=Schema_Saved;
                AuthLogic.RolesModified += UserAndRoleLogic_RolesModified;

                if (queries)
                {
                    PropertyRoute.SetIsAllowedCallback(pp=>GetPropertyAccess(pp) > Access.None);
                }
            }
        }

        static void Schema_Initializing(Schema sender)
        {
            _runtimeRules = NewCache();
        }

        static void Schema_Saved(RulePropertyDN rule, bool isRoot)
        {
            Transaction.RealCommit += () => _runtimeRules = null;
        }

        static void UserAndRoleLogic_RolesModified()
        {
            Transaction.RealCommit += () => _runtimeRules = null;
        }

        static Access GetAccess(RoleDN role, PropertyRoute propertyPath)
        {
            return RuntimeRules.TryGetC(role).TryGetC(propertyPath.IdentifiableType).TryGetS(propertyPath.PropertyString()) ?? Access.Modify; 
        }

        static Access GetBaseAccess(RoleDN role, PropertyRoute propertyPath)
        {
            return role.Roles.Count == 0 ? Access.Modify :
                  role.Roles.Select(r => GetAccess(r, propertyPath)).MaxAccess();
        }

        public static Access GetPropertyAccess(PropertyRoute propertyPath)
        {
            return GetAccess(RoleDN.Current, propertyPath);
        }

        public static List<AccessRule> GetAccessRule(Lite<RoleDN> roleLite, TypeDN typeDN)
        {
            var role = roleLite.Retrieve(); 

            Type type = TypeLogic.DnToType[typeDN]; 
            List<PropertyDN> properties = PropertyLogic.RetrieveOrGenerateProperty(typeDN);
            return properties.Select(p => new AccessRule(GetBaseAccess(role, p.PropertyPath))
                    {
                        Resource = p,
                        Access = GetAccess(role, p.PropertyPath),
                    }).ToList();
        }

        public static void SetAccessRule(List<AccessRule> rules, Lite<RoleDN> roleLite, TypeDN typeDN)
        {
            var role = roleLite.Retrieve(); 

            var current = Database.Query<RulePropertyDN>().Where(r => r.Role == role && r.Property.Type == typeDN).ToDictionary(a => a.Property.Path);
            var should = rules.Where(a => a.Overriden).ToDictionary(r => ((PropertyDN)r.Resource).Path);

            Synchronizer.Synchronize(current, should,
                (p, pr) => pr.Delete(),
                (p, ar) => new RulePropertyDN { Property = ((PropertyDN)ar.Resource), Role = role, Access = ar.Access}.Save(),
                (p, pr, ar) => { pr.Access = ar.Access; pr.Save(); });

            _runtimeRules = null;
        }

        public static Dictionary<Type, Dictionary<string,Access>> AuthorizedProperties()
        {
            return RuntimeRules.TryGetC(RoleDN.Current) ?? new Dictionary<Type, Dictionary<string, Access>>();
        }

        public static Dictionary<RoleDN, Dictionary<Type, Dictionary<string, Access>>> NewCache()
        {
            using (AuthLogic.Disable())
            using (new EntityCache(true))
            {
                List<RoleDN> roles = AuthLogic.RolesInOrder().ToList();

                Dictionary<RoleDN, Dictionary<Type, Dictionary<string, Access>>> realRules =
                    Database.RetrieveAll<RulePropertyDN>()
                      .AgGroupToDictionary(ru => ru.Role, gr => gr
                          .AgGroupToDictionary(ru => TypeLogic.DnToType[ru.Property.Type], gr2 => gr2
                              .ToDictionary(ru => ru.Property.Path, ru => ru.Access)));

                Dictionary<RoleDN, Dictionary<Type, Dictionary<string, Access>>> newRules = new Dictionary<RoleDN, Dictionary<Type, Dictionary<string, Access>>>();
                foreach (var role in roles)
                {
                    var permissions = role.Roles.Count == 0 ? 
                         null :
                         role.Roles.Select(r => newRules.TryGetC(r)).OuterCollapseDictionariesC(
                            dicts => dicts.OuterCollapseDictionariesS(
                                access => access.MaxAccess())
                            .Simplify(a => a == Access.Modify));

                    permissions = permissions.OverrideJoin(realRules.TryGetC(role),
                        (t, dc, dr) => dc.Override(dr)).Simplify(d => d == null);

                    if (permissions != null)
                        newRules.Add(role, permissions); 
                }

                return newRules;
            }
        }
     }
}
