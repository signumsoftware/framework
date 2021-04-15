using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Signum.Entities.Reflection;

namespace Signum.Engine
{
    public struct InvalidateWith
    {
        static readonly Type[] Empty = Array.Empty<Type>();

        readonly Type[]? types;
        public Type[] Types
        {
            get { return types ?? Empty; }
        }

        public InvalidateWith(params Type[]? types)
        {
            if(types != null)
                foreach (var type in types)
                {
                    if (type.IsAbstract)
                        throw new InvalidOperationException("Impossible to invalidate using {0} because is abstract".FormatWith(type));

                    if (!Reflector.IsEntity(type))
                        throw new InvalidOperationException("Impossible to invalidate using {0} because is not and Entity".FormatWith(type));
                }


            this.types = types;
        }
    }


    public static class GlobalLazy
    {
        static readonly HashSet<IResetLazy> registeredLazyList = new HashSet<IResetLazy>();
        public static List<ResetLazyStats> Statistics()
        {
            return registeredLazyList.Select(a => a.Stats()).OrderByDescending(x=>x.SumLoadTime).ToList();
        }


        public static ResetLazy<T> WithoutInvalidations<T>(Func<T> func, LazyThreadSafetyMode mode = LazyThreadSafetyMode.ExecutionAndPublication)
        {
            ResetLazy<T> result = new ResetLazy<T>(() =>
            {
                using (ExecutionMode.Global())
                using (HeavyProfiler.Log("ResetLazy", () => typeof(T).TypeName()))
                using (Transaction? tr = Transaction.InTestTransaction ? null : Transaction.ForceNew())
                using (new EntityCache(EntityCacheType.ForceNewSealed))
                {
                    var value = func();

                    if (tr != null)
                        tr.Commit();

                    return value;
                }
            }, mode,
            declaringType: func.Method.DeclaringType);

            registeredLazyList.Add(result);

            return result;
        }

        public static void ResetAll()
        {
            foreach (var lp in registeredLazyList)
                lp.Reset();
        }

        public static void LoadAll()
        {
            foreach (var lp in registeredLazyList)
                lp.Load();
        }
    }


}
