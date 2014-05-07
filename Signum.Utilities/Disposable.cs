using System;
using System.Collections.Generic;
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
    }

    
}
