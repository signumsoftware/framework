using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

namespace Signum.Engine
{
    public class ExecutionContext
    {
        public string Description { get; private set; }
        public ExecutionContext(string description)
        {
            this.Description = description;
        }

        public override string ToString()
        {
            return Description;
        }


        public static ExecutionContext UserInterface = new ExecutionContext("UserInterface");  

        [ThreadStatic]
        static ExecutionContext current;
        public static ExecutionContext Current
        {
            get { return current; }
        }

        public static IDisposable Scope(ExecutionContext executionContext)
        {
            var oldValue = current;
            current = executionContext;
            return new Disposable(() => current = oldValue);
        }
    }
}
