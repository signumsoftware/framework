using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.Synchronization;
using Signum.Utilities;
using Signum.Utilities.DataStructures;

namespace Signum.Engine
{
    public class CommandTimeoutScope : IDisposable
    {
        [ThreadStatic]
        private static Stack<int> stack;

        private bool disposed = false;

        internal static int? Current
        {
            get
            {
                if (stack == null || stack.Count == 0)
                    return null;

                return stack.Peek();
            }
        }

        /// <param name="timeout">timeout of a command in seconds</param>
        public CommandTimeoutScope(int timeout)
        {
            if (stack == null)
                stack = new Stack<int>();

            stack.Push(timeout);
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
    }
}
