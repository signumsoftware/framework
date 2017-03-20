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
            action?.Invoke();
        }

        public static IDisposable Combine(IDisposable first, IDisposable second)
        {
            if (first == null || second == null)
                return first ?? second;

            var firstEx = first as IDisposableException;
            var secondEx = second as IDisposableException;

            if (firstEx == null && secondEx == null)
                return new Disposable(() => { try { first.Dispose(); } finally { second.Dispose(); } });

            return new DisposableException(              
                ex =>
                {
                    try { if (firstEx != null) firstEx.OnException(ex); }
                    finally { if (secondEx != null) secondEx.OnException(ex); }
                },
                () => { try { first.Dispose(); } finally { second.Dispose(); } });
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

    public class DisposableException : IDisposableException
    {
        Action<Exception> onException;
        Action dispose;

        public DisposableException(Action<Exception> onException, Action dispose)
        {
            this.onException = onException;
            this.dispose = dispose;
        }

        public void OnException(Exception ex)
        {
            this.onException(ex);
        }

        public void Dispose()
        {
            this.dispose();
        }
    }
    
}
