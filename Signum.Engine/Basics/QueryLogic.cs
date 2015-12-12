using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Utilities;
using System.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Basics
{
    public static class QueryLogic
    {
        static ResetLazy<Dictionary<string, object>> queryNamesLazy;
        public static Dictionary<string, object> QueryNames 
        { 
            get { return queryNamesLazy.Value; } 
        }

        static ResetLazy<Dictionary<object, QueryEntity>> queryNameToEntityLazy;
        public static Dictionary<object, QueryEntity> QueryNameToEntity 
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

                sb.Include<QueryEntity>();

                sb.Schema.Synchronizing += SynchronizeQueries;
                sb.Schema.Generating += Schema_Generating;

                queryNamesLazy = sb.GlobalLazy(()=>CreateQueryNames(), new InvalidateWith(typeof(QueryEntity)));

                queryNameToEntityLazy = sb.GlobalLazy(() => 
                    EnumerableExtensions.JoinStrict(
                        Database.Query<QueryEntity>().ToList(),
                        QueryNames,
                        q => q.Key,
                        kvp => kvp.Key,
                        (q, kvp) => KVP.Create(kvp.Value, q),
                        "caching QueryEntity. Consider synchronize").ToDictionary(),
                    new InvalidateWith(typeof(QueryEntity)));
            }
        }


        public static object ToQueryName(this QueryEntity query)
        {
            return QueryNames.GetOrThrow(query.Key, "QueryName with key {0} not found");
        }

        public static object ToQueryName(string queryKey)
        {
            return QueryNames.GetOrThrow(queryKey, "QueryName with unique name {0} not found");
        }

        public static object TryToQueryName(string queryKey)
        {
            return QueryNames.TryGetC(queryKey);
        }

        private static Dictionary<string, object> CreateQueryNames()
        {
            return DynamicQueryManager.Current.GetQueryNames().ToDictionary(qn => QueryUtils.GetKey(qn), "queryName");
        }

        static IEnumerable<QueryEntity> GenerateQueries()
        {
            return DynamicQueryManager.Current.GetQueryNames()
                .Select(qn => new QueryEntity
                {
                    Key = QueryUtils.GetKey(qn)
                });
        }

        public static List<QueryEntity> GetTypeQueries(TypeEntity typeEntity)
        {
            Type type = TypeLogic.GetType(typeEntity.CleanName);

            return DynamicQueryManager.Current.GetTypeQueries(type).Keys.Select(GetQueryEntity).ToList();
        }


        public const string QueriesKey = "Queries";

        static SqlPreCommand Schema_Generating()
        {
            Table table = Schema.Current.Table<QueryEntity>();

            var should = GenerateQueries();

            return should.Select((q, i) => table.InsertSqlSync(q, suffix: i.ToString())).Combine(Spacing.Simple).PlainSqlCommand();

        }

        static SqlPreCommand SynchronizeQueries(Replacements replacements)
        {
            var should = GenerateQueries();

            var current = Administrator.TryRetrieveAll<QueryEntity>(replacements);

            Table table = Schema.Current.Table<QueryEntity>();

            using (replacements.WithReplacedDatabaseName())
                return Synchronizer.SynchronizeScriptReplacing(
                    replacements,
                    QueriesKey,
                    should.ToDictionary(a => a.Key, "query in memory"),
                    current.ToDictionary(a => a.Key, "query in database"),
                    (n, s) => table.InsertSqlSync(s),
                    (n, c) => table.DeleteSqlSync(c),
                    (fn, s, c) =>
                    {
                        c.Key = s.Key;
                        return table.UpdateSqlSync(c);
                    }, Spacing.Double);
        }

        public static QueryEntity GetQueryEntity(object queryName)
        {
            return QueryNameToEntity.GetOrThrow(queryName, "QueryName {0} not found on the database"); 
        }
    }
}
