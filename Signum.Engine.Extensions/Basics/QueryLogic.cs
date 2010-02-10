using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Utilities;
using System.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Entities;

namespace Signum.Engine.Basics
{
    public static class QueryLogic
    {
        public static Dictionary<string, object> QueryNames { get; private set; }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {

               // QueryManagers = queryManagers;
                sb.Schema.Initializing(InitLevel.Level0SyncEntities, new InitEventHandler(Schema_Initializing));

                sb.Include<QueryDN>();

                sb.Schema.Synchronizing += SynchronizeQueries;
            }
        }

        public static object ToQueryName(string uniqueQueryName)
        {
            return QueryNames.GetOrThrow(uniqueQueryName, "QueryName with unique name {0} not found");
        }

        public static object TryToQueryName(string uniqueQueryName)
        {
            return QueryNames.TryGetC(uniqueQueryName);
        }

        static void Schema_Initializing(Schema sender)
        {
            QueryNames = CreateQueryNames();
        }

        private static Dictionary<string, object> CreateQueryNames()
        {
            return DynamicQueryManager.Current.GetQueryNames().ToDictionary(qn => QueryUtils.GetQueryName(qn));
        }

        public static List<QueryDN> RetrieveOrGenerateQueries(TypeDN typeDN)
        {
            Type type = TypeLogic.DnToType[typeDN];

            string[] queryNames = DynamicQueryManager.Current.GetQueryNames(type).Keys.Select(qn => QueryUtils.GetQueryName(qn)).ToArray();

            var current = Database.RetrieveAll<QueryDN>().Where(a => queryNames.Contains(a.Name)).ToDictionary(a => a.Name);
            var total = queryNames.Select(un => new QueryDN { Name = un }).ToDictionary(a => a.Name);

            total.SetRange(current);
            return total.Values.ToList();
        }

        const string QueriesKey = "Queries";

        static SqlPreCommand SynchronizeQueries(Replacements replacements)
        {
            var should = CreateQueryNames().Select(kvp => new QueryDN { Name = kvp.Key });

            var current = Administrator.TryRetrieveAll<QueryDN>(replacements);

            Table table = Schema.Current.Table<QueryDN>();

            return Synchronizer.SynchronizeReplacing(replacements, QueriesKey,
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
