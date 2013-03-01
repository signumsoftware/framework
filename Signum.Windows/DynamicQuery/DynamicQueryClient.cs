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
    public class DynamicQueryClient
    {
        static ConcurrentDictionary<QueryToken, IEnumerable<QueryToken>> extensionCache = new ConcurrentDictionary<QueryToken, IEnumerable<QueryToken>>();
        internal static IEnumerable<QueryToken> GetExtensionToken(QueryToken token)
        {
            return extensionCache.GetOrAdd(token, t => Server.Return((IDynamicQueryServer server) => server.ExternalQueryToken(t)));
        }

        static ConcurrentDictionary<object, QueryDescription> descriptionsCache = new ConcurrentDictionary<object, QueryDescription>();
        public static QueryDescription GetQueryDescription(object queryName)
        {
            return descriptionsCache.GetOrAdd(queryName, qn => Server.Return((IDynamicQueryServer s) => s.GetQueryDescription(qn)));
        }

        public static void SetFilterTokens(object queryName, IEnumerable<FilterOption> filters)
        {
            QueryDescription description = GetQueryDescription(queryName);

            foreach (var f in filters)
            {
                if (f.Token == null && f.Path.HasText())
                    f.Token = QueryUtils.Parse(f.Path, t => QueryUtils.SubTokens(t, description.Columns));

                f.RefreshRealValue();
            }
        }

        public static void SetOrderTokens(object queryName, IEnumerable<OrderOption> orders)
        {
            QueryDescription description = GetQueryDescription(queryName);

            foreach (var o in orders)
            {
                o.Token = QueryUtils.Parse(o.Path, t => QueryUtils.SubTokens(t, description.Columns));
            }
        }


        public static Lite<T> FindUnique<T>(string columnName, object value, UniqueType uniqueType)
           where T : class, IIdentifiable
        {
            return (Lite<T>)FindUnique(new FindUniqueOptions(typeof(T))
            {
                UniqueType = uniqueType,
                FilterOptions = new List<FilterOption>()
                {
                    new FilterOption(columnName, value)
                }
            });
        }

        public static Lite<T> FindUnique<T>(FindUniqueOptions options)
           where T : class, IIdentifiable
        {
            if (options.QueryName == null)
                options.QueryName = typeof(T);

            return (Lite<T>)FindUnique(options);
        }

        public static Lite<IdentifiableEntity> FindUnique(FindUniqueOptions options)
        {
            Navigator.Manager.AssertFindable(options.QueryName);

            SetFilterTokens(options.QueryName, options.FilterOptions);
            SetOrderTokens(options.QueryName, options.OrderOptions);

            var request = new UniqueEntityRequest
            {
                QueryName = options.QueryName,
                Filters = options.FilterOptions.Select(f => f.ToFilter()).ToList(),
                Orders = options.OrderOptions.Select(f => f.ToOrder()).ToList(),
                UniqueType = options.UniqueType,
            };

            return Server.Return((IDynamicQueryServer s) => s.ExecuteUniqueEntity(request));
        }

        public static int QueryCount(CountOptions options)
        {
            Navigator.Manager.AssertFindable(options.QueryName);

            SetFilterTokens(options.QueryName, options.FilterOptions);

            var request = new QueryCountRequest
            {
                QueryName = options.QueryName,
                Filters = options.FilterOptions.Select(f => f.ToFilter()).ToList()
            };

            return Server.Return((IDynamicQueryServer s) => s.ExecuteQueryCount(request));
        }

        #region Batch
        public static void QueryCountBatch(CountOptions options, Action<int> onResult, Action @finally)
        {
            Navigator.Manager.AssertFindable(options.QueryName);

            SetFilterTokens(options.QueryName, options.FilterOptions);

            var request = new QueryCountRequest
            {
                QueryName = options.QueryName,
                Filters = options.FilterOptions.Select(f => f.ToFilter()).ToList()
            };

            Enqueue(request, obj => onResult((int)obj), @finally);
        }

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

        static List<RequestTuple> tuples = new List<RequestTuple>();

        public static void Enqueue(BaseQueryRequest request, Action<object> onResult, Action @finally)
        {
            lock (tuples)
            {
                tuples.Add(new RequestTuple
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
                if (tuples.IsEmpty())
                    throw new InvalidOperationException("Calling Hooks_DispatcherInactive with no tuples");

                var tup = tuples.ToList();

                tuples.Clear();

                object[] results = null;
                Async.Do(() =>
                    {
                        results = Server.Return((IDynamicQueryServer dqs) => dqs.BatchExecute(tup.Select(a => a.Request).ToArray()));
                    },
                    () =>
                    {
                        foreach (var item in tup.Zip(results))
                        {
                            item.Item1.OnResult(item.Item2);
                        }
                    },
                    () =>
                    {
                        foreach (var item in tup)
                        {
                            item.Finally();
                        }

                    });

                Dispatcher.CurrentDispatcher.Hooks.DispatcherInactive -= new EventHandler(Hooks_DispatcherInactive);
            }
        }
        #endregion
    }
}
