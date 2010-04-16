using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using System.Reflection;
using Signum.Utilities;
using Signum.Entities;
using Signum.Services;
using Signum.Engine.Extensions.Properties;

namespace Signum.Engine.Authorization
{
    public static class FacadeMethodAuthLogic
    {
        static AuthCache<RuleFacadeMethodDN, FacadeMethodDN, string, bool> cache; 

        public static void Start(SchemaBuilder sb, Type serviceInterface)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertIsStarted(sb);
                FacadeMethodLogic.Start(sb, serviceInterface);

                cache = new AuthCache<RuleFacadeMethodDN, FacadeMethodDN, string, bool>(sb,
                     fm => fm.Name,
                     n => FacadeMethodLogic.RetrieveOrGenerateServiceOperations().Single(fm => fm.Name == n),
                     AuthUtils.MaxAllowed, true); 
            }
        }

        public static FacadeMethodRulePack GetFacadeMethodRules(Lite<RoleDN> roleLite)
        {
            return new FacadeMethodRulePack
            {
                 Role = roleLite,
                 Rules = cache.GetRules(roleLite, FacadeMethodLogic.RetrieveOrGenerateServiceOperations()).ToMList()
            };
        }

        public static void SetFacadeMethodRules(FacadeMethodRulePack rules)
        {
            cache.SetRules(rules, r => true);
        }

        public static void SetFacadeMethodAllowed(Lite<RoleDN> role, MethodInfo mi, bool allowed)
        {
            cache.SetAllowed(role, mi.Name, allowed);
        }

        public static void AuthorizeAccess(MethodInfo mi)
        {
            if (!cache.GetAllowed(RoleDN.Current, mi.Name))
                throw new UnauthorizedAccessException(Resources.AccessToFacadeMethod0IsNotAllowed.Formato(mi.Name));
        }
    }
}
