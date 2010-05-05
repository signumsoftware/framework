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
using Signum.Entities.Operations;
using Signum.Engine.Operations;
using System.Reflection;
using Signum.Engine.Extensions.Properties;

namespace Signum.Engine.Authorization
{
    public static class OperationAuthLogic
    {
        static AuthCache<RuleOperationDN, OperationAllowedRule, OperationDN, Enum, bool> cache; 

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertIsStarted(sb);
                OperationLogic.AssertIsStarted(sb);

                OperationLogic.AllowOperation += new AllowOperationHandler(OperationLogic_AllowOperation);

                cache = new AuthCache<RuleOperationDN, OperationAllowedRule, OperationDN, Enum, bool>(sb,
                     EnumLogic<OperationDN>.ToEnum,
                     EnumLogic<OperationDN>.ToEntity,
                     AuthUtils.MaxAllowed, true);
            }
        }

        static bool OperationLogic_AllowOperation(Enum operationKey)
        {
            return cache.GetAllowed(RoleDN.Current.ToLite(), operationKey);
        }

        public static List<OperationInfo> Filter(List<OperationInfo> operationInfos)
        {
            RoleDN role = RoleDN.Current;

            return operationInfos.Where(ai => cache.GetAllowed(role.ToLite(), ai.Key)).ToList(); 
        }

        public static OperationRulePack GetOperationRules(Lite<RoleDN> roleLite, TypeDN typeDN)
        {

            var resources = OperationLogic.GetAllOperationInfos(TypeLogic.DnToType[typeDN]).Select(a => EnumLogic<OperationDN>.ToEntity(a.Key));
            return new OperationRulePack
            {
                Role = roleLite,
                Type = typeDN,
                Rules = cache.GetRules(roleLite, resources).ToMList()
            };
        }

        public static void SetOperationRules(OperationRulePack rules)
        {
            var keys = OperationLogic.GetAllOperationInfos(TypeLogic.DnToType[rules.Type])
                .Select(a => OperationDN.UniqueKey(a.Key)).ToArray();

            cache.SetRules(rules, r => keys.Contains(r.Key));
        }

        public static void SetOperationAllowed(Lite<RoleDN> role, Enum operationKey, bool allowed)
        {
            cache.SetAllowed(role, operationKey, allowed);
        }


        public static bool GetOperationAllowed(Enum operationKey)
        {
            return cache.GetAllowed(RoleDN.Current.ToLite(), operationKey);
        }
    }
}
