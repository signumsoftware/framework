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

            var current = Database.RetrieveAll<QueryDN>().Where(a => queryNames.Contains(a.Key)).ToDictionary(a => a.Key);
            var total = DynamicQueryManager.Current.GetQueryNames(type).Keys.Select(qn => CreateQuery(qn)).ToDictionary(a => a.Key);

            total.SetRange(current);
            return total.Values.ToList();
        }

        public static QueryDN RetrieveOrGenerateQuery(object queryName)
        {
            return Database.RetrieveAll<QueryDN>().SingleOrDefault(a => a.Key == QueryUtils.GetQueryName(queryName)) ??
                CreateQuery(queryName);
        }

        static QueryDN CreateQuery(object queryName)
        {
            return new QueryDN { 
                Key = QueryUtils.GetQueryName(queryName), 
                DisplayName = QueryUtils.GetNiceQueryName(queryName) };
        }

        const string QueriesKey = "Queries";

        static SqlPreCommand SynchronizeQueries(Replacements replacements)
        {
            var should = DynamicQueryManager.Current.GetQueryNames().Select(qn => CreateQuery(qn));

            var current = Administrator.TryRetrieveAll<QueryDN>(replacements);

            Table table = Schema.Current.Table<QueryDN>();

            return Synchronizer.SynchronizeReplacing(replacements, QueriesKey,
                current.ToDictionary(a => a.Key),
                should.ToDictionary(a => a.Key),
                (n, c) => table.DeleteSqlSync(c),
                 null,
                 (fn, c, s) =>
                 {
                     c.Key = s.Key;
                     c.DisplayName = s.DisplayName;
                     return table.UpdateSqlSync(c);
                 }, Spacing.Double);
        }
    }
}
