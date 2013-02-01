
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
    }

    public static class GlobalLazy
    {
        static HashSet<IResetLazy> registeredLazyList = new HashSet<IResetLazy>();
        public static ResetLazy<T> WithoutInvalidations<T>(Func<T> func) where T : class
        {
            ResetLazy<T> result = new ResetLazy<T>(() =>
            {
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
            }, mode: LazyThreadSafetyMode.ExecutionAndPublication, 
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
