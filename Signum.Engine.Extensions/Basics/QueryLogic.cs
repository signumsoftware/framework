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
        public static HashSet<object> QueryNames { get; private set; }

        static DynamicQueryManager[] QueryManagers;

        public static void Start(SchemaBuilder sb, params DynamicQueryManager[] queryManagers)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {

                QueryManagers = queryManagers;
                sb.Schema.Initializing(InitLevel.Level0SyncEntities, new InitEventHandler(Schema_Initializing));

                sb.Include<QueryDN>();

                sb.Schema.Synchronizing += SynchronizeQueries;
            }
        }

        static void Schema_Initializing(Schema sender)
        {
            QueryNames = QueryManagers.SelectMany(a => a.GetQueryNames()).ToHashSet();
        }

        public static List<QueryDN> RetrieveOrGenerateQueries()
        {
            var current = Database.RetrieveAll<QueryDN>().ToDictionary(a => a.Name);
            var total = QueryNames.Select(o => new QueryDN { Name = Signum.Entities.DynamicQuery.QueryUtils.GetQueryName(o) }).ToDictionary(a => a.Name);

            total.SetRange(current);
            return total.Values.ToList();
        }

        const string QueriesKey = "Queries";

        static SqlPreCommand SynchronizeQueries(Replacements replacements)
        {
            var should = QueryManagers.SelectMany(a => a.GetQueryNames()).Distinct()
                .Select(o => new QueryDN { Name = QueryUtils.GetQueryName(o) }).ToList();

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
