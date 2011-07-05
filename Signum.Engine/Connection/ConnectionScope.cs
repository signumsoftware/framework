using System;
using System.Collections.Generic;
using System.Text;
using Signum.Utilities;
using Signum.Engine.Maps;
using System.IO;

namespace Signum.Engine
{
    public class ConnectionScope: IDisposable
    {
        [ThreadStatic]
        private static Stack<BaseConnection> stack;

        private bool disposed = false;

        public ConnectionScope(BaseConnection connection)
        { 
            if (stack == null)
                stack = new Stack<BaseConnection>();

            stack.Push(connection);        
        }

        public void Dispose()
        {
            if (!disposed)
            {
                stack.Pop();

                if (stack.Count == 0)
                    stack = null;
            }

            disposed = true;
        }

        public static BaseConnection Current
        {
            get 
            {
                if (stack == null || stack.Count == 0)
                    return Default;

                return stack.Peek();
            }
        }

        static BaseConnection _default;
        public static BaseConnection Default
        {
            get { return _default; }
            set { _default = value; }
        }
    }
}
