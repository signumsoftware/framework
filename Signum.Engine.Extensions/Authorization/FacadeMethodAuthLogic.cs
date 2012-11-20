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
        static AuthCache<RuleFacadeMethodDN, FacadeMethodAllowedRule, FacadeMethodDN, string, bool> cache;

        public static IManualAuth<string, bool> Manual { get { return cache; } }

        public static bool IsStarted { get { return cache != null; } }

        public static void Start(SchemaBuilder sb, params Type[] serviceInterface)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);
                FacadeMethodLogic.Start(sb, serviceInterface);

                cache = new AuthCache<RuleFacadeMethodDN, FacadeMethodAllowedRule, FacadeMethodDN, string, bool>(sb,
                     fm => fm.ToString(),
                     n => FacadeMethodLogic.RetrieveOrGenerateFacadeMethod(n),
                     AuthUtils.MaxBool,
                     AuthUtils.MinBool);


                AuthLogic.ExportToXml += () => cache.ExportXml("FacadeMethods", "FacadeMethod", fm => fm.ToString(), b => b.ToString());
                AuthLogic.ImportFromXml += (x, roles) =>
                    {
                        var methods = FacadeMethodLogic.RetrieveOrGenerateFacadeMethods().ToDictionary(a => a.ToString());
                        return cache.ImportXml(x, "FacadeMethods", "FacadeMethod", roles, str =>
                        {
                            var m = methods.GetOrThrow(str);
                            if (m.IsNew)
                                m.Save();
                            return m;
                        }, bool.Parse);
                    };
            }
        }

        public static FacadeMethodRulePack GetFacadeMethodRules(Lite<RoleDN> roleLite)
        {
            FacadeMethodRulePack result = new FacadeMethodRulePack { Role = roleLite };
            cache.GetRules(result, FacadeMethodLogic.RetrieveOrGenerateFacadeMethods().OrderBy(a => a.InterfaceName).ThenBy(a => a.MethodName));
            return result; 
        }

        public static void SetFacadeMethodRules(FacadeMethodRulePack rules)
        {
            cache.SetRules(rules, r => true);
        }

        public static bool GetFacadeMethodAllowed(Lite<RoleDN> role, MethodInfo mi)
        {
            return cache.GetAllowed(role, FacadeMethodLogic.Normalize(mi).Key());
        }

        public static bool GetFacadeMethodAllowed(MethodInfo mi)
        {
            if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
                return true;

            return cache.GetAllowed(RoleDN.Current.ToLite(), FacadeMethodLogic.Normalize(mi).Key());
        }

        public static void AuthorizeAccess(MethodInfo mi)
        {
            if (!GetFacadeMethodAllowed(mi))
                throw new UnauthorizedAccessException(Resources.AccessToFacadeMethod0IsNotAllowed.Formato(mi.Name));
        }

        public static DefaultDictionary<string, bool> FacadeMethodRules()
        {
            return cache.GetDefaultDictionary();
        }
    }
}
