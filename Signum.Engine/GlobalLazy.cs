
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
using Signum.Engine;
using Signum.Entities.Reflection;

namespace Signum.Engine
{
    public struct InvalidateWith
    {
        static readonly Type[] Empty = new Type[0];

        readonly Type[] types;
        public Type[] Types 
        {
            get { return types ?? Empty; } 
        }

        public InvalidateWith(params Type[] types)
        {
            if(types != null)
                foreach (var type in types)
                {
                    if (type.IsAbstract)
                        throw new InvalidOperationException("Impossible to invalidate using {0} because is abstract".Formato(type));

                    if (!Reflector.IsIdentifiableEntity(type))
                        throw new InvalidOperationException("Impossible to invalidate using {0} because is not and IdentifiableEntity".Formato(type));
                }


            this.types = types;
        }
    
        internal bool InSchema()
        {
            return Types.All(Schema.Current.Tables.ContainsKey);
        }
    }

    public static class GlobalLazy
    {
        public static GlobalLazyManager Manager = new GlobalLazyManager();

        static ConcurrentDictionary<IResetLazy, InvalidateWith> registeredLazyList = new ConcurrentDictionary<IResetLazy, InvalidateWith>();
        public static ResetLazy<T> Create<T>(Func<T> func, InvalidateWith invalidateWith) where T : class
        {
            ResetLazy<T> result = null;

            result = new ResetLazy<T>(() =>
            {
                Manager.AssertAttached(invalidateWith);

                using (ExecutionMode.Global())
                using (HeavyProfiler.Log("ResetLazy", () => typeof(T).TypeName()))
                using (Transaction tr = Transaction.InTestTransaction ? null : Transaction.ForceNew())
                using (new EntityCache(true))
                {
                    var value = func();

                    if (tr != null)
                        tr.Commit();

                    return value;
                }
            }, mode: LazyThreadSafetyMode.ExecutionAndPublication);


            registeredLazyList.GetOrAdd(result, invalidateWith);

            return result;
        }

        internal static void Schema_Initializing()
        {
            Schema schema = Schema.Current;

            foreach (var kvp in registeredLazyList)
            {
                if (kvp.Value.InSchema())
                {
                    IResetLazy lazy = kvp.Key;

                    Manager.AttachInvalidations(kvp.Value, (sender, args) => lazy.Reset());
                }
            }
        }

        public static IEnumerable<Type> TypesForInvalidation()
        {
            Schema schema = Schema.Current;

            return (from kvp in registeredLazyList
                    where kvp.Value.InSchema()
                    from type in kvp.Value.Types
                    select type).Distinct();
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

    public class GlobalLazyManager
    {
        public virtual void AttachInvalidations(InvalidateWith invalidateWith, EventHandler invalidate)
        {
            Action onInvalidation = () =>
            {
                if (Transaction.InTestTransaction)
                {
                    invalidate(this, null);
                    Transaction.Rolledback += () => invalidate(this, null);
                }

                Transaction.PostRealCommit += dic => invalidate(this, null);
            };

            Schema schema = Schema.Current; 

            foreach (var type in invalidateWith.Types)
            {
                giAttachInvalidations.GetInvoker(type)(schema, onInvalidation);
            }

            var dependants = DirectedGraph<Table>.Generate(invalidateWith.Types.Select(t => schema.Table(t)), t => t.DependentTables().Select(kvp => kvp.Key)).Select(t => t.Type).ToHashSet();
            dependants.ExceptWith(invalidateWith.Types);

            foreach (var type in dependants)
            {
                giAttachInvalidationsDependant.GetInvoker(type)(schema, onInvalidation);
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

        public virtual void AssertAttached(InvalidateWith invalidateWith)
        {
            foreach (var type in invalidateWith.Types)
            {
                Schema.Current.Table(type);
            }
        }
    }
}
