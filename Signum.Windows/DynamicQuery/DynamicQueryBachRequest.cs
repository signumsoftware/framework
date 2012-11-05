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

namespace Signum.Windows
{
    internal class DynamicQueryBachRequest
    {
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
    }
}
