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

        static IEnumerable<QueryEntity> GenerateQueries()
        {
            return DynamicQueryManager.Current.GetQueryNames()
                .Select(qn => new QueryEntity
                {
                    Key = QueryUtils.GetQueryUniqueKey(qn),
                    Name = QueryUtils.GetCleanName(qn)
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
                        c.Name = s.Name;
                        return table.UpdateSqlSync(c);
                    }, Spacing.Double);
        }

        public static QueryEntity GetQueryEntity(object queryName)
        {
            return QueryNameToEntity.GetOrThrow(queryName, "QueryName {0} not found on the database"); 
        }
    }
}
