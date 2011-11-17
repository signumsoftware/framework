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

        static readonly Variable<ExecutionContext> currentExecutionContext = Statics.ThreadVariable<ExecutionContext>("executionContext");

        public static ExecutionContext Current
        {
            get { return currentExecutionContext.Value; }
        }

        public static IDisposable Scope(ExecutionContext executionContext)
        {
            var oldValue = currentExecutionContext.Value;
            currentExecutionContext.Value = executionContext;
            return new Disposable(() => currentExecutionContext.Value = oldValue);
        }
    }
}
