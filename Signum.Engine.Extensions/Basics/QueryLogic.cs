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
using Signum.Entities.UserQueries;

namespace Signum.Engine.Basics
{
    public static class QueryLogic
    {
        static ResetLazy<Dictionary<string, object>> queryNamesLazy;
        public static Dictionary<string, object> QueryNames 
        { 
            get { return queryNamesLazy.Value; } 
        }

        static ResetLazy<Dictionary<object, QueryDN>> queryNameToEntityLazy;
        public static Dictionary<object, QueryDN> QueryNameToEntity 
        {
            get { return queryNameToEntityLazy.Value; }
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
                sb.Schema.Initializing += () =>
                {
                    queryNamesLazy.Load();

                    queryNameToEntityLazy.Load();
                };

                sb.Include<QueryDN>();

                sb.Schema.Synchronizing += SynchronizeQueries;
                sb.Schema.Generating += Schema_Generating;

                queryNamesLazy = sb.GlobalLazy(()=>CreateQueryNames(), new InvalidateWith(typeof(QueryDN)));

                queryNameToEntityLazy = sb.GlobalLazy(() => 
                    EnumerableExtensions.JoinStrict(
                        Database.Query<QueryDN>().ToList(),
                        QueryNames,
                        q => q.Key,
                        kvp => kvp.Key,
                        (q, kvp) => KVP.Create(kvp.Value, q),
                        "caching QueryDN. Consider synchronize").ToDictionary(),
                    new InvalidateWith(typeof(QueryDN)));
            }
        }


        public static object ToQueryName(this QueryDN query)
        {
            return QueryNames.GetOrThrow(query.Key, "QueryName with unique name {0} not found");
        }

        public static object ToQueryName(string uniqueQueryName)
        {
            return QueryNames.GetOrThrow(uniqueQueryName, "QueryName with unique name {0} not found");
        }

        public static object TryToQueryName(string uniqueQueryName)
        {
            return QueryNames.TryGetC(uniqueQueryName);
        }

        private static Dictionary<string, object> CreateQueryNames()
        {
            return DynamicQueryManager.Current.GetQueryNames().ToDictionary(qn => QueryUtils.GetQueryUniqueKey(qn), "queryName");
        }

        static IEnumerable<QueryDN> GenerateQueries()
        {
            return DynamicQueryManager.Current.GetQueryNames()
                .Select(qn => new QueryDN
                {
                    Key = QueryUtils.GetQueryUniqueKey(qn),
                    Name = QueryUtils.GetCleanName(qn)
                });
        }

        public static List<QueryDN> GetTypeQueries(TypeDN typeDN)
        {
            Type type = TypeLogic.GetType(typeDN.CleanName);

            return DynamicQueryManager.Current.GetTypeQueries(type).Keys.Select(GetQuery).ToList();
        }


        const string QueriesKey = "Queries";

        static SqlPreCommand Schema_Generating()
        {
            Table table = Schema.Current.Table<QueryDN>();

            var should = GenerateQueries();

            return should.Select((q, i) => table.InsertSqlSync(q, suffix: i.ToString())).Combine(Spacing.Simple).ToSimple();

        }

        static SqlPreCommand SynchronizeQueries(Replacements replacements)
        {
            var should = GenerateQueries();

            var current = Administrator.TryRetrieveAll<QueryDN>(replacements);

            Table table = Schema.Current.Table<QueryDN>();

            return Synchronizer.SynchronizeScriptReplacing(
                replacements,
                QueriesKey,
                should.ToDictionary(a => a.Key, "query in memory"),
                current.ToDictionary(a => a.Key, "query in database"),
                (n, s)=>table.InsertSqlSync(s),
                (n, c) => table.DeleteSqlSync(c),
                (fn, s, c) =>
                {
                    c.Key = s.Key;
                    c.Name = s.Name;
                    return table.UpdateSqlSync(c);
                }, Spacing.Double);
        }

        public static QueryDN GetQuery(object queryName)
        {
            return QueryNameToEntity.GetOrThrow(queryName, "QueryName {0} not found on the database"); 
        }
    }
}
