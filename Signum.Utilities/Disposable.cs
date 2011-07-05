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
    }
}
