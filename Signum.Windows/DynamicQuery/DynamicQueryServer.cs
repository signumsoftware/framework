using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using System.Collections.Concurrent;
using System.Windows.Threading;
using Signum.Services;
using System.Threading.Tasks;
using System.Windows;
using Signum.Utilities;
using Signum.Windows.DynamicQuery;
using Signum.Entities;

namespace Signum.Windows
{
    public static class DynamicQueryServer
    {
        static ConcurrentDictionary<QueryToken, IEnumerable<QueryToken>> extensionCache = new ConcurrentDictionary<QueryToken, IEnumerable<QueryToken>>();

        public static IEnumerable<QueryToken> GetExtensionToken(QueryToken token)
        {
            var result = extensionCache.GetOrAdd(token, t => Server.Return((IDynamicQueryServer server) => server.ExternalQueryToken(t)));
            return result;
        }

        static ConcurrentDictionary<object, QueryDescription> descriptionsCache = new ConcurrentDictionary<object, QueryDescription>();
        public static QueryDescription GetQueryDescription(object queryName)
        {
            return descriptionsCache.GetOrAdd(queryName, qn => Server.Return((IDynamicQueryServer s) => s.GetQueryDescription(qn)));
        }

        #region Query
        public static ResultTable Query(this QueryOptions options)
        {
            return options.ToRequest().Query();
        }

        private static ResultTable Query(this QueryRequest request)
        {
            Finder.Manager.AssertFindable(request.QueryName);
            return Server.Return((IDynamicQueryServer s) => s.ExecuteQuery(request));
        }

        public static void QueryBatch(this QueryOptions options, Action<ResultTable> onResult, Action @finally)
        {
            options.ToRequest().QueryBatch(onResult, @finally);
        }

        public static void QueryBatch(this QueryRequest request, Action<ResultTable> onResult, Action @finally)
        {
            Finder.Manager.AssertFindable(request.QueryName);
            Enqueue(request, obj => onResult((ResultTable)obj), @finally);
        }

        public static QueryRequest ToRequest(this QueryOptions options)
        {
            QueryDescription qd = GetQueryDescription(options.QueryName);

            ColumnOption.SetColumnTokens(options.ColumnOptions, qd);
            FilterOption.SetFilterTokens(options.FilterOptions, qd);
            OrderOption.SetOrderTokens(options.OrderOptions, qd);

            var request = new QueryRequest
            {
                QueryName = options.QueryName,
                Filters = options.FilterOptions.Select(f => f.ToFilter()).ToList(),
                Orders = options.OrderOptions.Select(f => f.ToOrder()).ToList(),
                Columns = ColumnOption.MergeColumns(options.ColumnOptions, options.ColumnOptionsMode, qd),
                Pagination = options.Pagination ?? QueryOptions.DefaultPagination,
            };
            return request;
        }
        #endregion

        #region QueryUnique
        public static Lite<T> QueryUnique<T>(string columnName, object value, UniqueType uniqueType)where T : class, IEntity
        {
            return (Lite<T>)new UniqueOptions(typeof(T))
            {
                UniqueType = uniqueType,
                FilterOptions = new List<FilterOption>()
                {
                    new FilterOption(columnName, value)
                }
            }.ToRequest().QueryUnique();
        }

        public static Lite<T> QueryUnique<T>(this UniqueOptions options)
           where T : class, IEntity
        {
            if (options.QueryName == null)
                options.QueryName = typeof(T);

            return (Lite<T>)options.ToRequest().QueryUnique();
        }

        public static Lite<Entity> QueryUnique(this UniqueOptions options)
        {
            return options.ToRequest().QueryUnique();
        }

        private static Lite<Entity> QueryUnique(this UniqueEntityRequest request)
        {
            Finder.Manager.AssertFindable(request.QueryName);

            return Server.Return((IDynamicQueryServer s) => s.ExecuteUniqueEntity(request));
        }

        public static void QueryUniqueBatch(this UniqueOptions options, Action<Lite<Entity>> onResult, Action @finally)
        {
            options.ToRequest().QueryUniqueBatch(onResult, @finally);
        }

