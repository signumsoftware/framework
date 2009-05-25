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
    public static class ServiceOperationLogic
    {
        static Type serviceInterface;
        public static void Start(SchemaBuilder sb, Type serviceInterface)
        {
            if (sb.NotDefined<ServiceOperationDN>())
            {
                ServiceOperationLogic.serviceInterface = serviceInterface;
                sb.Include<ServiceOperationDN>();

                sb.Schema.Synchronizing += SyncronizeServiceOperations;
            }
        }

        public static List<ServiceOperationDN> RetrieveOrGenerateServiceOperations()
        {
            var current = Database.RetrieveAll<ServiceOperationDN>().ToDictionary(a => a.Name);
            var total = GenerateServiceOperations().ToDictionary(a => a.Name);

            total.SetRange(current);
            return total.Values.ToList();
        }

        static List<ServiceOperationDN> GenerateServiceOperations()
        {
            return serviceInterface.GetInterfaces().SelectMany(i => i.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy))
                .Select(mi => new ServiceOperationDN { Name = mi.Name }).ToList();
        }

        const string ServiceOperationsKey = "ServiceOperations";

        public static SqlPreCommand SyncronizeServiceOperations(Replacements replacements)
        {
            var should = GenerateServiceOperations();

            var current = Administrator.TryRetrieveAll<ServiceOperationDN>(replacements);

            Table table = Schema.Current.Table<ServiceOperationDN>();

            return Synchronizer.SyncronizeReplacing(replacements, ServiceOperationsKey,
                current.ToDictionary(a => a.Name),
                should.ToDictionary(a => a.Name),
                (n, c) => table.DeleteSqlSync(c.Id),
                null,
                (fn, c, s) =>
                {
                    c.Name = s.Name;
                    return table.UpdateSqlSync(c);
                }, Spacing.Double);
        }
    }
}
