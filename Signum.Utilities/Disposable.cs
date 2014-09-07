using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Signum.Utilities
{
    public class Disposable: IDisposable
    {
        Action action;
        public Disposable(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            this.action = action;
        }

        public void Dispose()
        {
            if (action != null)
                action(); 
        }

        public static IDisposable Combine(IDisposable first, IDisposable second)
        {
            if (first == null || second == null)
                return first ?? second;

            return new Disposable(() => { try { first.Dispose(); } finally { second.Dispose(); } });
        }

        public static IDisposable Combine<Del>(Del delegated, Func<Del, IDisposable> invoke) where Del : class, ICloneable, ISerializable
        {
            if (delegated == null)
                return null;

            IDisposable result = null;
            foreach (var func in delegated.GetInvocationListTyped())
            {
                try
                {
                    result = Disposable.Combine(result, invoke(func));
                }
                catch (Exception e)
                {
                    if (result != null)
                        result.Dispose();

                    throw e;
                }
            }

            return result;
        }


    }

    
}
