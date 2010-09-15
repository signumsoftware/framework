using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using System.Reflection;
using Signum.Utilities;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Entities;

namespace Signum.Engine.Basics
{
    public static class FacadeMethodLogic
    {
        static Type serviceInterface;
        public static void Start(SchemaBuilder sb, Type serviceInterface)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                FacadeMethodLogic.serviceInterface = serviceInterface;
                sb.Include<FacadeMethodDN>();

                sb.Schema.Synchronizing += SyncronizeServiceOperations;
            }
        }

        public static bool IsEnabled
        {
            get { return serviceInterface != null; }
        }

        public static List<FacadeMethodDN> RetrieveOrGenerateServiceOperations()
        {
            var current = Database.RetrieveAll<FacadeMethodDN>().ToDictionary(a => a.Name);
            var total = GenerateFacadeMethods().ToDictionary(a => a.Name);

            total.SetRange(current);
            return total.Values.ToList();
        }

        static List<FacadeMethodDN> GenerateFacadeMethods()
        {
            return GenerateServiceMethodInfo().Select(mi => new FacadeMethodDN { Name = mi.Name }).ToList();
        }

        public static MethodInfo GenerateServiceMethodInfo(string miName)
        {
            return serviceInterface.GetInterfaces().SelectMany(i => i.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)).Single(m => m.Name == miName);
        }

        public static List<MethodInfo> GenerateServiceMethodInfo()
        {
            return serviceInterface.GetInterfaces().SelectMany(i => i.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)).ToList();
        }

        const string FacadeMethodKey = "FacadeMethod";

        public static SqlPreCommand SyncronizeServiceOperations(Replacements replacements)
        {
            var should = GenerateFacadeMethods();

            var current = Administrator.TryRetrieveAll<FacadeMethodDN>(replacements);

            Table table = Schema.Current.Table<FacadeMethodDN>();

            return Synchronizer.SynchronizeReplacing(replacements, FacadeMethodKey,
                current.ToDictionary(a => a.Name),
                should.ToDictionary(a => a.Name),
                (n, c) => table.DeleteSqlSync(c),
                null,
                (fn, c, s) =>
                {
                    c.Name = s.Name;
                    return table.UpdateSqlSync(c);
                }, Spacing.Double);
        }
    }
}
