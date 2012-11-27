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
using Signum.Utilities.Reflection;
using Signum.Entities.Reports;
using Signum.Entities.UserQueries;

namespace Signum.Engine.Basics
{
    public static class QueryLogic
    {
        static Dictionary<string, object> queryNames;
        public static Dictionary<string, object> QueryNames
        {
            get { return queryNames ?? (queryNames = CreateQueryNames()); }
            private set { queryNames = value; }
        }

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(sb)));
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                // QueryManagers = queryManagers;
                sb.Schema.Initializing[InitLevel.Level0SyncEntities] += Schema_Initializing;

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

        static void Schema_Initializing()
        {
            QueryNames = CreateQueryNames();
        }

        private static Dictionary<string, object> CreateQueryNames()
        {
            return DynamicQueryManager.Current.GetQueryNames().ToDictionary(qn => QueryUtils.GetQueryUniqueKey(qn));
        }

        public static Dictionary<string, QueryDN> RetrieveOrGenerateQueries()
        {
            var current = Database.RetrieveAll<QueryDN>().ToDictionary(a => a.Key);
            var total = DynamicQueryManager.Current.GetQueries().Keys.ToDictionary(qn => QueryUtils.GetQueryUniqueKey(qn), qn => CreateQuery(qn));

            total.SetRange(current);
            return total;
        }

        public static List<QueryDN> RetrieveOrGenerateQueries(TypeDN typeDN)
        {
            Type type = TypeLogic.DnToType[typeDN];

            string[] queryNames = DynamicQueryManager.Current.GetQueries(type).Keys.Select(qn => QueryUtils.GetQueryUniqueKey(qn)).ToArray();

            var current = Database.RetrieveAll<QueryDN>().Where(a => queryNames.Contains(a.Key)).ToDictionary(a => a.Key);
            var total = DynamicQueryManager.Current.GetQueries(type).Keys.Select(qn => CreateQuery(qn)).ToDictionary(a => a.Key);

            total.SetRange(current);
            return total.Values.ToList();
        }

        public static QueryDN RetrieveOrGenerateQuery(object queryName)
        {
            return Database.Query<QueryDN>().SingleOrDefaultEx(a => a.Key == QueryUtils.GetQueryUniqueKey(queryName)) ??
                CreateQuery(queryName);
        }

        static QueryDN CreateQuery(object queryName)
        {
            using (Sync.ChangeCulture(Schema.Current.ForceCultureInfo))
            {
                return new QueryDN
                {
                    Key = QueryUtils.GetQueryUniqueKey(queryName),
                    Name = QueryUtils.GetCleanName(queryName)
                };
            }
        }

        const string QueriesKey = "Queries";

        static SqlPreCommand SynchronizeQueries(Replacements replacements)
        {
            var should = DynamicQueryManager.Current.GetQueryNames().Select(qn => CreateQuery(qn));

            var current = Administrator.TryRetrieveAll<QueryDN>(replacements);

            Table table = Schema.Current.Table<QueryDN>();

            return Synchronizer.SynchronizeScriptReplacing(
                replacements,
                QueriesKey,
                should.ToDictionary(a => a.Key),
                current.ToDictionary(a => a.Key),
                null,
                (n, c) => table.DeleteSqlSync(c),
                (fn, s, c) =>
                {
                    c.Key = s.Key;
                    c.Name = s.Name;
                    return table.UpdateSqlSync(c);
                }, Spacing.Double);
        }
    }
}
