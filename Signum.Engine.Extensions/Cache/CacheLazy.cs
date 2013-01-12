
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
        static ConcurrentDictionary<IResetLazy, Type[]> registeredLazyList = new ConcurrentDictionary<IResetLazy, Type[]>();
        public static ResetLazy<T> Create<T>(Func<T> func, LazyThreadSafetyMode mode = LazyThreadSafetyMode.PublicationOnly) where T : class
        {
            ResetLazy<T> result = null;
            EventHandler<CacheEventArgs> invalidate = (sender, args) =>
            {
                if (args == CacheEventArgs.Invalidated)
                {
                    result.Reset();
                }
                else if (args == CacheEventArgs.Disabled)
                {
                    if (Transaction.InTestTransaction)
                    {
                        result.Reset();
                        Transaction.Rolledback += () => result.Reset();
                    }

                    Transaction.PostRealCommit += dic => result.Reset();
                }
            };

            bool isFirsTime = true;

            result = new ResetLazy<T>(() =>
            {
                if (isFirsTime)
                {
                    Type[] types = registeredLazyList[result];
                    if (types != null)
                        CacheLogic.AttachInvalidations(invalidate, types);
                    isFirsTime = false;
                }

                using (ExecutionMode.Global())
                using (HeavyProfiler.Log("Lazy", () => typeof(T).TypeName()))
                using (Transaction tr = Transaction.InTestTransaction ? null : Transaction.ForceNew())
                using (new EntityCache(true))
                {
                    var value = func();

                    if (tr != null)
                        tr.Commit();

                    return value;
                }
            }, mode);

            registeredLazyList.GetOrAdd(result, (Type[])null);

            return result;
        }

        public static ResetLazy<T> InvalidateWith<T>(this ResetLazy<T> lazy, params Type[] types) where T : class
        {
            if (!registeredLazyList.ContainsKey(lazy))
                throw new InvalidOperationException("The lazy of type '{0}' is not a CacheLazy".Formato(typeof(T).TypeName()));

            registeredLazyList.TryUpdate(lazy, types, null);

            return lazy;
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