        private static void QueryUniqueBatch(this UniqueEntityRequest request, Action<Lite<Entity>> onResult, Action @finally)
        {
            Finder.Manager.AssertFindable(request.QueryName);
            Enqueue(request, obj => onResult((Lite<Entity>)obj), @finally);
        }

        public static UniqueEntityRequest ToRequest(this UniqueOptions options)
        {
            QueryDescription qd = GetQueryDescription(options.QueryName);

            FilterOption.SetFilterTokens(options.FilterOptions, qd);
            OrderOption.SetOrderTokens(options.OrderOptions, qd);

            var request = new UniqueEntityRequest
            {
                QueryName = options.QueryName,
                Filters = options.FilterOptions.Select(f => f.ToFilter()).ToList(),
                Orders = options.OrderOptions.Select(f => f.ToOrder()).ToList(),
                UniqueType = options.UniqueType,
            };
            return request;
        } 
        #endregion

        #region QueryCount
        public static int QueryCount(this QueryCountOptions options)
        {
            return options.ToRequest().QueryCount();
        }

        public static int QueryCount(this QueryValueRequest request)
        {
            Finder.Manager.AssertFindable(request.QueryName);
            return Server.Return((IDynamicQueryServer s) => s.ExecuteQueryCount(request));
        }

        public static void QueryCountBatch(QueryCountOptions options, Action<int> onResult, Action @finally)
        {
            options.ToRequest().QueryCountBatch(@onResult, @finally);
        }

        private static void QueryCountBatch(this QueryValueRequest request, Action<int> onResult, Action @finally)
        {
            Finder.Manager.AssertFindable(request.QueryName);
            Enqueue(request, obj => onResult((int)obj), @finally);
        }

        public static QueryValueRequest ToRequest(this QueryCountOptions options)
        {
            QueryDescription qd = GetQueryDescription(options.QueryName);

            FilterOption.SetFilterTokens(options.FilterOptions, qd);

            var request = new QueryValueRequest
            {
                QueryName = options.QueryName,
                Filters = options.FilterOptions.Select(f => f.ToFilter()).ToList()
            };
            return request;
        }
        #endregion

        #region Batch
        class RequestTuple
        {
            public BaseQueryRequest Request;
            public Action<object> OnResult;
            public Action Finally;

            public override string ToString()
            {
                return Request.ToString();
            }
        }

        static Dictionary<Dispatcher, List<RequestTuple>> tuples = new Dictionary<Dispatcher, List<RequestTuple>>();

        public static void Enqueue(BaseQueryRequest request, Action<object> onResult, Action @finally)
        {
            lock (tuples)
            {
                tuples.GetOrCreate(Dispatcher.CurrentDispatcher).Add(new RequestTuple
                {
                    Request = request,
                    OnResult = onResult,
                    Finally = @finally,
                });

                Dispatcher.CurrentDispatcher.Hooks.DispatcherInactive -= new EventHandler(Hooks_DispatcherInactive);
                Dispatcher.CurrentDispatcher.Hooks.DispatcherInactive += new EventHandler(Hooks_DispatcherInactive);
            }
        }

        static void Hooks_DispatcherInactive(object sender, EventArgs e)
        {
            lock (tuples)
            {
                var tup = tuples.Extract(Dispatcher.CurrentDispatcher, "Calling Hooks_DispatcherInactive with no tuples for {0}").ToList();
                Dispatcher.CurrentDispatcher.Hooks.DispatcherInactive -= new EventHandler(Hooks_DispatcherInactive);

                object[] results = null;
                Async.Do(() =>
                    {
                        results = Server.Return((IDynamicQueryServer dqs) => dqs.BatchExecute(tup.Select(a => a.Request).ToArray()));
                    },
                    () =>
                    {
                        foreach (var item in tup.Zip(results))
                        {
                            item.first.OnResult(item.second);
                        }
                    },
                    () =>
                    {
                        foreach (var item in tup)
                        {
                            item.Finally();
                        }

                    });
            }
        }
        #endregion
    }
}
