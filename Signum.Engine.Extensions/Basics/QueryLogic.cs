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
            return QueryNames.GetOrThrow(uniqueQueryName, "No query with name '{0}' found".Formato(uniqueQueryName));
        }

        static void Schema_Initializing(Schema sender)
        {
            QueryNames = DynamicQueryManager.Current.GetQueryNames().ToDictionary(qn => QueryUtils.GetQueryName(qn));
        }

        public static List<QueryDN> RetrieveOrGenerateQueries()
        {
            var current = Database.RetrieveAll<QueryDN>().ToDictionary(a => a.Name);
            var total = QueryNames.Select(kvp => new QueryDN { Name = kvp.Key }).ToDictionary(a => a.Name);

            total.SetRange(current);
            return total.Values.ToList();
        }

        const string QueriesKey = "Queries";

        static SqlPreCommand SynchronizeQueries(Replacements replacements)
        {
            var should = QueryNames.Select(kvp => new QueryDN { Name = kvp.Key });

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
