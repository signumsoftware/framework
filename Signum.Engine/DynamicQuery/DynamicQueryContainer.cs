using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Data;
using System.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using System.Diagnostics;
using Signum.Services;
using Signum.Engine.Linq;
using Signum.Entities.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System.Collections.Concurrent;
using System.Threading;
using static Signum.Engine.Maps.SchemaBuilder;
using System.Threading.Tasks;

namespace Signum.Engine.DynamicQuery
{
    public class DynamicQueryContainer
    {
        Dictionary<object, DynamicQueryBucket> queries = new Dictionary<object, DynamicQueryBucket>();

        public void Register<T>(object queryName, Func<DynamicQueryCore<T>> lazyQueryCore, Implementations? entityImplementations = null)
        {
            queries[queryName] = new DynamicQueryBucket(queryName, lazyQueryCore, entityImplementations ?? DefaultImplementations(typeof(T), queryName));
        }

        public void Register<T>(object queryName, Func<IQueryable<T>> lazyQuery, Implementations? entityImplementations = null)
        {
            queries[queryName] = new DynamicQueryBucket(queryName, () => DynamicQueryCore.Auto(lazyQuery()), entityImplementations ?? DefaultImplementations(typeof(T), queryName));
        }

        public void Register(object queryName, DynamicQueryBucket bucket)
        {
            queries[queryName] = bucket;
        }

        public DynamicQueryBucket TryGetQuery(object queryName)
        {
            AssertQueryAllowed(queryName, false);
            return queries.TryGetC(queryName);
        }

        public DynamicQueryBucket GetQuery(object queryName)
        {
            AssertQueryAllowed(queryName, false);
            return queries.GetOrThrow(queryName);
        }

        public Implementations GetEntityImplementations(object queryName)
        {
            //AssertQueryAllowed(queryName);
            return queries.GetOrThrow(queryName).EntityImplementations;
        }


        static Implementations DefaultImplementations(Type type, object queryName)
        {
            var property = type.GetProperty("Entity", BindingFlags.Instance | BindingFlags.Public);

            if (property == null)
                throw new InvalidOperationException("Entity property not found on query {0}".FormatWith(QueryUtils.GetKey(queryName)));

            return Implementations.By(property.PropertyType.CleanType());
        }

        T Execute<T>(ExecuteType executeType, object queryName, BaseQueryRequest request, Func<DynamicQueryBucket, T> executor)
        {
            using (ExecutionMode.UserInterface())
            using (HeavyProfiler.Log(executeType.ToString(), () => QueryUtils.GetKey(queryName)))
            {
                try
                {
                    var qb = GetQuery(queryName);

                    using (Disposable.Combine(QueryExecuted, f => f(executeType, queryName, request)))
                    {
                        return executor(qb);
                    }
                }
                catch (Exception e)
                {
                    e.Data["QueryName"] = queryName;
                    throw;
                }
            }
        }

        async Task<T> ExecuteAsync<T>(ExecuteType executeType, object queryName, BaseQueryRequest request, Func<DynamicQueryBucket, Task<T>> executor)
        {
            using (ExecutionMode.UserInterface())
            using (HeavyProfiler.Log(executeType.ToString(), () => QueryUtils.GetKey(queryName)))
            {
                try
                {
                    var qb = GetQuery(queryName);

                    using (Disposable.Combine(QueryExecuted, f => f(executeType, queryName, request)))
                    {
                        return await executor(qb);
                    }
                }
                catch (Exception e)
                {
                    e.Data["QueryName"] = queryName;
                    throw;
                }
            }
        }

        public event Func<ExecuteType, object, BaseQueryRequest, IDisposable> QueryExecuted;

        public enum ExecuteType
        {
            ExecuteQuery,
            ExecuteQueryValue,
            ExecuteGroupQuery,
            ExecuteUniqueEntity,
            QueryDescription,
            GetEntities,
            GetDQueryable
        }

        public ResultTable ExecuteQuery(QueryRequest request)
        {
            if (!request.GroupResults)
                return Execute(ExecuteType.ExecuteQuery, request.QueryName, request, dqb => dqb.Core.Value.ExecuteQuery(request));
            else
                return Execute(ExecuteType.ExecuteGroupQuery, request.QueryName, request, dqb => dqb.Core.Value.ExecuteQueryGroup(request));
        }

