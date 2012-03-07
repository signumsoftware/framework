using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Utilities.DataStructures;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Threading;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine
{
    public static class GlobalLazy
    {
        static bool initialized; 

        internal static void GlobalLazy_Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            var s = Schema.Current;
            foreach (var lp in registeredLazyList.Values.ToList())
            {
                if (lp.InvalidateWith != null)
                {
                    AttachInvalidations(s, lp);
                }
            }
        }

        private static void AttachInvalidations(Schema s, ILazyProxy lp)
        {
            Action a = () => lp.Reset();

            List<Type> cached = new List<Type>();
            foreach (var type in lp.InvalidateWith)
            {
                var cc = s.CacheController(type);
                if (cc != null && cc.IsComplete)
                {
                    cc.Invalidation += a;
                    cached.Add(type);
                }
            }

            var nonCached = lp.InvalidateWith.Except(cached).ToList();
            if (nonCached.Any())
            {
                foreach (var type in nonCached)
                {
                    giAttachInvalidations.GetInvoker(type)(s, a);
                }

                var dgIn = DirectedGraph<Table>.Generate(lp.InvalidateWith.Except(cached).Select(t => s.Table(t)), t => t.DependentTables().Select(kvp => kvp.Key)).ToHashSet();
                var dgOut = DirectedGraph<Table>.Generate(cached.Select(t => s.Table(t)), t => t.DependentTables().Select(kvp => kvp.Key)).ToHashSet();

                foreach (var table in dgIn.Except(dgOut))
                {
                    giAttachInvalidationsDependant.GetInvoker(table.Type)(s, a);
                }
            }
        }


        static GenericInvoker<Action<Schema, Action>> giAttachInvalidationsDependant = new GenericInvoker<Action<Schema, Action>>((s, a) => AttachInvalidationsDependant<IdentifiableEntity>(s, a));
        static void AttachInvalidationsDependant<T>(Schema s, Action action) where T : IdentifiableEntity
        {
            var ee = s.EntityEvents<T>();

            ee.Saving += e =>
            {
                if (!e.IsNew && e.Modified == true)
                    action();
            };
            ee.PreUnsafeUpdate += q => action();
        }

        static GenericInvoker<Action<Schema, Action>> giAttachInvalidations = new GenericInvoker<Action<Schema, Action>>((s, a) => AttachInvalidations<IdentifiableEntity>(s, a));
        static void AttachInvalidations<T>(Schema s, Action action) where T : IdentifiableEntity
        {
            var ee = s.EntityEvents<T>();

            ee.Saving += e =>
            {
                if (e.Modified == true)
                    action();
            };
            ee.PreUnsafeUpdate += q => action();
            ee.PreUnsafeDelete += q => action();
        }

        static Dictionary<object, ILazyProxy> registeredLazyList = new Dictionary<object, ILazyProxy>();
        public static Lazy<T> Create<T>(Func<T> func)
        {
            var result = new Lazy<T>(() =>
            {
                using (Schema.Current.GlobalMode())
                using (HeavyProfiler.Log("Lazy", () => typeof(T).TypeName()))
                using (new EntityCache(true))
                {
                    return func();
                }
            }, LazyThreadSafetyMode.PublicationOnly);

            registeredLazyList.Add(result, new LazyProxy<T>(result));

            return result;
        }

        public static Lazy<T> InvalidateWith<T>(this Lazy<T> lazy, params Type[] types)
        {
            var lp = registeredLazyList.GetOrThrow(lazy, "The lazy is not a GlobalLazy");
            
            lp.InvalidateWith = types;

            if (initialized)
                AttachInvalidations(Schema.Current, lp);

            return lazy;
        }

        public static void ResetAll()
        {
            foreach (var lp in registeredLazyList.Values)
                lp.Reset();
        }

        public static void LoadAll()
        {
            foreach (var lp in registeredLazyList.Values)
                lp.Load();
        }

        interface ILazyProxy
        {
            void Reset();
            void Load();
            Type[] InvalidateWith { get; set; }
        }

        class LazyProxy<T> : ILazyProxy
        {
            Lazy<T> lazy;
            public LazyProxy(Lazy<T> lazy)
            {
                this.lazy = lazy;
            }

            public void Reset()
            {
                lazy.ResetPublicationOnly();
            }

            public void Load()
            {
                lazy.Load();
            }

            public Type[] InvalidateWith { get; set; }
        }
    }
}
