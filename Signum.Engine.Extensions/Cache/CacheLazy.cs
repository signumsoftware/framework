
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;

namespace Signum.Engine.Cache
{
    public static class CacheLazy
    {
        static readonly object none = new object();

        static ConcurrentDictionary<IResetLazy, object> registeredLazyList = new ConcurrentDictionary<IResetLazy, object>();
        public static ResetLazy<T> Create<T>(Func<T> func, LazyThreadSafetyMode mode = LazyThreadSafetyMode.PublicationOnly) where T : class
        {
            ResetLazy<T> result = null;
            EventHandler invalidate = (sender, args) =>
            {
                if (args == CacheLogic.InvalidatedCacheEventArgs.Instance || args is SqlNotificationEventArgs)
                    result.Reset();
                else if (args == CacheLogic.DisabledCacheEventArgs.Instance)
                {
                    if (Transaction.InTestTransaction)
                    {
                        result.Reset();
                        Transaction.Rolledback += () => result.Reset();
                    }

                    Transaction.PostRealCommit += dic => result.Reset();
                }
            };

            result = new ResetLazy<T>(() =>
            {
                using (ExecutionMode.Global())
                using (HeavyProfiler.Log("Lazy", () => typeof(T).TypeName()))
                using (CacheLogic.NotifyCacheChange(invalidate))
                using (Transaction tr = Transaction.InTestTransaction ? null : Transaction.ForceNew())
                using (new EntityCache(true))
                {
                    var value = func();

                    if (tr != null)
                        tr.Commit();

                    return value;
                }
            }, mode);

            registeredLazyList.GetOrAdd(result, none);

            return result;
        }

        public static void ResetAll()
        {
            foreach (var lp in registeredLazyList.Keys)
                lp.Reset();
        }

        public static void LoadAll()
        {
            foreach (var lp in registeredLazyList.Keys)
                lp.Load();
        }
    }
}