        public Task<ResultTable> ExecuteQueryAsync(QueryRequest request, CancellationToken token)
        {
            if (!request.GroupResults)
                return ExecuteAsync(ExecuteType.ExecuteQuery, request.QueryName, request, dqb => dqb.Core.Value.ExecuteQueryAsync(request, token));
            else
                return ExecuteAsync(ExecuteType.ExecuteGroupQuery, request.QueryName, request, dqb => dqb.Core.Value.ExecuteQueryGroupAsync(request, token));
        }

        public object ExecuteQueryValue(QueryValueRequest request)
        {
            return Execute(ExecuteType.ExecuteQueryValue, request.QueryName, request, dqb => dqb.Core.Value.ExecuteQueryValue(request));
        }

        public Task<object> ExecuteQueryValueAsync(QueryValueRequest request, CancellationToken token)
        {
            return ExecuteAsync(ExecuteType.ExecuteQueryValue, request.QueryName, request, dqb => dqb.Core.Value.ExecuteQueryValueAsync(request, token));
        }

        public Lite<Entity> ExecuteUniqueEntity(UniqueEntityRequest request)
        {
            return Execute(ExecuteType.ExecuteUniqueEntity, request.QueryName, request, dqb => dqb.Core.Value.ExecuteUniqueEntity(request));
        }

        public Task<Lite<Entity>> ExecuteUniqueEntityAsync(UniqueEntityRequest request, CancellationToken token)
        {
            return ExecuteAsync(ExecuteType.ExecuteUniqueEntity, request.QueryName, request, dqb => dqb.Core.Value.ExecuteUniqueEntityAsync(request, token));
        }

        public QueryDescription QueryDescription(object queryName)
        {
            return Execute(ExecuteType.QueryDescription, queryName, null, dqb => dqb.GetDescription());
        }

        public IQueryable<Lite<Entity>> GetEntities(QueryEntitiesRequest request)
        {
            return Execute(ExecuteType.GetEntities, request.QueryName, null, dqb => dqb.Core.Value.GetEntities(request));
        }

        public DQueryable<object> GetDQueryable(DQueryableRequest request)
        {
            return Execute(ExecuteType.GetDQueryable, request.QueryName, null, dqb => dqb.Core.Value.GetDQueryable(request));
        }

        public event Func<object, bool, bool> AllowQuery;

        public bool QueryAllowed(object queryName, bool fullScreen)
        {
            foreach (var f in AllowQuery.GetInvocationListTyped())
            {
                if (!f(queryName, fullScreen))
                    return false;
            }

            return true;
        }

        public bool QueryDefined(object queryName)
        {
            return this.queries.ContainsKey(queryName);
        }

        public bool QueryDefinedAndAllowed(object queryName, bool fullScreen)
        {
            return QueryDefined(queryName) && QueryAllowed(queryName, fullScreen);
        }

        public void AssertQueryAllowed(object queryName, bool fullScreen)
        {
            if (!QueryAllowed(queryName, fullScreen))
                throw new UnauthorizedAccessException("Access to query {0} not allowed {1}".FormatWith(queryName, QueryAllowed(queryName, false) ? " for full screen" : ""));
        }

        public List<object> GetAllowedQueryNames(bool fullScreen)
        {
            return queries.Keys.Where(qn => QueryAllowed(qn, fullScreen)).ToList();
        }

        public Dictionary<object, DynamicQueryBucket> GetTypeQueries(Type entityType)
        {
            return (from kvp in queries
                    where !kvp.Value.EntityImplementations.IsByAll && kvp.Value.EntityImplementations.Types.Contains(entityType)
                    select kvp).ToDictionary();
        }

        public List<object> GetQueryNames()
        {
            return queries.Keys.ToList();
        }

        public Task<object[]> BatchExecute(BaseQueryRequest[] requests, CancellationToken token)
        {
            return Task.WhenAll<object>(requests.Select<BaseQueryRequest, Task<object>>(r =>
            {
                if (r is QueryValueRequest)
                    return ExecuteQueryValueAsync((QueryValueRequest)r, token);

                if (r is QueryRequest)
                    return ExecuteQueryAsync((QueryRequest)r, token).ContinueWith(a => (object)a.Result);

                if (r is UniqueEntityRequest)
                    return ExecuteUniqueEntityAsync((UniqueEntityRequest)r, token).ContinueWith(a => (object)a.Result);

                throw new InvalidOperationException("Unexpected QueryRequest type"); ;
            }));
        }
    }
}
