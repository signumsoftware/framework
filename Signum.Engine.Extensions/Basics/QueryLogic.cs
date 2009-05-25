using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Utilities;

namespace Signum.Engine.Basics
{
    public static class QueryLogic
    {
        static DynamicQueryManager dynamicQueryManager;
        public static void Start(SchemaBuilder sb, DynamicQueryManager dynamicQueryManager)
        {
            if (sb.NotDefined<QueryDN>())
            {
                QueryLogic.dynamicQueryManager = dynamicQueryManager;
                sb.Include<QueryDN>();

                sb.Schema.Synchronizing += SynchronizeQueries;
            }
        }

        public static List<QueryDN> RetrieveOrGenerateQueries()
        {
            var current = Database.RetrieveAll<QueryDN>().ToDictionary(a => a.Name);
            var total = GenerateQueries().ToDictionary(a => a.Name);

            total.SetRange(current);
            return total.Values.ToList();
        }

        static List<QueryDN> GenerateQueries()
        {
            return dynamicQueryManager.GetQueryNames().Select(o => new QueryDN { Name = o.ToString() }).ToList();
        }

        const string QueriesKey = "Queries";

        static SqlPreCommand SynchronizeQueries(Replacements replacements)
        {
            var should = GenerateQueries();

            var current = Administrator.TryRetrieveAll<QueryDN>(replacements);

            Table table = Schema.Current.Table<QueryDN>();

            return Synchronizer.SyncronizeReplacing(replacements, QueriesKey,
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
